#if !TOOLS
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using NaiWa.cards;

namespace NaiWa.scripts;

/// <summary>
/// Fixes the energy cost display for 奶蛙的永恒 in deck view / card library.
/// The canonical instance's _base is frozen at 0, but the deck view reads from
/// EnergyCost.GetWithModifiers which returns the frozen value.
/// This postfix overrides the displayed cost with the live PlayCount.
/// Uses cached last cost to skip redundant Label.Text assignments (avoids UI redraw loop).
/// </summary>
[HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
public static class NaiWaEternalCostFix
{
    private static int _lastCost = -1;

    static void Postfix(NCard __instance, PileType pileType)
    {
        var model = __instance.Model;
        if (model is not NaiWaNaiWaDeYongHeng eternal) return;

        // Only fix hand display — deck/library should show canonical cost (0).
        if (pileType != PileType.Hand) return;

        var label = __instance.GetNodeOrNull<Label>("%EnergyLabel");
        if (label == null) return;

        int cost = eternal.IncreasedCost;
        if (cost == _lastCost) return;
        _lastCost = cost;

        label.Text = cost.ToString();

        var icon = __instance.GetNodeOrNull<TextureRect>("%EnergyIcon");
        if (icon != null) icon.Visible = cost >= 0;
    }
}
#endif
