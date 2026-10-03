using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace NaiWa.powers;

/// <summary>
/// 奶蛇 - 回合结束时，给予所有敌人等同自身生命值的糖分。
/// </summary>
public sealed class NaiWaSnakeTankPower : NaiWaMinionPowerBase
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (side != base.Owner.Side) return;
        await TriggerTurnEffect(ctx, base.Owner.PetOwner);
    }

    /// <summary>每回合效果：给予所有敌人等同自身生命值的糖分。</summary>
    public override async Task TriggerTurnEffect(PlayerChoiceContext ctx, Player owner)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        var enemies = base.Owner.CombatState?.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies == null || enemies.Count == 0) return;
        await PowerCmd.Apply<NaiWaTangFenPower>(ctx, enemies, base.Owner.CurrentHp, base.Owner, null);
    }
}
