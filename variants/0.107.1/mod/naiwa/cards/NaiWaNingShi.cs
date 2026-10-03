using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 凝视 - 1费uncommon技能。给予一名敌人糖分等于你当前糖分，获得10糖分。升级0费。
/// </summary>
/// <summary>
/// 凝视用的糖分数值：基础 5，手牌中每有 1 张糖化翻倍，卡面实时显示最终值。
/// </summary>
public sealed class NaiWaTangFenGiveVar : DynamicVar
{
    public NaiWaTangFenGiveVar(decimal baseSugar)
        : base("TangFen", baseSugar) { }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        decimal value = BaseValue;
        int count = NaiWaTangHua.CountInHand(card.Owner);
        for (int i = 0; i < count; i++)
            value *= 2;
        base.PreviewValue = value;
    }
}

public sealed class NaiWaNingShi : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new NaiWaTangFenGiveVar(5m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaTangFenPower>(),
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_ningshi.png";

    public NaiWaNingShi() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 给予5层糖分，手牌中每有1张糖化数值翻倍
        int sugar = DynamicVars["TangFen"].IntValue;
        int tangHuaCount = NaiWaTangHua.CountInHand(Owner);
        for (int i = 0; i < tangHuaCount; i++)
            sugar *= 2;

        await PowerCmd.Apply<NaiWaTangFenPower>(choiceContext, cardPlay.Target!,
            sugar, Owner.Creature, this);

        // Also gain 1 糖化
        await NaiWaTangHua.GiveToHand(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust); // 升级去掉消耗
    }
}
