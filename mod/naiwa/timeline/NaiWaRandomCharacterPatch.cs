#if !TOOLS
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Unlocks;

namespace NaiWa.timeline;

// NaiWa's new unlock must not add a prerequisite to the original random button.
[HarmonyPatch]
public class NaiWaRandomCharacterRequirementPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init));
        yield return AccessTools.Method(typeof(NCharacterSelectScreen), "UpdateRandomCharacterVisibility");
    }

    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getter = AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacters));
        var filter = AccessTools.Method(typeof(NaiWaRandomCharacterRequirementPatch), nameof(WithoutNaiWa));
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.Calls(getter)) yield return new CodeInstruction(OpCodes.Call, filter);
        }
    }

    internal static IEnumerable<CharacterModel> WithoutNaiWa(IEnumerable<CharacterModel> characters)
        => characters.Where(c => c is not Characters.NaiWa);
}

// Random resolution runs deterministically on every peer. Use transmitted lobby
// unlock states, never local SaveManager state, to agree on the same candidate pool.
[HarmonyPatch(typeof(StartRunLobby), "BeginRunLocally")]
public class NaiWaRandomCharacterPoolPatch
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getter = AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacters));
        var filter = AccessTools.Method(typeof(NaiWaRandomCharacterPoolPatch), nameof(AvailableToLobby));
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (!instruction.Calls(getter)) continue;
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, filter);
        }
    }

    internal static IEnumerable<CharacterModel> AvailableToLobby(IEnumerable<CharacterModel> characters, StartRunLobby lobby)
    {
        bool naiWaUnlocked = lobby.Players.All(p =>
            UnlockState.FromSerializable(p.unlockState).IsEpochRevealed<NaiWaBirthEpoch>());
        return naiWaUnlocked ? characters : NaiWaRandomCharacterRequirementPatch.WithoutNaiWa(characters);
    }
}
#endif
