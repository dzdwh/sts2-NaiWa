using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace NaiWa.minions;

/// <summary>
/// 按 MinionPosition 把随从摆到主人身边。
/// NCombatRoom.AddCreature / PositionPlayersAndPets 默认把非 Osty 宠物堆在玩家脚下（重叠），
/// 这里在两处 Postfix 中重新套用站位。
/// 每只随从在基础站位附近随机散开（纯视觉 → 用 Rng.Chaotic，不碰确定性 RNG 流），
/// 并与同主人的其他随从保持 X 间距；随机结果缓存在 MinionModel.LayoutOffset，
/// 同一只随从布局重算时不会跳位。
/// </summary>
public static class MinionLayout
{
    /// <summary>与其他随从的最小 X 间距（贴图宽 75~98，90 留呼吸感）。</summary>
    private const float MinSpacingX = 90f;

    /// <summary>随机重试上限。</summary>
    private const int MaxRetries = 20;

    public static void Apply(Creature pet)
    {
        if (pet?.PetOwner == null || pet.Monster is not MinionModel minion) return;
        var room = NCombatRoom.Instance;
        var node = room?.GetCreatureNode(pet);
        var ownerNode = room?.GetCreatureNode(pet.PetOwner.Creature);
        if (node == null || ownerNode == null) return;
        minion.LayoutOffset ??= PickOffset(pet, minion);
        node.Position = ownerNode.Position + minion.LayoutOffset.Value;

        // Sort by screen Y: minions lower on screen (higher Y → closer) render in front
        var allMinions = pet.PetOwner.PlayerCombatState.Pets
            .Select(p => room.GetCreatureNode(p))
            .Where(n => n != null)
            .OrderBy(n => n!.GlobalPosition.Y)
            .ToList();
        for (int i = 0; i < allMinions.Count; i++)
            allMinions[i]!.ZIndex = i;
    }

    private static Vector2 PickOffset(Creature pet, MinionModel minion)
    {
        var baseOffset = BaseOffsetFor(minion.Position);
        var taken = pet.PetOwner!.PlayerCombatState.Pets
            .Where(p => p != pet && p.Monster is MinionModel { LayoutOffset: not null })
            .Select(p => ((MinionModel)p.Monster).LayoutOffset!.Value)
            .ToList();

        // Phase 1: random jitter with collision check
        var candidate = baseOffset + RandomJitter();
        for (var i = 0; i < MaxRetries && IsOverlapping(candidate, taken); i++)
            candidate = baseOffset + RandomJitter();

        // Phase 2: fallback — if random still overlaps, slide right in fixed steps
        for (var step = 1f; IsOverlapping(candidate, taken) && step <= 10f; step++)
            candidate = new Vector2(baseOffset.X + MinSpacingX * step, baseOffset.Y + RandomJitter().Y);

        return candidate;
    }

    private static bool IsOverlapping(Vector2 pos, List<Vector2> taken)
        => taken.Any(t => Mathf.Abs(t.X - pos.X) < MinSpacingX && Mathf.Abs(t.Y - pos.Y) < 60f);

    /// <summary>基础站位（相对主人节点，+X 朝敌人方向），参考 Osty 的 (150,-75)，可按手感调整。</summary>
    private static Vector2 BaseOffsetFor(MinionPosition pos) => pos switch
    {
        MinionPosition.Front => new Vector2(170f, 0f),
        MinionPosition.Back => new Vector2(-140f, 0f),
        MinionPosition.FrontUpper => new Vector2(170f, -110f),
        MinionPosition.BackUpper => new Vector2(-140f, -110f),
        MinionPosition.Upper => new Vector2(0f, -150f),
        _ => new Vector2(170f, 0f),
    };

    /// <summary>随机散开范围：X ±80、Y ±45。</summary>
    private static Vector2 RandomJitter() =>
        new Vector2(Rng.Chaotic.NextFloat(-80f, 80f), Rng.Chaotic.NextFloat(-45f, 45f));
}
