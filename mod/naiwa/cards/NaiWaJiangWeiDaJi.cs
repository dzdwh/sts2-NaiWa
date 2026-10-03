using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.minions;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 酱味打击 - 2费common攻击。造成10点伤害，额外造成等同于当前奶蛋生命值的伤害。
/// 奶蛋召唤4（升级8）。
/// </summary>
public sealed class NaiWaJiangWeiDaJi : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(10m),
        new ExtraDamageVar(1m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            (card, _) =>
            {
                var egg = card.Owner?.PlayerCombatState?.Pets
                    .FirstOrDefault(p => p.IsAlive && p.Monster is NaiWaEggMinion
                        && p.PetOwner == card.Owner);
                return egg?.CurrentHp ?? 0;
            }),
        new DynamicVar("EggHp", 4m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaEggTankPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_jiangweidaji.png";

    public NaiWaJiangWeiDaJi() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);

        await MinionCmd.AddMinion<NaiWaEggMinion>(ctx, Owner,
            new MinionSummonOptions(DynamicVars["EggHp"].IntValue, Source: this, Position: MinionPosition.Front));
    }

    protected override void OnUpgrade()
    {
        DynamicVars["EggHp"].UpgradeValueBy(4m); // 奶蛋召唤4→8
    }
}
