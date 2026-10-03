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
/// 铸星奶王 - 1费uncommon攻击牌。大笑牌。造成9点伤害。
/// 将一张随机大笑牌（升级后为升级过的大笑牌）添加到手牌。
/// </summary>
public sealed class NaiWaZhuXingNaiWang : CardModel
{
    /// <summary>可随机生成的大笑牌池（排除远古大笑和自身，避免退化循环）。</summary>
    private static readonly Type[] LaughPool =
    {
        typeof(NaiWaXiao), typeof(NaiWaChongCi), typeof(NaiWaHeHuNaiLi), typeof(NaiWaPengFu),
        typeof(NaiWaWanMeiNaiJi), typeof(NaiWaZhongBin),
        typeof(NaiWaDaiZhenZhuErHuanDeNaiWa),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(9m, ValueProp.Move),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaLaughPower>(),
        HoverTipFactory.FromPower<NaiWaLaughCardPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_zhu_xing_nai_wang.png";

    public NaiWaZhuXingNaiWang() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);
        if (await NaiWaXiao.TriggerLaughTracking(choiceContext, Owner))
            NaiWaXiao.TriggerLaughVisual(Owner);

        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 随机一张大笑牌，升级后生成升级版，加入手牌
        var rng = Owner.RunState.Rng.CombatCardSelection;
        Type pickedType = LaughPool[rng.NextInt(LaughPool.Length)];
        CardModel card = pickedType switch
        {
            var t when t == typeof(NaiWaXiao) => CombatState.CreateCard<NaiWaXiao>(Owner),
            var t when t == typeof(NaiWaChongCi) => CombatState.CreateCard<NaiWaChongCi>(Owner),
            var t when t == typeof(NaiWaHeHuNaiLi) => CombatState.CreateCard<NaiWaHeHuNaiLi>(Owner),
            var t when t == typeof(NaiWaPengFu) => CombatState.CreateCard<NaiWaPengFu>(Owner),
            var t when t == typeof(NaiWaWanMeiNaiJi) => CombatState.CreateCard<NaiWaWanMeiNaiJi>(Owner),
            var t when t == typeof(NaiWaZhongBin) => CombatState.CreateCard<NaiWaZhongBin>(Owner),
            _ => CombatState.CreateCard<NaiWaDaiZhenZhuErHuanDeNaiWa>(Owner),
        };

        if (base.IsUpgraded)
            CardCmd.Upgrade(card);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() { }
}
