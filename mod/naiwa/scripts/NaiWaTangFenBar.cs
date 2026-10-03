#if !TOOLS
using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using NaiWa.relics;
using NaiWa.powers;

namespace NaiWa.scripts;

/// <summary>
/// 糖分显示：半透明总量条（从左到右 = 糖分累计 / 最大生命值阈值）垫在血条内下面，
/// 实心黄条（当前血量范围内的进度）叠在其上方——黄条覆盖处透明条被隐藏，
/// 糖分超过当前血量时透明条从黄条右缘继续延伸。
/// </summary>
[HarmonyPatch(typeof(NHealthBar), "_Ready")]
public class NaiWaTangFenReadyPatch
{
    private static readonly Color SugarColor = new(1f, 0.85f, 0f);

    static void Postfix(Control ____poisonForeground)
    {
        var parent = ____poisonForeground.GetParent();
        if (parent.HasNode("TangFenForeground")) return;

        // 半透明总量条（先加，垫在下面）：0 → 糖分累计右缘，可超出当前血量
        var overflow = (Control)____poisonForeground.Duplicate();
        overflow.Name = "TangFenForegroundOverflow";
        overflow.SelfModulate = new Color(1f, 0.85f, 0f, 0.45f);
        overflow.Modulate = Colors.White;
        overflow.Visible = false;
        parent.AddChild(overflow);

        // 实心黄条（后加，叠在上面）：0 → 当前 HP 右缘，覆盖住下面的透明条
        var main = (Control)____poisonForeground.Duplicate();
        main.Name = "TangFenForeground";
        main.SelfModulate = SugarColor;
        main.Modulate = Colors.White;
        main.Visible = false;
        parent.AddChild(main);
    }
}

[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
public class NaiWaTangFenRefreshPatch
{
    static void Postfix(
        Control ____hpForeground,
        Control ____poisonForeground,
        Creature ____creature)
    {
        if (____creature == null) return;
        var parent = ____poisonForeground.GetParent();
        var tangFenFg = parent.GetNodeOrNull<Control>("TangFenForeground");
        var tangFenOverflow = parent.GetNodeOrNull<Control>("TangFenForegroundOverflow");
        if (tangFenFg == null || tangFenOverflow == null) return;

        // 糖分累计进度：怪物用独立累计进度，玩家直接用当前糖分层数
        int amt = 0;
        int threshold = 0;
        var sugar = ____creature.GetPower<NaiWaTangFenPower>();
        if (sugar != null)
        {
            if (____creature.IsMonster)
            {
                amt = NaiWaTangFenPower.GetMonsterProgress(____creature);
                threshold = NaiWaTangFenPower.GetMonsterThreshold(____creature);
            }
            else
            {
                amt = (int)sugar.Amount;
                // 奶蛙尾巴 / 始祖尾巴：阈值=最大生命值，否则=当前生命值
                threshold = (____creature.Player?.GetRelic<NaiWaWeiba>() != null
                             || ____creature.Player?.GetRelic<ShiZuWeiBa>() != null)
                    ? ____creature.MaxHp : ____creature.CurrentHp;
            }
        }
        if (amt <= 0 || threshold <= 0)
        {
            tangFenFg.Visible = false;
            tangFenOverflow.Visible = false;
            return;
        }

        float hpOff = ____hpForeground.OffsetRight;
        float hpBarWidth = ____hpForeground.Size.X;
        float full = hpBarWidth + Math.Abs(hpOff);

        // 糖分条右缘按最大生命值阈值；实心段右缘到当前 HP 填充右缘
        float sugarRight = Math.Min(full, full * ((float)amt / threshold));
        float fullSugarRight = Math.Min(sugarRight, hpBarWidth);

        ShowSegment(tangFenOverflow, 0f, sugarRight, full);   // 半透明（下面）
        ShowSegment(tangFenFg, 0f, fullSugarRight, full);     // 实心（上面）
    }

    private static void ShowSegment(Control seg, float left, float right, float full)
    {
        if (right <= left)
        {
            seg.Visible = false;
            return;
        }
        seg.OffsetLeft = left;
        seg.OffsetRight = right - full;
        seg.Visible = true;
    }
}

[HarmonyPatch(typeof(NHealthBar), "RefreshText")]
public class NaiWaTangFenTextPatch
{
    static void Postfix(Creature ____creature, MegaLabel ____hpLabel)
    {
        if (____creature == null) return;
        var sugar = ____creature.GetPower<NaiWaTangFenPower>();
        if (sugar == null) return;
        int amt;
        int threshold;
        if (____creature.IsMonster)
        {
            amt = NaiWaTangFenPower.GetMonsterProgress(____creature);
            threshold = NaiWaTangFenPower.GetMonsterThreshold(____creature);
        }
        else
        {
            amt = (int)sugar.Amount;
            threshold = ____creature.MaxHp;
        }
        if (amt > 0 && threshold > 0 && amt >= threshold)
        {
            ____hpLabel.AddThemeColorOverride("font_color", new Color("FFD700"));
            ____hpLabel.AddThemeColorOverride("font_outline_color", new Color("4A3800"));
        }
    }
}
#endif
