using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using NaiWa.cards;

namespace NaiWa.minions;

public static class MinionCmd
{
    private static bool _registered;
    private static readonly Dictionary<Player, int> _nextOrders = new();

    private static int NextOrder(Player player)
    {
        _nextOrders.TryGetValue(player, out int v);
        _nextOrders[player] = v + 1;
        return v + 1;
    }

    /// <summary>Reset summon order counters between combats (called from combat-end cleanup).</summary>
    public static void ClearCombat()
    {
        _nextOrders.Clear();
    }

    /// <summary>Fires when a minion is newly summoned or revived. Player is the owner.</summary>
    public static event Func<Player, Task>? OnMinionSummoned;

    /// <summary>
    /// 召唤效果：打出召唤牌（新召唤/复活/已在场加HP）、破败之奶、奶格拉底之死等给随从加生命。
    /// 奶蛋每回合给自己 +2 生命不属于召唤效果，不经过这里。
    /// </summary>
    public static event Func<Player, Task>? OnSummonEffectOccurred;

    private static async Task FireOnMinionSummoned(Player player)
    {
        NaiWaNaiNu.RefreshCostVisuals(player);
        if (OnMinionSummoned != null)
        {
            foreach (var handler in OnMinionSummoned.GetInvocationList())
                await ((Func<Player, Task>)handler)(player);
        }
    }

    /// <summary>触发"召唤效果"事件（卧榻之乳蛙等订阅）。</summary>
    public static async Task FireSummonEffect(Player player)
    {
        if (OnSummonEffectOccurred == null) return;
        foreach (var handler in OnSummonEffectOccurred.GetInvocationList())
            await ((Func<Player, Task>)handler)(player);
    }

    private static void EnsureRegistered()
    {
        if (_registered) return;
        var types = new Type[] { typeof(NaiWaEggMinion), typeof(NaiWaMouseMinion),
            typeof(NaiWaDogMinion), typeof(NaiWaSnakeMinion), typeof(NaiWaRabbitMinion), typeof(NaiWaLionMinion),
            typeof(NaiWaNaiLongMinion), typeof(NaiWaNaiJiMinion), typeof(NaiWaNaiZhuMinion),
            typeof(NaiWaJingMinion) };
        foreach (var t in types)
        {
            try
            {
                GD.Print($"[MinionCmd] Injecting {t.Name}...");
                ModelDb.Inject(t);
                GD.Print($"[MinionCmd] {t.Name} injected OK");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[MinionCmd] Inject {t.Name} FAILED: {ex.GetType().Name} - {ex.Message}");
            }
        }
        _registered = true;
    }

    public static async Task<Creature> AddMinion<T>(PlayerChoiceContext ctx, Player player,
        MinionSummonOptions options = default) where T : MinionModel
    {
        ArgumentNullException.ThrowIfNull(player);
        // 天下无双：阻止召唤 → 获得1敏捷
        if (powers.NaiWaTianXiaWuShuangPower.IsActive(player.Creature))
        {
            await powers.NaiWaTianXiaWuShuangPower.OnMinionBlocked(player);
            return null!;
        }
        EnsureRegistered();
        // 已在场（Allies 里含死亡尸体：盟友侧死亡不会从 CombatState 移除）
        var existing = player.Creature.CombatState.Allies.FirstOrDefault(c => c.Monster is T && c.PetOwner == player);
        // 被奶鲸吞下（暂时离场）的随从也算"在场"：重复召唤只叠 HP，不新建实例（防重复随从）
        if (existing == null)
            existing = powers.NaiWaJingTankPower.FindSwallowed(player, typeof(T));
        if (existing != null && existing.IsAlive)
        {
            // 存活：重复召唤 → 加基础HP（不含历史累积值，避免翻倍）
            var addHp = (options.MaxHp ?? 1m) - options.AccumulatedHp;
            if (existing.Monster is NaiWaMouseMinion && options.Source is NaiWaYongYuanDeNaiShu mouseCard)
            {
                var buff = existing.GetPower<powers.NaiWaMouseBuffPower>();
                if (buff != null)
                    addHp = buff.GetAccumulatedDelta(mouseCard, (int)(options.MaxHp ?? 1m));
            }
            if (addHp > 0) await CreatureCmd.GainMaxHp(existing, addHp);
            await FireSummonEffect(player); // 打出召唤牌（已在场加HP）属于召唤效果
            return existing;
        }
        if (existing != null)
        {
            // 尸体复活：对齐 OstyCmd.Summon 的 isReviving 分支
            GD.Print($"[MinionCmd] Reviving dead {typeof(T).Name}...");
            await ReviveCorpse(ctx, player, existing, options.MaxHp ?? existing.MaxHp, options.Source);
            await FireOnMinionSummoned(player);
            await FireSummonEffect(player);
            return existing;
        }
        try
        {
            GD.Print($"[MinionCmd] AddPet<{typeof(T).Name}> calling PlayerCmd.AddPet...");
            var pet = await PlayerCmd.AddPet<T>(player);
            GD.Print($"[MinionCmd] AddPet OK, pet={pet} pet.Monster={pet?.Monster}");
            if (pet == null) { GD.PrintErr("[MinionCmd] pet is NULL!"); return null; }
            if (pet.Monster is MinionModel minion)
            {
                minion.Position = options.Position;
                minion.SummonOrder = NextOrder(player);
                WireDeathTransparency(pet);
                await minion.OnSummon(ctx, player, options);
                MinionLayout.Apply(pet);
                // 新召唤后记录该副本已贡献的 TotalHp（防第二次打出重复加基础 HP）
                if (options.Source is NaiWaYongYuanDeNaiShu summonedMouseCard)
                    pet.GetPower<powers.NaiWaMouseBuffPower>()?
                        .SetAccumulatedContribution(summonedMouseCard, (int)(options.MaxHp ?? 1m));
                await FireOnMinionSummoned(player);
                await FireSummonEffect(player);
            }
            else { GD.PrintErr($"[MinionCmd] pet.Monster is not MinionModel! type={pet.Monster?.GetType().Name}"); }
            return pet;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MinionCmd] AddMinion FAILED: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// 机械复活一具随从尸体（尸体本就留在 Allies，这里只补登记/重排/回血/恢复 UI）。
    /// 不触发召唤效果事件（FireOnMinionSummoned / FireSummonEffect）——由调用方按需触发：
    /// AddMinion 的复活分支会触发；死生契阔的自动复活不触发。
    /// </summary>
    public static async Task ReviveCorpse(PlayerChoiceContext ctx, Player player, Creature corpse,
        decimal reviveHp, CardModel? source)
    {
        if (corpse == null || corpse.IsAlive || corpse.Monster is not MinionModel)
            return;
        if (corpse.CombatState != player.Creature.CombatState)
            return;

        player.PlayerCombatState.AddPetInternal(corpse); // 幂等；死亡时若被移出 _pets 则重新登记
        ((MinionModel)corpse.Monster).SummonOrder = NextOrder(player); // 复活后成为最新，优先挨打
        await CreatureCmd.SetMaxHp(corpse, reviveHp);
        await CreatureCmd.Heal(corpse, reviveHp); // 死→活：触发 Revived 事件 + StartReviveAnim + AnimEnableUi
        NaiWaNaiNu.RefreshCostVisuals(player); // 自动复活不经过 FireOnMinionSummoned，也需要刷新减费。
        NCombatRoom.Instance?.SetCreatureIsInteractable(corpse, true); // 恢复血条可见/可悬停
        MinionLayout.Apply(corpse);
        // 奶鲸复活不经过 OnSummon：补回 TankPower，否则吞下/变大/下回合死亡逻辑失效
        if (corpse.Monster is NaiWaJingMinion)
        {
            await PowerCmd.Apply<powers.NaiWaJingTankPower>(ctx, corpse, 1, player.Creature, source);
            powers.NaiWaJingTankPower.UpdateScale(corpse);
        }
    }

    /// <summary>死亡时贴图半透明（0.35 alpha），复活时恢复全不透明。</summary>
    private static void WireDeathTransparency(Creature pet)
    {
        pet.Died += _ =>
        {
            if (pet.PetOwner != null)
                NaiWaNaiNu.RefreshCostVisuals(pet.PetOwner);
            var node = NCombatRoom.Instance?.GetCreatureNode(pet);
            if (node != null) node.Visuals.Modulate = new Color(1f, 1f, 1f, 0f);
        };
        pet.Revived += _ =>
        {
            var node = NCombatRoom.Instance?.GetCreatureNode(pet);
            if (node != null) node.Visuals.Modulate = new Color(1f, 1f, 1f, 1f);
        };
    }
}
