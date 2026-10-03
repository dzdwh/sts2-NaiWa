#if !TOOLS
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using NaiWa.Events;

namespace NaiWa.Scripts;

/// <summary>
/// The default event portrait deliberately overscans a 2560x1200 canvas.
/// Keep the full Nai Wang crystal illustration visible instead of cropping its sides.
/// </summary>
[HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.SetEvent))]
public class NaiWaNaiWangShuiJingLayoutPatch
{
    public static void Postfix(NEventLayout __instance, EventModel eventModel)
    {
        if (eventModel is not NaiWaNaiWangShuiJing)
            return;

        TextureRect? portrait = __instance.GetNodeOrNull<TextureRect>("%Portrait");
        if (portrait == null)
            return;

        portrait.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        portrait.OffsetLeft = 0f;
        portrait.OffsetTop = 39f;
        portrait.OffsetRight = 0f;
        portrait.OffsetBottom = 39f;
        portrait.Scale = Vector2.One;
        portrait.PivotOffset = Vector2.Zero;
        portrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        portrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
    }
}
#endif
