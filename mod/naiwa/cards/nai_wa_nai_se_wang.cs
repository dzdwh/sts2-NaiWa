using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.minions;

namespace NaiWa.cards;

/// <summary>
/// 奶瑟王 - 1费rare攻击牌。造成4点伤害2/3次。
/// 伤害额外提升你所有随从的平均生命值。
/// </summary>
public sealed class NaiWaNaiSeWang : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(4m),
        new ExtraDamageVar(1m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            (card, _) =>
            {
                var owner = card.Owner;
                if (owner?.Creature?.CombatState == null) return 0m;
                // 只统计奶蛙自己的召唤物（MinionModel）；异鸟/佩尔的士兵等官方 Pet 不算
                // （同奶鲸修复：官方 Pet 可能有异常血量，如异鸟 10000 HP）
                var pets = owner.Creature.CombatState.Allies
                    .Where(a => a.IsAlive && a.PetOwner == owner && a.Monster is MinionModel)
                    .ToList();
                if (pets.Count == 0) return 0m;
                return pets.Sum(p => p.CurrentHp) / pets.Count;
            }),
        new RepeatVar(2),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_se_wang.png";

    public NaiWaNaiSeWang() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1m); // 2→3
    }
}
