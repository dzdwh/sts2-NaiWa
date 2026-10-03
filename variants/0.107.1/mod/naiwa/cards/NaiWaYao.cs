using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa.minions;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 奶咬 - 1费uncommon保留。召唤奶蛇，你每有10层糖分额外召唤1。升级基础生命5→8。
/// </summary>
public sealed class NaiWaYao : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("BaseHp", 6m),
        new DynamicVar("PerTangHua", 1m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaSnakeTankPower>(),
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_yao.png";

    public NaiWaYao() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var baseHp = DynamicVars["BaseHp"].IntValue;
        var perTangHua = DynamicVars["PerTangHua"].IntValue;
        int tangHuaCount = NaiWaTangHua.CountInHand(Owner);
        var hp = baseHp + tangHuaCount * perTangHua;

        await MinionCmd.AddMinion<NaiWaSnakeMinion>(ctx, Owner,
            new MinionSummonOptions(hp, Source: this, Position: MinionPosition.Front));

        // Give 1 TangHua after summon (counts for next summon)
        await NaiWaTangHua.GiveToHand(ctx, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PerTangHua"].UpgradeValueBy(1m); // 1->2
    }
}
