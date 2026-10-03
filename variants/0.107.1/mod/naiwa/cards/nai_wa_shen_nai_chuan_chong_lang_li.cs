using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.cards;

/// <summary>
/// 神奶川冲浪里 - 1费rare技能牌。选择一张手牌，本场战斗该牌耗能-1。消耗。升级去掉消耗。
/// </summary>
public sealed class NaiWaShenNaiChuanChongLangLi : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar("Reduce", 1),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Exhaust,
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_shen_nai_chuan_chong_lang_li.png";

    public NaiWaShenNaiChuanChongLangLi() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        if (!PileType.Hand.GetPile(Owner).Cards.Any())
            return;

        var picked = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
            context: choiceContext,
            player: Owner,
            filter: null,
            source: this)).FirstOrDefault();
        if (picked != null)
            picked.EnergyCost.AddThisCombat(-1, reduceOnly: true);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
