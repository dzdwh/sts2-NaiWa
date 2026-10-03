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

/// <summary>奶鸡和蛋 - 2费rare技能牌。召唤奶鸡5/10HP。</summary>
public sealed class NaiWaNaiJiHeDan : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("JiHp", 5m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaNaiJiTankPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_ji_he_dan.png";

    public NaiWaNaiJiHeDan() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var hp = DynamicVars["JiHp"].IntValue;
        await MinionCmd.AddMinion<NaiWaNaiJiMinion>(ctx, Owner,
            new MinionSummonOptions(hp, Source: this, Position: MinionPosition.FrontUpper));
    }

    protected override void OnUpgrade()
    {
        DynamicVars["JiHp"].UpgradeValueBy(5m); // 5→10
    }
}
