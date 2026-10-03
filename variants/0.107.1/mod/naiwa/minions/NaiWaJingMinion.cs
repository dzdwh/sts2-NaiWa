using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using NaiWa.powers;

namespace NaiWa.minions;

public sealed class NaiWaJingMinion : MinionModel
{
    public override int MinInitialHp => 1;
    public override int MaxInitialHp => 1;
    protected override string VisualsPath =>
        "res://scenes/creature_visuals/nai_wa_jing.tscn";

    public override async Task OnSummon(PlayerChoiceContext ctx, Player owner, MinionSummonOptions options)
    {
        // 奶鲸可能离场后残留旧实例在 _pets，必须取最新加入的（末尾）而不是第一个
        var self = owner.PlayerCombatState?.Pets.LastOrDefault(p =>
            p.Monster is NaiWaJingMinion && p.PetOwner == owner);
        if (self == null) return;
        if (options.MaxHp is decimal maxHp)
            await CreatureCmd.SetMaxAndCurrentHp(self, maxHp);
        await PowerCmd.Apply<NaiWaJingTankPower>(ctx, self, 1, owner.Creature, options.Source);
        NaiWaJingTankPower.UpdateScale(self);
    }
}
