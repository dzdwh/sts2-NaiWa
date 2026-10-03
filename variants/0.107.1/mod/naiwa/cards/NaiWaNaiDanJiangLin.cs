using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa.minions;

using NaiWa.powers;
namespace NaiWa.cards;

public sealed class NaiWaNaiDanJiangLin : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("EggHp", 6m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaEggTankPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_naidan_jianglin.png";

    public NaiWaNaiDanJiangLin() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var hp = DynamicVars["EggHp"].IntValue;
        await MinionCmd.AddMinion<NaiWaEggMinion>(ctx, Owner,
            new MinionSummonOptions(hp, Source: this, Position: MinionPosition.FrontUpper));
    }

    protected override void OnUpgrade()
    {
        DynamicVars["EggHp"].UpgradeValueBy(3m); // 6→9
    }
}
