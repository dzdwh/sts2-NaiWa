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
/// 四小奶鹅 - 4费rare技能牌。选择一张手牌获得重放4。消耗（升级去掉）。
/// 仿原版"重构"：BaseReplayCount += 4。
/// </summary>
public sealed class NaiWaSiXiaoNaiE : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_si_xiao_nai_e.png";

    public NaiWaSiXiaoNaiE() : base(4, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var picked = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
            context: choiceContext,
            player: Owner,
            filter: null, // 可与已有重放（含附魔重放）叠加
            source: this)).FirstOrDefault();
        if (picked != null)
            picked.BaseReplayCount += 3;

        // 将1张糖化分别加入抽牌堆、手牌、弃牌堆和消耗牌堆
        await NaiWaTangHua.GiveToDrawPile(choiceContext, Owner);
        await NaiWaTangHua.GiveToHand(choiceContext, Owner);
        await NaiWaTangHua.GiveToDiscard(choiceContext, Owner);
        await NaiWaTangHua.GiveToExhaustPile(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
