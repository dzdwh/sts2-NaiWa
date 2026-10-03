using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>完美奶击 — 模仿完美打击，统计牌组中大笑牌数量。</summary>
public sealed class NaiWaWanMeiNaiJi : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(16m),
        new ExtraDamageVar(2m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            (card, _) => card.Owner.PlayerCombatState.AllCards.Count(
                c => NaiWaKeywords.IsLaughCard(c))),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaLaughPower>(),
        HoverTipFactory.FromPower<NaiWaLaughCardPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_wnj.png";

    public NaiWaWanMeiNaiJi() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (await NaiWaXiao.TriggerLaughTracking(ctx, Owner))
            NaiWaXiao.TriggerLaughVisual(Owner);
        await Task.Delay(300);
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(5m); // 16→21
        DynamicVars.ExtraDamage.UpgradeValueBy(1m); // 2→3
    }
}
