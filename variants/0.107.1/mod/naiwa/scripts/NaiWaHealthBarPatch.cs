#if !TOOLS
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using NaiWa.minions;

namespace NaiWa.scripts;

/// <summary>
/// 修复随从血条不显示：NCombatRoom.AddCreature 把非 Osty 宠物当装饰物——
/// 堆到玩家脚下并对该玩家所有非 Osty 宠物 ToggleIsInteractable(false)（血条隐藏、不可悬停）。
/// 该调用发生在 NCreature._Ready 之后，所以必须 Postfix AddCreature 而不是 _Ready。
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
public class NaiWaMinionAddCreaturePatch
{
    static void Postfix(NCombatRoom __instance, Creature creature)
    {
        if (creature.PetOwner == null || !LocalContext.IsMe(creature.PetOwner)) return;
        // vanilla 的循环把该玩家所有非 Osty 宠物一起禁用了，这里全部恢复（尸体保持隐藏，复活时再开）
        foreach (var pet in creature.PetOwner.PlayerCombatState.Pets)
        {
            if (pet.Monster is not MinionModel) continue;
            if (pet.IsAlive)
                __instance.GetCreatureNode(pet)?.ToggleIsInteractable(true);
            MinionLayout.Apply(pet);
        }
    }
}

/// <summary>
/// 战斗布局重算（PositionPlayersAndPets）也会把宠物摆回玩家脚下，重新套用随从站位。
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.PositionPlayersAndPets))]
public class NaiWaMinionRepositionPatch
{
    static void Postfix(List<NCreature> creatureNodes)
    {
        foreach (var node in creatureNodes)
        {
            if (node.Entity?.Monster is MinionModel)
                MinionLayout.Apply(node.Entity);
        }
    }
}
#endif
