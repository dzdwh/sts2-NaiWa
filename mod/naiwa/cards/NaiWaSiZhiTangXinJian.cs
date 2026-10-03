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
/// 四指糖心剑 - 1费rare攻击。打出时储存糖分，每次打出额外造成已储存糖分伤害，每第4次打出后重置。
/// </summary>
public sealed class NaiWaSiZhiTangXinJian : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(12m, ValueProp.Move),
        new DynamicVar("StoredPerTangHua", 10m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaTangFenPower>(),
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    /// <summary>Glow when the next play is the 4th (extra damage trigger).</summary>
    protected override bool ShouldGlowGoldInternal =>
        Owner?.Creature?.GetPower<NaiWaSiZhiTangXinJianPower>()?.Amount == 3;

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_wuzhi_tang_xin_jian.png";

    public NaiWaSiZhiTangXinJian() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var power = Owner.Creature.GetPower<NaiWaSiZhiTangXinJianPower>();
        if (power == null)
            power = await PowerCmd.Apply<NaiWaSiZhiTangXinJianPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        else
            await PowerCmd.ModifyAmount(ctx, power, 1, Owner.Creature, this);

        int playCount = power?.Amount ?? 1;

        // 打出时，手牌中每有1张糖化，储存10点伤害
        if (power != null)
            power.StoredSugar += DynamicVars["StoredPerTangHua"].IntValue * NaiWaTangHua.CountInHand(Owner);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);

        // 每次打出都额外造成已储存的伤害
        if (power != null && power.StoredSugar > 0)
        {
            await DamageCmd.Attack((decimal)power.StoredSugar)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target!)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(ctx);
        }

        // 每第4次打出后重置已储存伤害与打出次数
        if (playCount >= 4 && power != null)
        {
            power.StoredSugar = 0;
            await PowerCmd.ModifyAmount(ctx, power, -(playCount - 1), Owner.Creature, this);
        }

        await NaiWaTangHua.GiveToHand(ctx, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); // 12→16
    }
}
