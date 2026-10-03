using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.minions;

namespace NaiWa.powers;

/// <summary>
/// 随从 Power 基类：照抄官方 DieForYouPower 的三个 override，让随从死亡后尸体留场可复活。
/// 缺了这些，死亡时 KillWithoutCheckingWinCondition 会销毁 NCreature 节点并把宠物移出 _pets，
/// 之后再怎么 Heal 都只是改数据，屏幕上永远没有随从。
///
/// 承伤优先级：后召唤先挡（SummonOrder 大的优先）。
/// </summary>
public abstract class NaiWaMinionPowerBase : PowerModel
{
    /// <summary>
    /// 触发一次该随从的每回合效果（奶脚"触发所有随从的效果各1次"调用）。
    /// 奶鲸未重写此方法：吞随从不属于可被触发的每回合效果。
    /// </summary>
    public virtual Task TriggerTurnEffect(PlayerChoiceContext ctx, Player owner) => Task.CompletedTask;

    /// <summary>
    /// 当前承伤的随从：奶蛋活着时是奶蛋，否则是召唤序最大（最后召唤）的存活随从。
    /// 与 ModifyUnblockedDamageTarget 的判定规则一致，死生契阔用它选目标。
    /// </summary>
    public static Creature? GetCurrentTank(Player player)
    {
        var allies = player?.Creature?.CombatState?.Allies;
        if (allies == null) return null;
        var myMinions = allies.Where(a => a.IsAlive && a.PetOwner == player && a.Monster is MinionModel).ToList();
        var egg = myMinions.FirstOrDefault(a => a.Monster is NaiWaEggMinion);
        if (egg != null) return egg;
        return myMinions.OrderByDescending(a => ((MinionModel)a.Monster).SummonOrder).FirstOrDefault();
    }

    /// <summary>否决"死亡后从战斗移除"：尸体节点留场（死亡动画 shouldRemove=false），宠物保留在 _pets。</summary>
    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
        => creature != base.Owner;

    /// <summary>Power 不随 Owner 死亡移除，否则复活后第二次死亡没人否决移除。</summary>
    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

    /// <summary>死亡状态下不接受新 Power（官方注释：so Osty won't receive powers while it is dead）。</summary>
    public override bool ShouldAllowHitting(Creature creature) => creature.IsAlive;

    /// <summary>
    /// 替主人挡非阻挡伤害。
    /// 只有召唤序号最大（最新）的活随从才挡——后召先挨打。
    /// </summary>
    public override Creature? ModifyUnblockedDamageTarget(Creature target, decimal damage, ValueProp props, Creature? dealer)
    {
        if (base.Owner == null || base.Owner.IsDead) return target;
        if (target != base.Owner.PetOwner?.Creature) return target;
        if (!props.IsPoweredAttack()) return target;
        // Only the newest alive minion tanks
        if (!IsNewestAliveTank()) return target;
        Godot.GD.Print($"[Tank] Redirect target={target.Name} damage={damage} props={props} dealer={dealer?.Name} -> owner={base.Owner.Name}(hp{base.Owner.CurrentHp})");
        return base.Owner;
    }

    private bool IsNewestAliveTank()
    {
        var player = base.Owner.PetOwner;
        if (player == null) return true;
        var myMonster = base.Owner.Monster;

        // 奶蛋永远最高优先级：只要奶蛋活着，其他随从都不挡（只统计在场随从，被奶鲸吞下的不参与）
        var isEgg = myMonster is NaiWaEggMinion;
        if (isEgg) return true;
        var eggAlive = player.Creature.CombatState.Allies.Any(p =>
            p.IsAlive && p != base.Owner && p.PetOwner == player && p.Monster is NaiWaEggMinion);
        if (eggAlive) return false;

        // 非蛋随从之间：后召唤先挡（SummonOrder 大的优先）
        var myOrder = ((MinionModel)myMonster).SummonOrder;
        return !player.Creature.CombatState.Allies.Any(p =>
            p.IsAlive && p != base.Owner
            && p.PetOwner == player
            && p.Monster is MinionModel m && m.SummonOrder > myOrder);
    }

    /// <summary>Kill all living pets owned by the player (deterministic on both host/client).</summary>
    private static void KillPetsForPlayer(Player player)
    {
        var pets = player.PlayerCombatState?.Pets.ToList();
        if (pets == null) return;
        foreach (var pet in pets)
        {
            if (pet.IsAlive)
                _ = CreatureCmd.Kill(pet);
        }
    }

    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Kill), typeof(Creature), typeof(bool))]
    private static class PlayerDeathPetKiller
    {
        static void Postfix(Creature creature)
        {
            // 只有"玩家本体"死亡才杀宠物。Creature.Player 是玩家对象（Player 不是 Creature 子类），
            // 旧代码 (Player)(object)creature 强转对任何 creature 都抛 InvalidCastException，
            // 沙虫吞下玩家后 SandpitPower.AfterRemoved → Kill 触发这里 → 打断 StartTurn → 回合卡死。
            // 随从/怪物 creature.Player 为 null，不会误触发。
            var player = creature.Player;
            if (player != null && player.Creature == creature)
            {
                KillPetsForPlayer(player);
            }
        }
    }

    /// <summary>
    /// 玩家被打死时同样杀宠物：战斗伤害致死走 Kill(IReadOnlyCollection) 集合重载
    /// （CreatureCmd.Damage 末尾 await Kill(killedCreatures)），单体重载的 Patch 不会触发，
    /// 漏掉会导致多人/单人战斗里玩家死亡后召唤物留在场上。两个重载必须都挂。
    /// </summary>
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Kill),
        typeof(IReadOnlyCollection<Creature>), typeof(bool))]
    private static class PlayerDeathPetKillerAll
    {
        static void Postfix(IReadOnlyCollection<Creature> creatures)
        {
            foreach (var creature in creatures)
            {
                var player = creature.Player;
                if (player != null && player.Creature == creature)
                {
                    KillPetsForPlayer(player);
                }
            }
        }
    }
}
