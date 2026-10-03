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
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 奶国人 - 1费uncommon攻击牌。造成9/13点伤害。从抽牌堆选择1张牌加入手牌。获得10层糖分。
/// </summary>
public sealed class NaiWaNaiGuoRen : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_guo_ren.png";

    public NaiWaNaiGuoRen() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var picked = (await CardSelectCmd.FromCombatPile(
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
            context: choiceContext,
            pile: PileType.Draw.GetPile(Owner),
            player: Owner)).FirstOrDefault();
        if (picked != null)
            await CardPileCmd.Add(picked, PileType.Hand);

        await NaiWaTangHua.GiveToHand(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); // 10→14
    }
}
