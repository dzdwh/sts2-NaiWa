using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NaiWa.cards;

namespace NaiWa.powers;

/// <summary>
/// 朋友 - 回合开始时将一张带有召唤效果的牌或其升级版加入手牌，该牌获得虚无。
/// Counter 叠加：每层每回合给 1 张。
/// </summary>
public sealed class NaiWaPengYouPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>是否给升级版召唤牌（打出过升级版朋友后永久生效，双端执行一致）。</summary>
    public bool IsUpgraded;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        await base.BeforeHandDraw(player, choiceContext, combatState);
        if (player != base.Owner?.Player || base.Owner == null || !base.Owner.IsAlive) return;

        int count = Math.Max(1, base.Amount); // 每层每回合给 1 张
        for (int i = 0; i < count; i++)
        {
            var card = CreateSummonCard(player);
            if (IsUpgraded && card.IsUpgradable)
                CardCmd.Upgrade(card);
            CardCmd.ApplyKeyword(card, CardKeyword.Ethereal); // 给的牌获得虚无
            await CardPileCmd.AddGeneratedCardsToCombat(new[] { card }, PileType.Hand, player);
        }
        Flash();
    }

    private CardModel CreateSummonCard(Player player)
    {
        // 注意：新增召唤牌时需保证已注册进 NaiWaCardPool，
        // 否则 Card.Pool 会查进 MockCardPool 抛 "You monster!" 卡死回合开始。
        var summonTypes = new[]
        {
            typeof(NaiWaJiangWeiDaJi), typeof(NaiWaNaiDanJiangLin),
            typeof(NaiWaNaiGouWo), typeof(NaiWaNaiJiHeDan),
            typeof(NaiWaNaiLong), typeof(NaiWaXiaoNaiZhu), typeof(NaiWaYao), typeof(NaiWaYongYuanDeNaiShu),
        };
        var rng = player.RunState.Rng.CombatCardSelection;
        return summonTypes[rng.NextInt(summonTypes.Length)] switch
        {
            var t when t == typeof(NaiWaJiangWeiDaJi) => CombatState.CreateCard<NaiWaJiangWeiDaJi>(player),
            var t when t == typeof(NaiWaNaiDanJiangLin) => CombatState.CreateCard<NaiWaNaiDanJiangLin>(player),
            var t when t == typeof(NaiWaNaiGouWo) => CombatState.CreateCard<NaiWaNaiGouWo>(player),
            var t when t == typeof(NaiWaNaiJiHeDan) => CombatState.CreateCard<NaiWaNaiJiHeDan>(player),
            var t when t == typeof(NaiWaNaiLong) => CombatState.CreateCard<NaiWaNaiLong>(player),
            var t when t == typeof(NaiWaXiaoNaiZhu) => CombatState.CreateCard<NaiWaXiaoNaiZhu>(player),
            var t when t == typeof(NaiWaYao) => CombatState.CreateCard<NaiWaYao>(player),
            _ => CombatState.CreateCard<NaiWaYongYuanDeNaiShu>(player),
        };
    }
}
