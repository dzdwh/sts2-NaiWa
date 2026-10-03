using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NaiWa.cards;

/// <summary>奶之手 — 0费先古攻击牌。失去1点最大生命值，对所有敌人造成14/18点伤害，抽2/3张牌。</summary>
public sealed class NaiWaNaiZhiShou : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new MaxHpVar(1m),
        new DamageVar(14m, ValueProp.Move),
        new CardsVar(2),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_zhi_shou.png";

    public NaiWaNaiZhiShou()
        : base(0, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.LoseMaxHp(
            choiceContext, Owner.Creature, DynamicVars.MaxHp.BaseValue, isFromCard: true);
        await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
