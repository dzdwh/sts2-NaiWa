using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NaiWa.cards;

/// <summary>
/// 奶仆 - 2费uncommon攻击牌。随机对敌人造成5点伤害3次。
/// 如果本回合打出过带召唤效果的牌，这张牌的耗能降为0。
/// </summary>
public sealed class NaiWaNaiPu : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(5m, ValueProp.Move),
        new RepeatVar(3),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_pu.png";

    public NaiWaNaiPu() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.None) { }

    /// <summary>本回合打出过带召唤效果的牌 → 实际耗能 0（权威扣费走这里，双端一致）。</summary>
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this || Owner == null) return false;
        if (!NaiWaNaiJiao.HasPlayedSummonCardThisTurn(Owner)) return false;
        modifiedCost = 0m;
        return true;
    }

    /// <summary>抽到时若已打出过带召唤效果的牌，直接显示 0 费（同奶怒做法）。</summary>
    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card == this && NaiWaNaiJiao.HasPlayedSummonCardThisTurn(Owner))
            EnergyCost.SetThisTurn(0);
        return Task.CompletedTask;
    }

    protected override bool ShouldGlowGoldInternal => NaiWaNaiJiao.HasPlayedSummonCardThisTurn(Owner);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain); // 获得保留
    }
}
