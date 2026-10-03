#if !TOOLS
using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using NaiWa.NaiWapools;

namespace NaiWa.scripts;

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
public class NaiWaCardLibrary
{
    static void Postfix(
        NCardLibrary __instance,
        ref Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters,
        ref Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters)
    {
        // Get Ironclad filter via unique name (same pattern as NCardLibrary._Ready line 164)
        var ironcladFilter = __instance.GetNode<NCardPoolFilter>("%IroncladPool");
        if (ironcladFilter == null) return;

        var parent = ironcladFilter.GetParent();
        if (parent == null || parent.HasNode("NaiWaPool")) return;

        // Clone Ironclad's filter
        var naiwaPool = (NCardPoolFilter)ironcladFilter.Duplicate();
        naiwaPool.Name = "NaiWaPool";

        var insertIndex = ironcladFilter.GetIndex() + 1;
        parent.AddChild(naiwaPool);
        parent.MoveChild(naiwaPool, insertIndex);

        // NCardPoolFilter has no Label child — text is embedded in the Image texture.
        // Set Loc for hover tips instead.
        naiwaPool.Loc = ModelDb.Character<Characters.NaiWa>().Title;

        // Replace icon with NaiWa's character icon
        var image = naiwaPool.GetNodeOrNull<TextureRect>("Image");
        if (image != null)
        {
            var icon = GD.Load<Texture2D>("res://images/ui/top_panel/character_icon_nai_wa.png");
            if (icon != null) image.Texture = icon;
        }

        // Register filter
        var callable = new Callable(__instance, "UpdateCardPoolFilter");
        naiwaPool.Connect(NCardPoolFilter.SignalName.Toggled, callable);
        ____poolFilters.Add(naiwaPool, c => c.Pool is NaiWaCardPool && c.Rarity != CardRarity.Token);
        ____cardPoolFilters.Add(ModelDb.Character<Characters.NaiWa>(), naiwaPool);
    }
}
#endif
