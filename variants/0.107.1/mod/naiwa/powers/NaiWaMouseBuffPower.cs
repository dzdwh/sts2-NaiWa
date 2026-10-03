using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace NaiWa.powers;

/// <summary>
/// 永远的奶鼠 — 每回合随机效果（每20HP额外一次）。
/// 承伤优先级继承 NaiWaMinionPowerBase：奶蛋 > 后召唤先挡。
/// </summary>
public sealed class NaiWaMouseBuffPower : NaiWaMinionPowerBase
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override bool ShouldAllowHitting(Creature creature) => true;

    /// <summary>召唤此奶鼠的卡牌，用于跨战斗持久化HP�?/summary>
    public cards.NaiWaYongYuanDeNaiShu? SourceCard { get; set; }

    /// <summary>本战斗内，每张"永远的奶鼠"副本已贡献给这只奶鼠的 TotalHp（防多副本重复/漏算）。</summary>
    private readonly Dictionary<cards.NaiWaYongYuanDeNaiShu, int> _contributedAcc = new();

    /// <summary>记录某张奶鼠卡副本当前 TotalHp 已应用到这只奶鼠。</summary>
    public void SetAccumulatedContribution(cards.NaiWaYongYuanDeNaiShu card, int acc)
        => _contributedAcc[card] = acc;

    /// <summary>
    /// 本张奶鼠卡副本还需补给奶鼠的 HP：
    /// 每张副本独立记账，奶鼠 HP = 各张已打出副本当前 TotalHp 之和。
    /// 本次打出补给 = 该副本当前 TotalHp − 该副本已贡献的 TotalHp。
    /// </summary>
    /// <param name="card">奶鼠卡副本。</param>
    /// <param name="currentTotalHp">该副本当前 TotalHp（卡面 MouseHp）。</param>
    public int GetAccumulatedDelta(cards.NaiWaYongYuanDeNaiShu card, int currentTotalHp)
    {
        _contributedAcc.TryGetValue(card, out int prev);
        int delta = currentTotalHp - prev;
        if (delta > 0) _contributedAcc[card] = currentTotalHp;
        return Math.Max(0, delta);
    }

    private readonly List<Action<CombatRoom, Player>> _pendingRewards = new();

    // === Action sets ===

    private static async Task Common3x3(PlayerChoiceContext ctx, Player p, Creature self, Rng rng)
    {
        var enemies = self.CombatState?.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies == null || enemies.Count == 0) return;
        var target = enemies[rng.NextInt(enemies.Count)];
        try { await CreatureCmd.Damage(ctx, target, 5m, ValueProp.Unpowered, self, null); } catch (Exception ex) { Log.Error($"[MouseBuff ST] Damage failed: {ex.Message}"); }
    }

    private static async Task CommonAoe(PlayerChoiceContext ctx, Player p, Creature self, Rng rng)
    {
        var enemies = self.CombatState?.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies == null || enemies.Count == 0) return;
        foreach (var e in enemies)
            try { await CreatureCmd.Damage(ctx, e, 3m, ValueProp.Unpowered, self, null); } catch (Exception ex) { Log.Error($"[MouseBuff AoE] Damage failed: {ex.Message}"); }
    }

    private static async Task CommonBlock(PlayerChoiceContext ctx, Player p, Creature self, Rng rng)
    {
        await CreatureCmd.GainBlock(p.Creature, 5m, ValueProp.Unpowered, null);
    }

    private static async Task UncommonStrength(PlayerChoiceContext ctx, Player p, Creature self, Rng rng)
    {
        await PowerCmd.Apply<StrengthPower>(ctx, p.Creature, 1, p.Creature, null);
    }

    private static async Task UncommonDexterity(PlayerChoiceContext ctx, Player p, Creature self, Rng rng)
    {
        await PowerCmd.Apply<DexterityPower>(ctx, p.Creature, 1, p.Creature, null);
    }

    private static async Task UncommonHeal(PlayerChoiceContext ctx, Player p, Creature self, Rng rng)
    {
        await CreatureCmd.Heal(p.Creature, 6m);
    }

    private static async Task RareMaxHp(PlayerChoiceContext ctx, Player p, Creature self, Rng rng)
    {
        await CreatureCmd.GainMaxHp(p.Creature, 3);
    }

    private static readonly (Func<PlayerChoiceContext, Player, Creature, Rng, Task> Action, string Text, VfxColor Color)[] _commonActions =
    {
        (Common3x3, "5 !", VfxColor.Red),
        (CommonAoe, "3 AOE!", VfxColor.Red),
        (CommonBlock, "5 !", VfxColor.Blue),
    };

    private static readonly (Func<PlayerChoiceContext, Player, Creature, Rng, Task> Action, string Text, VfxColor Color)[] _uncommonActions =
    {
        (UncommonStrength, "+1 !", VfxColor.Green),
        (UncommonDexterity, "+1 !", VfxColor.Green),
        (UncommonHeal, "6 !", VfxColor.Green),
    };

    private static readonly string[] _rarePostTexts = { " !", "30 !", " !" };

    private enum RarePostEffect { Card, Gold, Potion }

    /// <summary>在奶鼠头顶弹出对话泡，模仿多人模�?ping 的效果�?/summary>
    private static void ShowBubble(Creature self, string text, VfxColor color = VfxColor.White)
    {
        var bubble = NSpeechBubbleVfx.Create(text, self, 1.5, color);
        if (bubble == null) return;
        // 气泡添加到场景后再改 Scale 和位置，避免 _Ready 动画覆盖
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(bubble);
        bubble.Scale = new Godot.Vector2(0.5f, 0.5f);
        bubble.GlobalPosition = bubble.GlobalPosition + new Godot.Vector2(0, -100);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (player != base.Owner.PetOwner) return;
        await TriggerTurnEffect(ctx, player);
    }

    /// <summary>每回合效果：执行 1+自身生命/20 次随机效果。</summary>
    public override async Task TriggerTurnEffect(PlayerChoiceContext ctx, Player player)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        var rng = player.PlayerRng.Rewards;
        int execCount = 1 + base.Owner.CurrentHp / 20;

        for (int i = 0; i < execCount; i++)
        {
            var roll = rng.NextFloat();
            if (roll < 0.7f)
            {
                var a = _commonActions[rng.NextInt(_commonActions.Length)];
                await a.Action(ctx, player, base.Owner, rng);
                ShowBubble(base.Owner, a.Text, a.Color);
            }
            else if (roll < 0.95f)
            {
                var a = _uncommonActions[rng.NextInt(_uncommonActions.Length)];
                await a.Action(ctx, player, base.Owner, rng);
                ShowBubble(base.Owner, a.Text, a.Color);
            }
            else
            {
                if (rng.NextBool())
                {
                    await RareMaxHp(ctx, player, base.Owner, rng);
                    ShowBubble(base.Owner, "+3 !", VfxColor.Gold);
                }
                else
                {
                    var post = (RarePostEffect)rng.NextInt(3);
                    ShowBubble(base.Owner, _rarePostTexts[(int)post], VfxColor.Gold);
                    switch (post)
                    {
                        case RarePostEffect.Card:
                            _pendingRewards.Add((room, p) =>
                                room.AddExtraReward(p, new CardReward(CardCreationOptions.ForRoom(p, room.RoomType), 3, p)));
                            break;
                        case RarePostEffect.Gold:
                            _pendingRewards.Add((room, p) =>
                                room.AddExtraReward(p, new GoldReward(30, p)));
                            break;
                        case RarePostEffect.Potion:
                            _pendingRewards.Add((room, p) =>
                                room.AddExtraReward(p, new PotionReward(p)));
                            break;
                    }
                }
            }
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        // HP 持久化已移至 NaiWaMouseHpPersistence Harmony patch
        // （Power 上的 AfterCombatEnd 对宠物不触发�?        if (_pendingRewards.Count == 0) return Task.CompletedTask;
        var player = base.Owner?.PetOwner;
        if (player == null) return Task.CompletedTask;
        foreach (var reward in _pendingRewards)
            reward(room, player);
        _pendingRewards.Clear();
        return Task.CompletedTask;
    }

    /// <summary>心牌触发：执行一次奶鼠随机效果�?/summary>
    public static async Task TriggerOneEffect(Player player, Creature mouse)
    {
        var rng = player.PlayerRng.Rewards;
        var roll = rng.NextFloat();
        var ctx = new ThrowingPlayerChoiceContext();
        var source = (mouse != null && mouse.IsAlive) ? mouse : player.Creature;
        if (roll < 0.7f)
        {
            var a = _commonActions[rng.NextInt(_commonActions.Length)];
            await a.Action(ctx, player, source, rng);
            if (mouse != null) ShowBubble(mouse, a.Text, a.Color);
        }
        else if (roll < 0.95f)
        {
            var a = _uncommonActions[rng.NextInt(_uncommonActions.Length)];
            await a.Action(ctx, player, source, rng);
            if (mouse != null) ShowBubble(mouse, a.Text, a.Color);
        }
        else
        {
            if (rng.NextBool())
            {
                await RareMaxHp(ctx, player, source, rng);
                if (mouse != null) ShowBubble(mouse, "+3 HP !", VfxColor.Gold);
            }
            else
            {
                var post = (RarePostEffect)rng.NextInt(3);
                if (mouse != null) ShowBubble(mouse, _rarePostTexts[(int)post], VfxColor.Gold);
                var creatureForPending = mouse?.GetPower<NaiWaMouseBuffPower>()
                    ?? player.Creature.GetPower<NaiWaMouseBuffPower>();
                if (creatureForPending == null) return;
                switch (post)
                {
                    case RarePostEffect.Card:
                        creatureForPending._pendingRewards.Add((room, p) =>
                            room.AddExtraReward(p, new CardReward(CardCreationOptions.ForRoom(p, room.RoomType), 3, p)));
                        break;
                    case RarePostEffect.Gold:
                        creatureForPending._pendingRewards.Add((room, p) =>
                            room.AddExtraReward(p, new GoldReward(30, p)));
                        break;
                    case RarePostEffect.Potion:
                        creatureForPending._pendingRewards.Add((room, p) =>
                            room.AddExtraReward(p, new PotionReward(p)));
                        break;
                }
            }
        }
    }
}
