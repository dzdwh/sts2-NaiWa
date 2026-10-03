using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace NaiWa.powers;

/// <summary>
/// 天下无双 — 激活期间阻止大笑和召唤随从，并转化为属性。
/// 阻止大笑 → 获得1力量。阻止召唤 → 获得1敏捷。
/// </summary>
public sealed class NaiWaTianXiaWuShuangPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Gain", 1m),
    };

    /// <summary>检查生物是否受天下无双影响。</summary>
    public static bool IsActive(Creature? creature)
    {
        return creature != null && creature.IsAlive && creature.GetPower<NaiWaTianXiaWuShuangPower>() != null;
    }

    /// <summary>阻止大笑时调用：获得 Gain 力量。</summary>
    public static async Task OnLaughBlocked(Player player)
    {
        if (player?.Creature == null || !player.Creature.IsAlive) return;
        int gain = player.Creature.GetPower<NaiWaTianXiaWuShuangPower>()?.Amount ?? 1;
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), player.Creature, gain, player.Creature, null);
    }

    /// <summary>阻止召唤时调用：获得 Gain 敏捷。</summary>
    public static async Task OnMinionBlocked(Player player)
    {
        if (player?.Creature == null || !player.Creature.IsAlive) return;
        int gain = player.Creature.GetPower<NaiWaTianXiaWuShuangPower>()?.Amount ?? 1;
        await PowerCmd.Apply<DexterityPower>(
            new ThrowingPlayerChoiceContext(), player.Creature, gain, player.Creature, null);
    }
}
