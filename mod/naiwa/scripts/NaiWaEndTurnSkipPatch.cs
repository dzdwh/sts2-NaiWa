#if !TOOLS
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes.Combat;
using NaiWa.powers;

namespace NaiWa.scripts;

/// <summary>
/// 在每名玩家的回合开始钩子入口清理其上一回合的糖分跳过标记。
/// 不能由 NaiWaTangFenPower 自己清理：溢出时该 Power 已被移除，下一回合不会再收到回调；
/// 多人时所有 Power 还会收到同一个玩家的回合开始回调，放在实例里会产生执行顺序竞态。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterPlayerTurnStart))]
public class NaiWaEndTurnSkipCleanupPatch
{
    static void Prefix(Player player)
    {
        NaiWaTangFenPower.SkippedPlayers.Remove(player);
    }
}

/// <summary>
/// 修复"糖分跳回合后按 E 仍能撤销跳过"：
/// 奶蛙在 AfterPlayerTurnStart（TurnStarted 事件之前）就 SetReadyToEndTurn(canBackOut:false) 标记跳过。
/// 官方 NEndTurnButton.OnTurnStarted 只看"存活且可行动"就无条件 SetState(Enabled)，不检查玩家是否已就绪，
/// 于是按钮被重新点亮、E 热键重新注册；此时按 E 走 CallReleaseLogic 的 Undo 分支——
/// UndoReadyToEndTurn 不检查 canBackOut，直接把玩家移出就绪集合 → 跳过被解除、又能正常出牌/结束回合。
/// 官方自己的自跳回合（VoidForm）在 Play 阶段调 PlayerCmd.EndTurn，按钮已启用过、事件会把热键卸载，
/// 不存在这个竞态。
/// 这里在按钮层做双保险：单人/多人跳过期间都吞掉 EndTurn 按钮的一切点击/热键，
/// 并阻止该回合稍后的 OnTurnStarted 把按钮重新启用。
/// </summary>
[HarmonyPatch(typeof(NEndTurnButton), nameof(NEndTurnButton.CallReleaseLogic))]
public class NaiWaEndTurnSkipGuardPatch
{
    /// <summary>跳过期间按 E / 点按钮一律吞掉，避免进入 UndoEndPlayerTurnAction 分支。</summary>
    static bool Prefix()
    {
        if (NaiWaTangFenPower.SkippedPlayers.Count == 0) return true;
        var me = LocalContext.GetMe(CombatManager.Instance?.DebugOnlyGetState());
        return me == null || !NaiWaTangFenPower.SkippedPlayers.Contains(me);
    }
}

[HarmonyPatch(typeof(NEndTurnButton), "OnTurnStarted")]
public class NaiWaEndTurnSkipVisualPatch
{
    /// <summary>
    /// 被糖分强制跳过时，PlayerCmd.EndTurn 已经通过原版 PlayerEndedTurn 事件禁用了按钮。
    /// 这里阻止随后到达的 OnTurnStarted 再启动滑入动画；不要直接改 _state，否则状态与
    /// 位置 Tween 会不同步，导致下一个正常回合按钮仍停在屏幕外。
    /// </summary>
    static bool Prefix()
    {
        if (NaiWaTangFenPower.SkippedPlayers.Count == 0) return true;
        var me = LocalContext.GetMe(CombatManager.Instance?.DebugOnlyGetState());
        return me == null || !NaiWaTangFenPower.SkippedPlayers.Contains(me);
    }
}
#endif
