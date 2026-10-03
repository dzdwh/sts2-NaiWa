using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.minions;

namespace NaiWa.powers;

/// <summary>奶狮 — 回合结束时消耗自身1点最大生命值，然后对随机敌人造成等同于自身生命的伤害，每有一名随从额外一次。</summary>
public sealed class NaiWaLionTankPower : NaiWaMinionPowerBase
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (side != base.Owner.Side) return;
        await TriggerTurnEffect(ctx, base.Owner.PetOwner);
    }

    /// <summary>每回合效果：消耗1点最大生命，对随机敌人造成等同自身生命的伤害，每有一名随从额外一次。</summary>
    public override async Task TriggerTurnEffect(PlayerChoiceContext ctx, Player owner)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        var enemies = base.Owner.CombatState?.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies == null || enemies.Count == 0) return;
        var ownerPlayer = base.Owner.PetOwner;
        if (ownerPlayer == null) return;
        // 只数在场随从（被奶鲸吞下/离场残留的旧实例不参与）
        int hitCount = 1 + base.Owner.CombatState.Allies.Count(p =>
            p.IsAlive && p != base.Owner && p.PetOwner == ownerPlayer && p.Monster is MinionModel);

        // 前提：消耗1点最大生命值（只扣一次）
        await CreatureCmd.LoseMaxHp(ctx, base.Owner, 1, isFromCard: false);
        if (!base.Owner.IsAlive) return; // 消耗致死则不再造成伤害

        var rng = base.Owner.CombatState.RunState.Rng.CombatCardSelection;
        for (int i = 0; i < hitCount; i++)
        {
            var target = enemies[rng.NextInt(enemies.Count)];
            try
            {
                await CreatureCmd.Damage(ctx, target, (decimal)base.Owner.CurrentHp, ValueProp.Unpowered, base.Owner);
            }
            catch (Exception ex)
            {
                Log.Error($"[NaiWaLionTankPower] Damage failed: {ex.Message}");
            }
        }
    }
}
