using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.minions;

namespace NaiWa.powers;

/// <summary>
/// 奶鲸 - 在场时吞下所有其他召唤物（暂时离场，不触发效果），每吞下一个获得等量回血；
/// 下回合开始放出被吞召唤物并离场。体型为基础随从2倍，随最大生命变大（类似奥斯提）。
/// </summary>
public sealed class NaiWaJingTankPower : NaiWaMinionPowerBase
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    private static bool _subscribed;

    /// <summary>被吞下的召唤物（战斗内，暂时离场）。</summary>
    public List<Creature> SwallowedPets { get; } = new();

    /// <summary>订阅随从召唤事件（NaiWaMod.Init 调用）：奶鲸在场时吞下其他召唤物。</summary>
    internal static void Initialize()
    {
        if (!_subscribed)
        {
            MinionCmd.OnMinionSummoned += OnAnyMinionSummoned;
            _subscribed = true;
        }
    }

    /// <summary>
    /// 查找该玩家当前被奶鲸吞下的指定类型随从（没有则 null）。
    /// 重复召唤时用于叠 HP，不新建实例（同一类型只有一个随从）。
    /// </summary>
    public static Creature? FindSwallowed(Player player, Type minionType)
    {
        if (player?.Creature?.CombatState == null) return null;
        var jing = player.Creature.CombatState.Allies.FirstOrDefault(
            a => a.IsAlive && a.Monster is NaiWaJingMinion && a.PetOwner == player);
        var power = jing?.GetPower<NaiWaJingTankPower>();
        if (power == null) return null;
        return power.SwallowedPets.FirstOrDefault(p =>
            p != null && p.Monster != null && p.Monster.GetType() == minionType);
    }

    /// <summary>奶鲸被召唤 / 新召唤物召唤后：吞下所有未吞的其他召唤物（召唤效果已执行完）。</summary>
    private static async Task OnAnyMinionSummoned(Player player)
    {
        var jing = player.Creature.CombatState?.Allies.FirstOrDefault(
            a => a.IsAlive && a.Monster is NaiWaJingMinion && a.PetOwner == player);
        if (jing == null) return;
        var power = jing.GetPower<NaiWaJingTankPower>();
        if (power == null) return;

        var toSwallow = player.PlayerCombatState.Pets
            .Where(p => p.IsAlive && p != jing
                        && p.Monster is MinionModel // 只吞奶蛙自己的召唤物；异鸟/佩尔的士兵等官方或其他来源的 Pets 不算
                        && p.CombatState?.Allies.Contains(p) == true // 只吞在场随从，防残留旧奶鲸
                        && !power.SwallowedPets.Contains(p))
            .ToList();
        foreach (var pet in toSwallow)
        {
            await power.Swallow(pet);
        }
    }

    /// <summary>吞下一个召唤物：奶鲸增加等量最大生命值（体型随之变大），召唤物暂时离场。</summary>
    public async Task Swallow(Creature pet)
    {
        if (base.Owner == null || base.Owner.CombatState == null) return;
        if (pet.CombatState != base.Owner.CombatState || !pet.CombatState.Allies.Contains(pet)) return; // 防御：只吞在场随从

        if (base.Owner.IsAlive)
            await CreatureCmd.GainMaxHp(base.Owner, pet.CurrentHp);
        SwallowedPets.Add(pet);

        base.Owner.CombatState.RemoveCreature(pet, unattach: false);
        if (pet.PetOwner != null)
            cards.NaiWaNaiNu.RefreshCostVisuals(pet.PetOwner);
        var room = NCombatRoom.Instance;
        var node = room?.GetCreatureNode(pet);
        if (node != null)
        {
            room!.RemoveCreatureNode(node);
            node.QueueFree(); // 吞下不是死亡：直接释放节点，否则视觉上随从仍在场
        }
    }

    /// <summary>下回合开始：放出被吞召唤物，奶鲸死亡（视觉消失、尸体可复活）。</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (player.Creature != base.Owner.PetOwner?.Creature) return;

        await ReleaseAll();

        // 下回合奶鲸死亡（官方死亡流程：死亡动画后贴图透明，尸体留场可复活）
        await CreatureCmd.Kill(base.Owner);
        // 注意：不能 Remove TankPower——"尸体留场可复活"靠本 Power 的
        // ShouldCreatureBeRemovedFromCombatAfterDeath=false 保证。删掉后尸体会被
        // OnPetDied 清出 Allies/_pets，下次大口奶鲸找不到尸体 → 新建第二只鲸鱼（召唤多个随从）。
        // 复活路径的 PowerCmd.Apply 会找到已有实例，无需删除。
    }

    /// <summary>奶鲸死亡：立即放出被吞召唤物（防止随从消失）。</summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature != base.Owner) return;
        await ReleaseAll();
    }

    /// <summary>放出所有被吞召唤物（加回 Allies + 重建节点 + 摆位）。</summary>
    private async Task ReleaseAll()
    {
        if (base.Owner?.CombatState == null) return;
        foreach (var pet in SwallowedPets)
        {
            if (pet == null || pet.CombatState != base.Owner.CombatState) continue;
            // 吞下期间玩家已重新召唤了同类型随从（新实例在场）→ 旧实例作废，不再放出，防止重复随从
            bool replaced = base.Owner.CombatState.Allies.Any(a =>
                a.IsAlive && a != pet && a.PetOwner == pet.PetOwner
                && a.Monster != null && pet.Monster != null && a.Monster.GetType() == pet.Monster.GetType());
            if (replaced) continue;
            base.Owner.CombatState.AddCreature(pet);
            NCombatRoom.Instance?.AddCreature(pet);
            MinionLayout.Apply(pet);
        }
        SwallowedPets.Clear();
        if (base.Owner.PetOwner != null)
            cards.NaiWaNaiNu.RefreshCostVisuals(base.Owner.PetOwner);
        await Task.CompletedTask;
    }

    /// <summary>生命值变化时刷新体型（随最大生命变大）。</summary>
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        await base.AfterCurrentHpChanged(creature, delta);
        if (creature == base.Owner)
            UpdateScale(base.Owner);
    }

    /// <summary>回合结束时对所有敌人造成等同自身生命值的伤害（奶脚触发也走 TriggerTurnEffect）。</summary>
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.BeforeSideTurnEnd(ctx, side, participants);
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (side != base.Owner.Side) return;
        await TriggerTurnEffect(ctx, base.Owner.PetOwner);
    }

    /// <summary>每回合效果：对所有敌人造成等同自身生命值的伤害。</summary>
    public override async Task TriggerTurnEffect(PlayerChoiceContext ctx, Player owner)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        var enemies = base.Owner.CombatState?.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies == null || enemies.Count == 0) return;
        try
        {
            await CreatureCmd.Damage(ctx, enemies, base.Owner.CurrentHp, ValueProp.Unpowered, base.Owner, null);
        }
        catch (Exception ex)
        {
            Godot.GD.PrintErr($"[NaiWaJingTankPower] Turn-end damage failed: {ex.Message}");
        }
    }

    /// <summary>更新奶鲸体型：基础贴图已是其他随从2倍，HP 增长再放大（0~100 → 1.0~1.6）。</summary>
    public static void UpdateScale(Creature jing)
    {
        if (jing == null) return;
        var node = NCombatRoom.Instance?.GetCreatureNode(jing);
        if (node?.Visuals == null) return;
        float s = Mathf.Lerp(1f, 1.6f, Mathf.Clamp((float)jing.MaxHp / 100f, 0f, 1f));
        node.Visuals.Scale = Vector2.One * s;
    }
}
