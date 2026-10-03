using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 抽象主义 - 1费uncommon能力牌。每消耗1张糖化，对随机敌人造成等同于当前消耗堆中糖化数量的伤害。
/// 升级：每回合向消耗堆添加1张糖化。
/// </summary>
public sealed class NaiWaChouXiang : CardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_chouxiang.png";

    public NaiWaChouXiang() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<NaiWaChouXiangPower>(choiceContext, Owner.Creature,
            IsUpgraded ? 2 : 1, Owner.Creature, this); // Amount 2 = 升级（每回合向消耗堆加糖化）
    }

    protected override void OnUpgrade()
    {
        // 升级效果由 Power 的 Amount==2 触发：每回合向消耗堆添加1张糖化
    }
}
