using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 奶祷 - 0费common技能牌。获得1张糖化，将弃牌堆一张牌放入手牌。升级后不消耗。
/// </summary>
public sealed class NaiWaDao : CardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Exhaust,
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_dao.png";

    public NaiWaDao() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await NaiWaTangHua.GiveToHand(choiceContext, Owner);

        // Select a card from discard pile
        var discard = PileType.Discard.GetPile(Owner);
        if (discard.Cards.Any())
        {
            var prompt = new LocString("cards", "NAI_WA_DAO.selectionScreenPrompt");
            var cards = await CardSelectCmd.FromCombatPile(
                choiceContext, discard, Owner,
                new CardSelectorPrefs(prompt, 1));
            var card = cards.FirstOrDefault();
            if (card != null)
                await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
