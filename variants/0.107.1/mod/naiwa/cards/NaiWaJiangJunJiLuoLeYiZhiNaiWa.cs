using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 将军击落了一只奶娃 - 2费rare能力牌。回合开始时，从抽牌堆选择一张牌加入手牌。升级-1费。
/// </summary>
public sealed class NaiWaJiangJunJiLuoLeYiZhiNaiWa : CardModel
{
    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_jiang_jun_ji_luo_le_yi_zhi_nai_wa.png";

    public NaiWaJiangJunJiLuoLeYiZhiNaiWa() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<NaiWaJiangJunJiLuoLeYiZhiNaiWaPower>(choiceContext, Owner.Creature,
            1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2→1
    }
}
