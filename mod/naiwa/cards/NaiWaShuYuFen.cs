using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.cards;

/// <summary>
/// 鼠与粉 - 1费common技能。消耗。消耗1张手牌，随机将1张奶蛙食物加入手牌。升级去掉消耗。
/// </summary>
public sealed class NaiWaShuYuFen : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_shu_yu_fen.png";

    public NaiWaShuYuFen() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // Consume 1 hand card
        var picked = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            context: choiceContext,
            player: Owner,
            filter: null,
            source: this)).FirstOrDefault();

        if (picked != null)
            await CardCmd.Exhaust(choiceContext, picked);

        // Choose 1 of 3 random 奶蛙食物
        var rng = Owner.RunState.Rng.CombatCardSelection;
        var pick = new[] { 0, 1, 2, 3, 4, 5, 6 };
        rng.Shuffle(pick);
        CardModel MakeFood(int index) => index switch
        {
            0 => CombatState.CreateCard<NaiWaNailiu>(Owner),
            1 => CombatState.CreateCard<NaiWaNaiTaoGuanTou>(Owner),
            2 => CombatState.CreateCard<NaiWaNaiQiLin>(Owner),
            3 => CombatState.CreateCard<NaiWaHuoBaoNaiJiao>(Owner),
            4 => CombatState.CreateCard<NaiWaNaiGua>(Owner),
            5 => CombatState.CreateCard<NaiWaMoLiNaiLv>(Owner),
            _ => CombatState.CreateCard<NaiWaNaiYouGuo>(Owner),
        };
        var options = pick.Take(3).Select(MakeFood).ToList();
        var chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner, canSkip: true);
        if (chosen != null)
            await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
