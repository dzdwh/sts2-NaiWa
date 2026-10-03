using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 朋友 - 2费rare能力牌。回合开始时将一张带有召唤效果的牌（升级后为升级版）加入手牌，该牌获得虚无。可叠加。
/// </summary>
public sealed class NaiWaPengYou : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Amount", 1m),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_dwpy.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        { HoverTipFactory.FromKeyword(NaiWaKeywords.YuanShu) };

    public NaiWaPengYou() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<NaiWaPengYouPower>(choiceContext, Owner.Creature,
            1, Owner.Creature, this);
        // 打出过升级版朋友 → 之后所有层给的召唤牌都是升级版
        var power = Owner.Creature.GetPower<NaiWaPengYouPower>();
        if (power != null && IsUpgraded)
            power.IsUpgraded = true;
    }

    protected override void OnUpgrade()
    {
        // 升级效果：加入的召唤牌变为升级版（由 Power 的 Amount 标记处理）
    }
}
