using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using NaiWa.powers;

namespace NaiWa.minions;

public sealed class NaiWaSnakeMinion : MinionModel
{
    public override int MinInitialHp => 4;
    public override int MaxInitialHp => 4;
    protected override string VisualsPath =>
        "res://scenes/creature_visuals/nai_wa_snake.tscn";

    public override async Task OnSummon(PlayerChoiceContext ctx, Player owner, MinionSummonOptions options)
    {
        var self = owner.PlayerCombatState?.Pets.FirstOrDefault(p =>
            p.Monster is NaiWaSnakeMinion && p.PetOwner == owner);
        if (self == null) return;
        if (options.MaxHp is decimal maxHp)
            await CreatureCmd.SetMaxAndCurrentHp(self, maxHp);
        await PowerCmd.Apply<NaiWaSnakeTankPower>(ctx, self, 1, owner.Creature, options.Source);
    }
}
