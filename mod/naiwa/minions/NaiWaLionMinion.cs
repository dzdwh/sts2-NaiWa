using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using NaiWa.powers;

namespace NaiWa.minions;

public sealed class NaiWaLionMinion : MinionModel
{
    public override int MinInitialHp => 9;
    public override int MaxInitialHp => 9;
    protected override string VisualsPath =>
        "res://scenes/creature_visuals/nai_wa_lion.tscn";

    public override async Task OnSummon(PlayerChoiceContext ctx, Player owner, MinionSummonOptions options)
    {
        var self = owner.PlayerCombatState?.Pets.FirstOrDefault(p =>
            p.Monster is NaiWaLionMinion && p.PetOwner == owner);
        if (self == null) return;
        if (options.MaxHp is decimal maxHp)
            await CreatureCmd.SetMaxAndCurrentHp(self, maxHp);
        await PowerCmd.Apply<NaiWaLionTankPower>(ctx, self, 1, owner.Creature, options.Source);
    }
}
