using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;

namespace NaiWa;

/// <summary>
/// Tracks laugh events per combat.
/// - Global counter for cards like 呐喊 ("每有一个生物大笑一次")
/// - Per-player flag for cards like 断臂奶龙 ("如果你大笑过")
/// </summary>
public static class LaughTracker
{
    private static int _count;
    private static readonly HashSet<Player> _laughedThisTurn = new();

    public static void RecordLaugh()
    {
        _count++;
    }

    /// <summary>Record that a specific player laughed this turn.</summary>
    public static void RecordPlayerLaugh(Player player)
    {
        if (player != null) _laughedThisTurn.Add(player);
    }

    /// <summary>Has this player laughed this turn?</summary>
    public static bool HasPlayerLaughedThisTurn(Player player)
    {
        return _laughedThisTurn.Contains(player);
    }

    public static int GetLaughCount() => _count;

    public static void ClearCombat()
    {
        _count = 0;
        _laughedThisTurn.Clear();
    }

    public static void ClearForNewCombat()
    {
        _count = 0;
        _laughedThisTurn.Clear();
    }

    /// <summary>Reset per-player laugh flags at turn start.</summary>
    public static void ClearTurn()
    {
        _laughedThisTurn.Clear();
    }
}
