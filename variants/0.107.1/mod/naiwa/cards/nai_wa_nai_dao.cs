using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 道奶 - 1费uncommon攻击牌。对所有敌人造成7点伤害，
/// 手牌中已有至少3张糖化时额外造成1次伤害；将1张糖化加入手牌。
/// 升级：造成2次伤害（伤害数值不随升级提高）。
/// </summary>
public sealed class NaiWaNaiDao : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(7m, ValueProp.Move),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_dao.png";

    /// <summary>手牌中已有至少3张糖化时高亮（本次获得的糖化不计入）。</summary>
    protected override bool ShouldGlowGoldInternal =>
        Owner != null && NaiWaTangHua.CountInHand(Owner) >= 3;

    public NaiWaNaiDao() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);

        // 基础1次 + 升级+1次 + 手牌中每有3张糖化再+1次（本次获得的不计入）
        int hits = 1 + (IsUpgraded ? 1 : 0) + NaiWaTangHua.CountInHand(Owner) / 3;
        await NaiWaTangHua.GiveToHand(choiceContext, Owner);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级效果：伤害次数 1→2（伤害数值不变）
    }
}
