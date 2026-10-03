using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NaiWa.cards;

/// <summary>
/// 降糖 - 2费罕见攻击。造成12（16）点伤害，选择手牌中任意张糖化移除，
/// 每移除1张抽1张牌并额外造成5点伤害。
/// </summary>
public sealed class NaiWaJiangTang : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(12m, ValueProp.Move),
        new DynamicVar("ExtraDamage", 5m),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_jiangtang.png";

    public NaiWaJiangTang() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 选择手牌中任意张糖化移除，每移除1张抽1张牌并额外造成5点伤害
        var handTangHua = Owner.PlayerCombatState.Hand.Cards
            .Where(c => c is NaiWaTangHua)
            .Cast<NaiWaTangHua>()
            .ToList();
        int exhausted = 0;
        if (handTangHua.Count > 0)
        {
            var picked = (await CardSelectCmd.FromHand(
                prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 0, handTangHua.Count),
                context: choiceContext,
                player: Owner,
                filter: c => c is NaiWaTangHua,
                source: this)).Cast<NaiWaTangHua>().ToList();
            if (picked.Count > 0)
            {
                exhausted = picked.Count;
                await NaiWaTangHua.RemoveSpecific(choiceContext, Owner, picked);
                await CardPileCmd.Draw(choiceContext, exhausted, Owner);
            }
        }

        decimal damage = DynamicVars.Damage.BaseValue + exhausted * DynamicVars["ExtraDamage"].IntValue;
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); // 12→16
    }
}
