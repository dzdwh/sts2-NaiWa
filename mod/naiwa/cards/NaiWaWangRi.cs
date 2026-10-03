using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 往日 - 5费uncommon攻击牌。造成63点伤害。
/// 可被多次升级（仿灼热攻击）：每次升级耗能-1；耗能为0后，每次升级额外+12伤害。
/// </summary>
public sealed class NaiWaWangRi : CardModel
{
    /// <summary>可无限升级：火堆可以反复升级（每次升级都会调用 OnUpgrade）。</summary>
    public override int MaxUpgradeLevel => int.MaxValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(63m, ValueProp.Move),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => System.Array.Empty<IHoverTip>();

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_wang_ri.png";

    public NaiWaWangRi() : base(5, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 每次升级耗能-1；耗能为0后改为 +12 伤害（UpgradeBy 本身会钳制到 0）
        if (EnergyCost.GetWithModifiers(CostModifiers.None) > 0)
            EnergyCost.UpgradeBy(-1);
        else
            DynamicVars["Damage"].UpgradeValueBy(12m);
    }

    protected override void AfterDowngraded()
    {
        // 降级补丁保留剩余等级；原版此时已重置数值，附魔/异变尚未重新应用。
        EnergyCost.UpgradeBy(-Math.Min(CurrentUpgradeLevel, 5));
        DynamicVars.Damage.BaseValue += Math.Max(CurrentUpgradeLevel - 5, 0) * 12m;
        FinalizeUpgradeInternal();
    }
}
