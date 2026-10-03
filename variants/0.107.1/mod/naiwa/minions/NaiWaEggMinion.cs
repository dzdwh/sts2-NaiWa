using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using NaiWa.powers;

namespace NaiWa.minions;

public sealed class NaiWaEggMinion : MinionModel
{
    public override int MinInitialHp => 6;
    public override int MaxInitialHp => 6;
    protected override string VisualsPath =>
        "res://scenes/creature_visuals/nai_wa_egg.tscn";

    public override async Task OnSummon(PlayerChoiceContext ctx, Player owner, MinionSummonOptions options)
    {
        var self = owner.PlayerCombatState?.Pets.FirstOrDefault(p =>
            p.Monster is NaiWaEggMinion && p.PetOwner == owner);
        Log.Info($"[EggMinion.OnSummon] self={self} petsCount={owner.PlayerCombatState?.Pets.Count}");
        if (self == null) { Log.Warn("[EggMinion.OnSummon] self is null, cannot apply powers"); return; }
        if (options.MaxHp is decimal maxHp)
            await CreatureCmd.SetMaxAndCurrentHp(self, maxHp);
        await PowerCmd.Apply<NaiWaEggTankPower>(ctx, self, 1, owner.Creature, options.Source);
    }
}
