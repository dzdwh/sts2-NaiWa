using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.cards;

/// <summary>
/// 惊鸿一瞥 - 1费uncommon技能牌（多人模式牌）。
/// 在所有玩家手牌中添加一张随机奶蛙食物（升级：升级过的奶蛙食物）。
/// </summary>
public sealed class NaiWaJingHongYiPie : CardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    /// <summary>奶蛙食物池（与鼠与粉一致）。</summary>
    private static readonly Type[] FoodPool =
    {
        typeof(NaiWaNailiu), typeof(NaiWaNaiTaoGuanTou), typeof(NaiWaNaiQiLin), typeof(NaiWaHuoBaoNaiJiao),
        typeof(NaiWaNaiGua), typeof(NaiWaMoLiNaiLv), typeof(NaiWaNaiYouGuo),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_jing_hong_yi_pie.png";

    public NaiWaJingHongYiPie() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var rng = Owner.RunState.Rng.CombatCardSelection;
        Type pickedType = FoodPool[rng.NextInt(FoodPool.Length)];

        foreach (var player in CombatState.Players)
        {
            CardModel food = pickedType switch
            {
                var t when t == typeof(NaiWaNailiu) => CombatState.CreateCard<NaiWaNailiu>(player),
                var t when t == typeof(NaiWaNaiTaoGuanTou) => CombatState.CreateCard<NaiWaNaiTaoGuanTou>(player),
                var t when t == typeof(NaiWaNaiQiLin) => CombatState.CreateCard<NaiWaNaiQiLin>(player),
                var t when t == typeof(NaiWaHuoBaoNaiJiao) => CombatState.CreateCard<NaiWaHuoBaoNaiJiao>(player),
                var t when t == typeof(NaiWaNaiGua) => CombatState.CreateCard<NaiWaNaiGua>(player),
                var t when t == typeof(NaiWaMoLiNaiLv) => CombatState.CreateCard<NaiWaMoLiNaiLv>(player),
                _ => CombatState.CreateCard<NaiWaNaiYouGuo>(player),
            };

            if (base.IsUpgraded)
                CardCmd.Upgrade(food);
            await CardPileCmd.AddGeneratedCardToCombat(food, PileType.Hand, player);
        }
    }

    protected override void OnUpgrade() { }
}
