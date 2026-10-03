using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NaiWa.cards;

/// <summary>
/// 钢背蛙 - 1费common技能。获得12/16点格挡。手牌中时必须优先打出（仿原版执迷）。
/// </summary>
public sealed class NaiWaGangBeiWa : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(12m, ValueProp.Move),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { NaiWaKeywords.Artist };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_gang_bei_wa.png";

    public NaiWaGangBeiWa() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["Block"].IntValue, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Block"].UpgradeValueBy(4m); // 12→16
    }

    /// <summary>手牌中时，必须先打出这张牌（仿原版执迷 Enthralled）。</summary>
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner != base.Owner) return true;
        if (base.Pile?.Type != PileType.Hand) return true;
        if (card is NaiWaGangBeiWa) return true;
        if (autoPlayType != AutoPlayType.None) return true;
        return false;
    }
}
