#if !TOOLS
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using NaiWa.cards;

namespace NaiWa.scripts;

// 只改变往日的降级目标等级，保留原版重建数值、附魔、异变和 UI 通知顺序。
[HarmonyPatch(typeof(CardModel), nameof(CardModel.DowngradeInternal))]
public class NaiWaWangRiDowngradePatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(ci => ci.Calls(AccessTools.PropertySetter(
                    typeof(CardModel), nameof(CardModel.CurrentUpgradeLevel)))))
            .ThrowIfInvalid("Cannot find the upgrade-level reset in CardModel.DowngradeInternal");

        // CurrentUpgradeLevel = 0 -> CurrentUpgradeLevel = GetDowngradedLevel(this)
        matcher.Advance(1).SetInstruction(new CodeInstruction(OpCodes.Ldarg_0))
            .Advance(1).Insert(new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(NaiWaWangRiDowngradePatch), nameof(GetDowngradedLevel))));
        return matcher.InstructionEnumeration();
    }

    private static int GetDowngradedLevel(CardModel card) =>
        card is NaiWaWangRi ? Math.Max(0, card.CurrentUpgradeLevel - 1) : 0;
}
#endif
