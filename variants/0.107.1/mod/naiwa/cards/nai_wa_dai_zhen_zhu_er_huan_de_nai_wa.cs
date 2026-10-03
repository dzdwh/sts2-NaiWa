using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 带珍珠耳环的奶娃 - 3费rare攻击牌。大笑牌。
/// 造成3点伤害6次，获得1层无实体，消耗。升级-1费。
/// </summary>
public sealed class NaiWaDaiZhenZhuErHuanDeNaiWa : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Move),
        new RepeatVar(6),
        new PowerVar<IntangiblePower>(1m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Exhaust,
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaLaughPower>(),
        HoverTipFactory.FromPower<NaiWaLaughCardPower>(),
        HoverTipFactory.FromPower<IntangiblePower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_dai_zhen_zhu_er_huan_de_nai_wa.png";

    public NaiWaDaiZhenZhuErHuanDeNaiWa() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);
        if (await NaiWaXiao.TriggerLaughTracking(choiceContext, Owner))
            NaiWaXiao.TriggerLaughVisual(Owner);

        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await PowerCmd.Apply<IntangiblePower>(choiceContext, Owner.Creature,
            DynamicVars["IntangiblePower"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 3→2
    }
}
