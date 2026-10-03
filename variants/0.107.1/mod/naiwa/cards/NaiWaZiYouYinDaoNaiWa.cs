using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 自由引导奶蛙 - 2费rare能力牌。回合开始时选择一张手牌：不是大笑牌则令其获得大笑牌，否则打出这张牌。升级获得固有。
/// </summary>
public sealed class NaiWaZiYouYinDaoNaiWa : CardModel
{
    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_zi_you_yin_dao_nai_wa.png";

    public NaiWaZiYouYinDaoNaiWa() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<NaiWaZiYouYinDaoNaiWaPower>(choiceContext, Owner.Creature,
            1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate); // 升级获得固有
    }
}
