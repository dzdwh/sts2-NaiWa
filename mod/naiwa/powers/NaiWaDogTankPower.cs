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

/// <summary>回合结束时，治疗其他召唤物自身生命值的生命（不加生命上限）。</summary>
public sealed class NaiWaDogTankPower : NaiWaMinionPowerBase
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (side != base.Owner.Side) return;
        await TriggerTurnEffect(ctx, base.Owner.PetOwner);
    }

    /// <summary>每回合效果：治疗其他随从等同自身生命值的生命。</summary>
    public override async Task TriggerTurnEffect(PlayerChoiceContext ctx, Player owner)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        var ownerPlayer = base.Owner.PetOwner;
        if (ownerPlayer == null) return;
        var healAmount = base.Owner.CurrentHp;
        foreach (var pet in ownerPlayer.Creature.Pets)
        {
            if (pet != base.Owner && pet.IsAlive)
                await CreatureCmd.Heal(pet, healAmount);
        }
    }
}
