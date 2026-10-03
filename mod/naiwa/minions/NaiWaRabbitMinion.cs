using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using NaiWa.powers;

namespace NaiWa.minions;

public sealed class NaiWaRabbitMinion : MinionModel
{
    public override int MinInitialHp => 2;
    public override int MaxInitialHp => 2;
    protected override string VisualsPath =>
        "res://scenes/creature_visuals/nai_wa_rabbit.tscn";

    public override async Task OnSummon(PlayerChoiceContext ctx, Player owner, MinionSummonOptions options)
    {
        var self = owner.PlayerCombatState?.Pets.FirstOrDefault(p =>
            p.Monster is NaiWaRabbitMinion && p.PetOwner == owner);
        if (self == null) return;
        if (options.MaxHp is decimal maxHp)
            await CreatureCmd.SetMaxAndCurrentHp(self, maxHp);
        await PowerCmd.Apply<NaiWaRabbitTankPower>(ctx, self, 1, owner.Creature, options.Source);
        // Apply initial Dexterity to owner equal to rabbit's current HP
        await PowerCmd.Apply<DexterityPower>(ctx, owner.Creature, self.CurrentHp, self, options.Source);
    }
}
