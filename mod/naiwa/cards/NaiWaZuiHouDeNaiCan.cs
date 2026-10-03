using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 最后的奶餐 - 2费rare能力牌。回合开始时，将一张奶蛙食物加入手牌。升级后加入升级版食物。
/// </summary>
public sealed class NaiWaZuiHouDeNaiCan : CardModel
{
    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_zui_hou_de_nai_can.png";

    public NaiWaZuiHouDeNaiCan() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<NaiWaZuiHouDeNaiCanPower>(choiceContext, Owner.Creature,
            base.IsUpgraded ? 2 : 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() { }
}
