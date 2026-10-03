#if !TOOLS
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;

namespace NaiWa.scripts;

/// <summary>
/// 诊断用：记录瓦解（Disintegration）回合结束自伤是否在两端执行。
/// 之前的分歧报告显示"回合结束第二阶段客户端结算了瓦解自伤、主机端没有"，
/// 此日志用于验证该机制在双端是否一致触发（排查多人不同步）。
/// </summary>
[HarmonyPatch(typeof(DisintegrationPower), nameof(DisintegrationPower.AfterSideTurnEndLate))]
public static class NaiWaDisintegrationDiagPatch
{
    static void Prefix(DisintegrationPower __instance, CombatSide side, IEnumerable<Creature> participants)
    {
        bool contains = participants.Contains(__instance.Owner);
        Godot.GD.Print($"[Diag] Disintegration AfterSideTurnEndLate owner={__instance.Owner?.Name} amount={__instance.Amount} side={side} contains={contains}");
    }
}
#endif
