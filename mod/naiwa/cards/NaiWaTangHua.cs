using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using NaiWa.NaiWapools;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 糖化 - 0费Token技能牌。无法打出。保留。不能升级。
/// </summary>
public sealed class NaiWaTangHua : CardModel
{
    /// <summary>本回合每个玩家获得/失去糖化的次数（用于奶神赋等）。</summary>
    private static readonly Dictionary<Player, (int Turn, int Count)> _gainLossCountThisTurn = new();

    /// <summary>本场战斗每个玩家累计获得/失去糖化的次数（用于奶神赋等）。</summary>
    private static readonly Dictionary<Player, int> _gainLossCountCombat = new();

    /// <summary>本回合是否获得过糖化（用于思维奶化等）。</summary>
    private static readonly Dictionary<Player, (int Turn, bool Gained)> _gainedThisTurn = new();

    /// <summary>战斗开始/结束时清空所有糖化计数（防跨战斗残留）。</summary>
    public static void ClearCombat()
    {
        _gainLossCountThisTurn.Clear();
        _gainLossCountCombat.Clear();
        _gainedThisTurn.Clear();
    }

    /// <summary>新战斗开始时清空（与 ClearCombat 相同，语义区分开）。</summary>
    public static void ClearForNewCombat() => ClearCombat();

    /// <summary>回合开始时清空该玩家的糖化计数（在玩家回合开始 hook 里调用）。</summary>
    public static void ResetGainLossForTurn(Player player)
    {
        if (player != null)
        {
            _gainLossCountThisTurn.Remove(player);
            _gainedThisTurn.Remove(player);
        }
    }

    /// <summary>本回合该玩家是否获得过糖化。</summary>
    public static bool HasGainedThisTurn(Player player)
    {
        if (player?.PlayerCombatState == null) return false;
        var (turn, gained) = _gainedThisTurn.GetValueOrDefault(player);
        return turn == player.PlayerCombatState.TurnNumber && gained;
    }

    /// <summary>本回合该玩家获得或失去糖化的次数。</summary>
    public static int GetGainLossCountThisTurn(Player player)
    {
        if (player?.PlayerCombatState == null) return 0;
        var (turn, count) = _gainLossCountThisTurn.GetValueOrDefault(player);
        return turn == player.PlayerCombatState.TurnNumber ? count : 0;
    }

    /// <summary>本场战斗该玩家获得或失去糖化的累计次数。</summary>
    public static int GetGainLossCountCombat(Player player)
    {
        return player != null ? _gainLossCountCombat.GetValueOrDefault(player, 0) : 0;
    }

    private static void CountGainLoss(Player player)
    {
        if (player != null)
        {
            var (_, count) = _gainLossCountThisTurn.GetValueOrDefault(player);
            _gainLossCountThisTurn[player] = (player.PlayerCombatState?.TurnNumber ?? 0, count + 1);
            _gainLossCountCombat[player] = _gainLossCountCombat.GetValueOrDefault(player, 0) + 1;
        }
    }

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_tang_hua.png";

    public override CardPoolModel VisualCardPool => ModelDb.CardPool<NaiWaCardPool>();

    /// <summary>给玩家手牌加入 count 张糖化 token（糖化计数 = 手牌中糖化 token 数量）。</summary>
    public static async Task GiveToHand(PlayerChoiceContext ctx, Player player, int count = 1)
    {
        if (player?.Creature == null || player.Creature.CombatState == null) return;
        for (int i = 0; i < count; i++)
        {
            var card = player.Creature.CombatState.CreateCard<NaiWaTangHua>(player);
            Log.Info($"[TangHua] GiveToHand player={player.NetId} inProgress={CombatManager.Instance.IsInProgress} dead={player.Creature.IsDead} combatStateNull={(player.Creature.CombatState == null)} cardPile={(card.Pile?.Type.ToString() ?? "null")} removed={card.HasBeenRemovedFromState}");
            var result = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            Log.Info($"[TangHua] GiveToHand result success={result.success} cardPile={(card.Pile?.Type.ToString() ?? "null")}");
            CountGainLoss(player);
            _gainedThisTurn[player] = (player.PlayerCombatState?.TurnNumber ?? 0, true);
        }
    }

    /// <summary>给玩家弃牌堆加入 count 张糖化 token（获得计数与 GiveToHand 一致）。</summary>
    public static async Task GiveToDiscard(PlayerChoiceContext ctx, Player player, int count = 1)
    {
        if (player?.Creature == null || player.Creature.CombatState == null) return;
        for (int i = 0; i < count; i++)
        {
            var card = player.Creature.CombatState.CreateCard<NaiWaTangHua>(player);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, player);
            CountGainLoss(player);
            _gainedThisTurn[player] = (player.PlayerCombatState?.TurnNumber ?? 0, true);
        }
    }

    /// <summary>给玩家抽牌堆加入 count 张糖化 token（随机位置，获得计数与 GiveToHand 一致）。</summary>
    public static async Task GiveToDrawPile(PlayerChoiceContext ctx, Player player, int count = 1)
    {
        if (player?.Creature == null || player.Creature.CombatState == null) return;
        for (int i = 0; i < count; i++)
        {
            var card = player.Creature.CombatState.CreateCard<NaiWaTangHua>(player);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, player, CardPilePosition.Random);
            CountGainLoss(player);
            _gainedThisTurn[player] = (player.PlayerCombatState?.TurnNumber ?? 0, true);
        }
    }

    /// <summary>给玩家消耗堆加入 count 张糖化 token（获得计数与 GiveToHand 一致）。</summary>
    public static async Task GiveToExhaustPile(PlayerChoiceContext ctx, Player player, int count = 1)
    {
        if (player?.Creature == null || player.Creature.CombatState == null) return;
        for (int i = 0; i < count; i++)
        {
            var card = player.Creature.CombatState.CreateCard<NaiWaTangHua>(player);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Exhaust, player);
            CountGainLoss(player);
            _gainedThisTurn[player] = (player.PlayerCombatState?.TurnNumber ?? 0, true);
        }
    }

    /// <summary>手牌中的糖化 token 数量。</summary>
    public static int CountInHand(Player player)
    {
        return player?.PlayerCombatState?.Hand.Cards.Count(c => c is NaiWaTangHua) ?? 0;
    }

    /// <summary>从手牌消耗 count 张糖化 token（走官方 Exhaust）。</summary>
    public static async Task RemoveFromHand(PlayerChoiceContext ctx, Player player, int count)
    {
        if (player?.PlayerCombatState == null || count <= 0) return;
        var cards = player.PlayerCombatState.Hand.Cards
            .Where(c => c is NaiWaTangHua)
            .Take(count)
            .Cast<NaiWaTangHua>()
            .ToList();
        await RemoveSpecific(ctx, player, cards);
    }

    /// <summary>消耗指定的糖化卡（走官方 Exhaust，触发失去计数和奶量空处抽牌）。</summary>
    public static async Task RemoveSpecific(PlayerChoiceContext ctx, Player player, IEnumerable<NaiWaTangHua> cards)
    {
        if (player == null) return;
        foreach (var card in cards)
        {
            await CardCmd.Exhaust(ctx, card);
            CountGainLoss(player);
            // 清醒：糖化被移除时，从消耗堆回到手牌
            var qingXingCards = player.PlayerCombatState.ExhaustPile.Cards
                .Where(c => c is NaiWaQingXing)
                .ToList();
            foreach (var q in qingXingCards)
                await CardPileCmd.Add(q, PileType.Hand);
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Unplayable,
        CardKeyword.Retain,
    };

    public NaiWaTangHua() : base(-1, CardType.Status, CardRarity.Token, TargetType.Self) { }

    /// <summary>被消耗（官方 Exhaust 路径）→ 获得5层糖分。</summary>
    public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card != this) return Task.CompletedTask;
        if (base.Owner?.Creature == null || !base.Owner.Creature.IsAlive) return Task.CompletedTask;
        return ApplyExhaustEffects(choiceContext);
    }

    private async Task ApplyExhaustEffects(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<NaiWaTangFenPower>(choiceContext, base.Owner.Creature, 5, base.Owner.Creature, this);
        // 抽象主义：每消耗1张糖化，对随机敌人造成伤害
        await NaiWaChouXiangPower.OnTangHuaExhausted(choiceContext, base.Owner);
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => Task.CompletedTask;

    protected override void OnUpgrade() { }
}
