using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using NaiWa.cards;
using NaiWa.powers;

namespace NaiWa.minions;

public sealed class NaiWaMouseMinion : MinionModel
{
    public override int MinInitialHp => 2;
    public override int MaxInitialHp => 2;
    protected override string VisualsPath =>
        "res://scenes/creature_visuals/nai_wa_mouse.tscn";

    public override async Task OnSummon(PlayerChoiceContext ctx, Player owner, MinionSummonOptions options)
    {
        var self = owner.PlayerCombatState?.Pets.FirstOrDefault(p =>
            p.Monster is NaiWaMouseMinion && p.PetOwner == owner);
        Log.Info($"[MouseMinion.OnSummon] self={self} petsCount={owner.PlayerCombatState?.Pets.Count}");
        if (self == null) { Log.Warn("[MouseMinion.OnSummon] self is null, cannot apply powers"); return; }
        if (options.MaxHp is decimal maxHp)
            await CreatureCmd.SetMaxAndCurrentHp(self, maxHp);
        await PowerCmd.Apply<NaiWaMouseBuffPower>(ctx, self, 1, owner.Creature, options.Source);
        // 把卡牌引用传给 Power，用于跨战斗持久化 HP
        if (options.Source is NaiWaYongYuanDeNaiShu card)
        {
            var buff = self.GetPowerInstances<NaiWaMouseBuffPower>().FirstOrDefault();
            if (buff != null) buff.SourceCard = card;
            if (buff != null) buff.SetAccumulatedContribution(card, options.AccumulatedHp);
        }
    }
}
