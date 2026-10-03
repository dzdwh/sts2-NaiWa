#if !TOOLS
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using NaiWa.cards;

namespace NaiWa.scripts;

/// <summary>
/// 奶怒：手牌显示与实际扣费共用完整费用计算。
/// GetWithModifiers(All) 已包含 TryModifyEnergyCostInCombat，不能再次扣除随从数量。
/// </summary>
[HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
public class NaiWaNaiNuCostFix
{
    static void Postfix(NCard __instance, PileType pileType)
    {
        if (pileType != PileType.Hand) return;
        if (__instance.Model is not NaiWaNaiNu naiNu) return;

        var label = __instance.GetNodeOrNull<Label>("%EnergyLabel");
        if (label == null) return;

        int cost = naiNu.EnergyCost.GetWithModifiers(CostModifiers.All);
        if (label.Text != cost.ToString())
            label.Text = cost.ToString();
    }
}
#endif
