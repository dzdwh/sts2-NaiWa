using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NaiWa;
using NaiWa.cards;
using NaiWa.relics;

namespace NaiWa.powers;

/// <summary>
/// 糖分 - 记录糖分层数。玩家不再有溢出/糖化机制；怪物侧：糖分累计到最大生命值后，回合开始大笑+眩晕。
/// </summary>
public sealed class NaiWaTangFenPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>糖分跳过回合的玩家所有卡牌不可出（双端确定性拦截）。</summary>
    public static readonly HashSet<Player> SkippedPlayers = new();

    private static readonly Dictionary<Creature, int> _startOfTurnAmounts = new();
    private static readonly Dictionary<Creature, bool> _sugarGainedThisTurn = new();
    private static readonly Dictionary<Creature, int> _sugarGainCountThisTurn = new();
    private static readonly Dictionary<Creature, int> _sugarDroppedThisTurn = new();
    /// <summary>怪物糖分累计进度（达到最大生命值后，怪物回合开始大笑+眩晕）。</summary>
    private static readonly Dictionary<Creature, int> _monsterProgress = new();

    public static int GetStartOfTurnAmount(Creature creature)
    {
        _startOfTurnAmounts.TryGetValue(creature, out int v);
        return v;
    }

    public static bool HasGainedSugarThisTurn(Creature creature)
    {
        return _sugarGainedThisTurn.TryGetValue(creature, out bool v) && v;
    }

    /// <summary>本回合该生物获得糖分的次数（每次 +amount 计 1 次，用于奶神赋）。</summary>
    public static int GetSugarGainCountThisTurn(Creature creature)
    {
        if (creature == null) return 0;
        _sugarGainCountThisTurn.TryGetValue(creature, out int v);
        return v;
    }

    /// <summary>Called at combat end to prevent stale data.</summary>
    public static void ClearStartOfTurnAmounts()
    {
        _startOfTurnAmounts.Clear();
        _sugarGainedThisTurn.Clear();
        _sugarGainCountThisTurn.Clear();
        _monsterProgress.Clear();
        SkippedPlayers.Clear();
    }

    public static void ClearSugarDropTracker()
    {
        _sugarDroppedThisTurn.Clear();
    }

    public static void ResetSugarDropForTurn(Creature creature)
    {
        _sugarDroppedThisTurn[creature] = 0;
    }

    /// <summary>怪物糖分累计进度（用于血条显示）。</summary>
    public static int GetMonsterProgress(Creature creature)
    {
        return _monsterProgress.GetValueOrDefault(creature, 0);
    }

    /// <summary>怪物糖分耐受层数（每层阈值 ×2）。</summary>
    private static int GetMonsterTolerance(Creature creature)
    {
        return creature?.GetPower<NaiWaTangFenNaiShouPower>()?.Amount ?? 0;
    }

    /// <summary>怪物溢出阈值 = 当前生命值 × 2^耐受层数。</summary>
    public static int GetMonsterThreshold(Creature creature)
    {
        int hp = creature?.CurrentHp ?? 0;
        if (hp <= 0) return 0;
        int layers = GetMonsterTolerance(creature);
        long mult = 1;
        for (int i = 0; i < layers && mult < int.MaxValue; i++)
            mult *= 2;
        return (int)Math.Min(int.MaxValue, (long)hp * mult);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != null)
            _startOfTurnAmounts[base.Owner] = base.Amount;
        ResetSugarDropForTurn(base.Owner);
        _sugarGainedThisTurn[base.Owner] = false;
        _sugarGainCountThisTurn[base.Owner] = 0;
        LaughTracker.ClearTurn();

        if (player.Creature != base.Owner) return;
        if (base.Owner == null || !base.Owner.IsAlive) return;

        NaiWaTangHua.ResetGainLossForTurn(player);

        // 简化版玩家溢出：回合开始时糖分达到阈值 → 大笑并跳过本回合
        // 阈值：有奶蛙尾巴 = 最大生命值，否则 = 当前生命值；超糖形态：不再跳过回合
        // 奶蛙尾巴 / 始祖尾巴：阈值改为最大生命值
        bool hasTail = base.Owner.Player?.GetRelic<NaiWaWeiba>() != null
                       || base.Owner.Player?.GetRelic<ShiZuWeiBa>() != null;
        int threshold = hasTail ? base.Owner.MaxHp : base.Owner.CurrentHp;
        if (base.Amount >= threshold && !base.Owner.HasPower<NaiWaChaoTangXingTaiPower>())
            await SkipPlayerTurn(choiceContext, player);
    }

    /// <summary>糖分跳过回合的玩家不能出牌（ShouldPlay 双端确定性检查）。</summary>
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner != null && SkippedPlayers.Contains(card.Owner))
            return false;
        return true;
    }

    private async Task SkipPlayerTurn(PlayerChoiceContext choiceContext, Player player)
    {
        if (await NaiWaXiao.TriggerLaughTracking(choiceContext, player))
            NaiWaXiao.TriggerLaughVisual(player);

        // 触发后清空糖分
        await PowerCmd.Remove(this);

        // 始终走原版结束回合入口：它会在本地关闭玩家行动，并在单人/多人中统一设置
        // ready 状态。旧实现的单人分支只调用 SetReadyToEndTurn，漏掉了
        // OnEndedTurnLocally，导致触发后仍可能继续操作而没有真正跳过本回合。
        SkippedPlayers.Add(player);
        PlayerCmd.EndTurn(player, canBackOut: false);
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // Track whether sugar was gained this turn (for 思维奶化 etc.)
        if (power == this && amount > 0 && base.Owner != null)
        {
            _sugarGainedThisTurn[base.Owner] = true;
            _sugarGainCountThisTurn[base.Owner] =
                _sugarGainCountThisTurn.GetValueOrDefault(base.Owner, 0) + 1;

            // 怪物侧：糖分累计进度（钳制在最大生命值），达到后怪物回合开始触发大笑+眩晕
            if (base.Owner.IsMonster)
            {
                int threshold = GetMonsterThreshold(base.Owner);
                if (threshold > 0)
                    _monsterProgress[base.Owner] =
                        Math.Min(threshold, _monsterProgress.GetValueOrDefault(base.Owner, 0) + (int)amount);
            }
        }
    }

    /// <summary>怪物回合开始：糖分累计进度达到阈值 → 大笑 + 眩晕 + 清空糖分 + 耐受+1；未溢出则移除耐受。</summary>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        await base.AfterSideTurnStart(side, participants, combatState);
        if (side != CombatSide.Enemy) return;
        if (base.Owner == null || !base.Owner.IsAlive || !base.Owner.IsMonster) return;

        int progress = _monsterProgress.GetValueOrDefault(base.Owner, 0);
        int threshold = GetMonsterThreshold(base.Owner);
        var tolerance = base.Owner.GetPower<NaiWaTangFenNaiShouPower>();
        if (threshold > 0 && progress >= threshold)
        {
            _monsterProgress[base.Owner] = 0;
            NaiWaZhongBin.LaughSingleEnemy(base.Owner);
            base.Owner.StunInternal(_ => Task.CompletedTask, null);
            // 大笑后清空糖分
            await PowerCmd.Remove(this);
            // 糖分耐受 +1（下次阈值 ×2）
            if (tolerance != null)
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), tolerance, 1, base.Owner, null);
            else
                await PowerCmd.Apply<NaiWaTangFenNaiShouPower>(new ThrowingPlayerChoiceContext(), base.Owner, 1, base.Owner, null);
        }
        else if (tolerance != null)
        {
            // 未溢出 → 移除耐受
            await PowerCmd.Remove(tolerance);
        }
    }
}
