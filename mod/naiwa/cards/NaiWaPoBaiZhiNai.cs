using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.minions;

namespace NaiWa.cards;

/// <summary>
/// 破败之奶 - 1费common攻击。造成5（7）点伤害。根据造成伤害值随机令一名随从增加等量生命值。
/// </summary>
public sealed class NaiWaPoBaiZhiNai : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(5m, ValueProp.Move),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_po_bai_zhi_nai.png";

    public NaiWaPoBaiZhiNai() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var cmd = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash");
        await cmd.Execute(choiceContext);

        // 与拳斗/奶龙同口径：随从获得的 HP = 实际攻击伤害（含被格挡的部分）
        int dmg = cmd.Results?.SelectMany(r => r).Sum(r => r.TotalDamage + r.OverkillDamage) ?? 0;
        if (dmg <= 0) return;

        // Target the last summoned pet (highest SummonOrder)
        var pet = Owner.Creature.Pets?.Where(p => p.IsAlive)
            .OrderByDescending(p => p.Monster is MinionModel m ? m.SummonOrder : 0)
            .FirstOrDefault();
        if (pet != null)
        {
            await CreatureCmd.GainMaxHp(pet, dmg);
            await MinionCmd.FireSummonEffect(Owner); // 破败之奶加最大生命值属于召唤效果
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 5→7
    }
}
