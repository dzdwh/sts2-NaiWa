using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NaiWa.cards;

public sealed class NaiWaBaiRiMeng : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(26m, ValueProp.Move),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_brm.png";

    public NaiWaBaiRiMeng() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);

        // 用糖化填满手牌（模仿原版迫降 CrashLanding）
        int fill = CardPile.MaxCardsInHand - Owner.PlayerCombatState.Hand.Cards.Count;
        if (fill > 0)
            await NaiWaTangHua.GiveToHand(ctx, Owner, fill);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m); // 26→34
    }
}
