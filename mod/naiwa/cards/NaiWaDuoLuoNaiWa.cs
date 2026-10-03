using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 堕落奶蛙 - 2费rare能力牌。每当你大笑1次，抽1张牌。升级1费。
/// </summary>
public sealed class NaiWaDuoLuoNaiWa : CardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaLaughPower>(),
        HoverTipFactory.FromPower<NaiWaLaughCardPower>(),
        HoverTipFactory.FromPower<NaiWaDuoLuoNaiWaPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_duo_luo_nai_wa.png";

    public NaiWaDuoLuoNaiWa() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<NaiWaDuoLuoNaiWaPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2→1
    }
}
