#if !TOOLS
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Timeline.Epochs;

namespace NaiWa.timeline;

internal static class NaiWaTimelineProgress
{
    internal static bool Reconcile(ProgressState progress)
    {
        NaiWaTimelineRegistry.EnsureRegistered();
        bool changed = false;
        var epoch = progress.Epochs.FirstOrDefault(e => e.Id == NaiWaBirthEpoch.EpochId);

        // Validation can create a locked slot before LoadProgress's postfix runs.
        // Merely having empty character stats (created by the old save loader) is not playing.
        if ((epoch == null || epoch.State == EpochState.NotObtained)
            && HasLegacyNaiWaHistory(progress, ModelDb.Character<Characters.NaiWa>().Id))
        {
            progress.ObtainEpoch(NaiWaBirthEpoch.EpochId);
            if (epoch == null) progress.UnlockSlot(NaiWaBirthEpoch.EpochId);
            progress.RevealEpoch(NaiWaBirthEpoch.EpochId);
            changed = true;
        }

        if (progress.IsEpochRevealed(EpochModel.GetId<Defect1Epoch>())
            && !progress.IsEpochRevealed(NaiWaBirthEpoch.EpochId)
            && (epoch == null || epoch.State == EpochState.ObtainedNoSlot))
        {
            progress.UnlockSlot(NaiWaBirthEpoch.EpochId);
            changed = true;
        }

        // Previously completed Defect runs also qualify; do not require replaying one.
        var defectStats = progress.GetStatsForCharacter(ModelDb.Character<Defect>().Id);
        if (defectStats != null && (defectStats.TotalWins > 0 || defectStats.TotalLosses > 0)
            && !progress.IsEpochObtained(NaiWaBirthEpoch.EpochId))
        {
            progress.ObtainEpoch(NaiWaBirthEpoch.EpochId);
            changed = true;
        }
        var search = progress.Epochs.FirstOrDefault(e => e.Id == NaiWaSearchEpoch.EpochId);
        if (progress.IsEpochRevealed(NaiWaBirthEpoch.EpochId)
            && (search == null || search.State == EpochState.ObtainedNoSlot))
        {
            progress.UnlockSlot(NaiWaSearchEpoch.EpochId);
            changed = true;
        }
        if (!progress.IsEpochObtained(NaiWaSearchEpoch.EpochId) && HasCompletedFirstAct(progress))
        {
            progress.ObtainEpoch(NaiWaSearchEpoch.EpochId);
            changed = true;
        }
        var clues = progress.Epochs.FirstOrDefault(e => e.Id == NaiWaCluesEpoch.EpochId);
        if (progress.IsEpochRevealed(NaiWaSearchEpoch.EpochId)
            && (clues == null || clues.State == EpochState.ObtainedNoSlot))
        {
            progress.UnlockSlot(NaiWaCluesEpoch.EpochId);
            changed = true;
        }
        if (!progress.IsEpochObtained(NaiWaCluesEpoch.EpochId) && HasCompletedAct(progress, 1))
        {
            progress.ObtainEpoch(NaiWaCluesEpoch.EpochId);
            changed = true;
        }
        var imprisonment = progress.Epochs.FirstOrDefault(e => e.Id == NaiWaImprisonmentEpoch.EpochId);
        if (progress.IsEpochRevealed(NaiWaCluesEpoch.EpochId)
            && (imprisonment == null || imprisonment.State == EpochState.ObtainedNoSlot))
        {
            progress.UnlockSlot(NaiWaImprisonmentEpoch.EpochId);
            changed = true;
        }
        if (!progress.IsEpochObtained(NaiWaImprisonmentEpoch.EpochId) && HasCompletedAct(progress, 2))
        {
            progress.ObtainEpoch(NaiWaImprisonmentEpoch.EpochId);
            changed = true;
        }
        var suffering = progress.Epochs.FirstOrDefault(e => e.Id == NaiWaSufferingEpoch.EpochId);
        if (progress.IsEpochRevealed(NaiWaImprisonmentEpoch.EpochId)
            && (suffering == null || suffering.State == EpochState.ObtainedNoSlot))
        {
            progress.UnlockSlot(NaiWaSufferingEpoch.EpochId);
            changed = true;
        }
        if (!progress.IsEpochObtained(NaiWaSufferingEpoch.EpochId) && GetDefeatedEliteCount(progress) >= 15)
        {
            progress.ObtainEpoch(NaiWaSufferingEpoch.EpochId);
            changed = true;
        }
        var laughter = progress.Epochs.FirstOrDefault(e => e.Id == NaiWaLaughterEpoch.EpochId);
        if (progress.IsEpochRevealed(NaiWaSufferingEpoch.EpochId)
            && (laughter == null || laughter.State == EpochState.ObtainedNoSlot))
        {
            progress.UnlockSlot(NaiWaLaughterEpoch.EpochId);
            changed = true;
        }
        if (!progress.IsEpochObtained(NaiWaLaughterEpoch.EpochId) && GetDefeatedBossCount(progress) >= 15)
        {
            progress.ObtainEpoch(NaiWaLaughterEpoch.EpochId);
            changed = true;
        }
        var nightSky = progress.Epochs.FirstOrDefault(e => e.Id == NaiWaNightSkyEpoch.EpochId);
        if (progress.IsEpochRevealed(NaiWaLaughterEpoch.EpochId)
            && (nightSky == null || nightSky.State == EpochState.ObtainedNoSlot))
        {
            progress.UnlockSlot(NaiWaNightSkyEpoch.EpochId);
            changed = true;
        }
        // MaxAscension is the next unlocked level, not the highest level completed.
        if (!progress.IsEpochObtained(NaiWaNightSkyEpoch.EpochId)
            && HasCompletedAscensionOne(progress, ModelDb.Character<Characters.NaiWa>().Id))
        {
            progress.ObtainEpoch(NaiWaNightSkyEpoch.EpochId);
            changed = true;
        }
        var laugh = progress.Epochs.FirstOrDefault(e => e.Id == NaiWaLaughEpoch.EpochId);
        if (progress.IsEpochRevealed(NaiWaNightSkyEpoch.EpochId)
            && (laugh == null || laugh.State == EpochState.ObtainedNoSlot))
        {
            progress.UnlockSlot(NaiWaLaughEpoch.EpochId);
            changed = true;
        }
        return changed;
    }

    internal static bool IsAscensionTenWin(RunHistory history, ModelId characterId, ulong localPlayerId)
    {
        var player = history.Players.Count == 1 ? history.Players[0]
            : history.Players.FirstOrDefault(p => p.Id == localPlayerId);
        return history.Win && !history.WasAbandoned && history.Ascension == 10
            && !history.GameMode.AreAchievementsAndEpochsLocked() && player?.Character == characterId;
    }

    internal static bool ReconcileAscensionTenHistory(SaveManager saves)
    {
        if (saves.Progress.IsEpochObtained(NaiWaLaughEpoch.EpochId)) return false;
        var id = ModelDb.Character<Characters.NaiWa>().Id;
        foreach (var name in saves.GetAllRunHistoryNames())
        {
            var result = saves.LoadRunHistory(name);
            if (!result.Success || result.SaveData is not { } history) continue;
            if (!IsAscensionTenWin(history, id, PlatformUtil.GetLocalPlayerId(history.PlatformType))) continue;
            saves.Progress.ObtainEpoch(NaiWaLaughEpoch.EpochId);
            return true;
        }
        return false;
    }

    internal static bool HasCompletedAscensionOne(ProgressState progress, ModelId characterId)
        => progress.GetStatsForCharacter(characterId)?.MaxAscension >= 2;

    internal static long GetDefeatedEliteCount(ProgressState progress)
    {
        var elites = (HashSet<ModelId>)AccessTools.Method(typeof(ProgressSaveManager), "GetEliteEncounters").Invoke(null, null)!;
        return CountEliteWins(progress, ModelDb.Character<Characters.NaiWa>().Id, elites);
    }

    internal static long CountEliteWins(ProgressState progress, ModelId characterId, IReadOnlySet<ModelId> elites)
        => CountEncounterWins(progress, characterId, elites);

    internal static long GetDefeatedBossCount(ProgressState progress)
    {
        var bosses = ModelDb.Acts.SelectMany(a => a.AllBossEncounters).Select(e => e.Id).ToHashSet();
        return CountEncounterWins(progress, ModelDb.Character<Characters.NaiWa>().Id, bosses);
    }

    internal static long CountEncounterWins(ProgressState progress, ModelId characterId, IReadOnlySet<ModelId> encounters)
        => progress.EncounterStats.Values.Where(e => encounters.Contains(e.Id))
            .Sum(e => (long)(e.FightStats.FirstOrDefault(f => f.Character == characterId)?.Wins ?? 0));

    internal static bool HasCompletedFirstAct(ProgressState progress) => HasCompletedAct(progress, 0);

    internal static bool HasCompletedAct(ProgressState progress, int actIndex)
    {
        var id = ModelDb.Character<Characters.NaiWa>().Id;
        var stats = progress.GetStatsForCharacter(id);
        if (stats != null && (stats.TotalWins > 0 || stats.MaxAscension > 0)) return true;
        var bosses = ModelDb.ActsByIndex[actIndex].SelectMany(a => a.AllBossEncounters).Select(e => e.Id).ToHashSet();
        return progress.EncounterStats.Values.Any(e => bosses.Contains(e.Id)
            && e.FightStats.Any(f => f.Character == id && f.Wins > 0));
    }

    internal static bool HasLegacyNaiWaHistory(ProgressState progress, ModelId id)
    {
        var stats = progress.GetStatsForCharacter(id);
        return (stats != null && (stats.TotalWins > 0 || stats.TotalLosses > 0 || stats.Playtime > 0 || stats.MaxAscension > 0))
            || progress.EncounterStats.Values.Any(e => e.FightStats.Any(f => f.Character == id && (f.Wins > 0 || f.Losses > 0)));
    }
}

[HarmonyPatch(typeof(Defect1Epoch), nameof(Defect1Epoch.GetTimelineExpansion))]
public class NaiWaTimelineExpansionPatch
{
    [HarmonyPostfix]
    static void Postfix(ref EpochModel[] __result)
    {
        NaiWaTimelineRegistry.EnsureRegistered();
        if (!__result.Any(e => e.Id == NaiWaBirthEpoch.EpochId))
            __result = [.. __result, EpochModel.Get<NaiWaBirthEpoch>()];
    }
}

[HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.LoadProgress))]
public class NaiWaTimelineLoadPatch
{
    [HarmonyPostfix]
    static void Postfix(ProgressSaveManager __instance)
    {
        if (NaiWaTimelineProgress.Reconcile(__instance.Progress)) __instance.SaveProgress();
    }
}

[HarmonyPatch(typeof(NTimelineScreen), nameof(NTimelineScreen.OnSubmenuOpened))]
public class NaiWaTimelineOpenPatch
{
    [HarmonyPrefix]
    static void Prefix()
    {
        bool changed = NaiWaTimelineProgress.Reconcile(SaveManager.Instance.Progress);
        changed |= NaiWaTimelineProgress.ReconcileAscensionTenHistory(SaveManager.Instance);
        if (changed) SaveManager.Instance.SaveProgressFile();
    }
}

[HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.UpdateWithRunData))]
public class NaiWaTimelineDefectRunPatch
{
    [HarmonyPostfix]
    static void Postfix(ProgressSaveManager __instance, SerializableRun serializableRun)
    {
        if (serializableRun.GameMode.AreAchievementsAndEpochsLocked()) return;
        SerializablePlayer? localPlayer = serializableRun.Players.Count == 1
            ? serializableRun.Players[0]
            : serializableRun.Players.FirstOrDefault(p => p.NetId == PlatformUtil.GetLocalPlayerId(serializableRun.PlatformType));
        // A teammate playing Defect must not unlock NaiWa for the local player.
        if (localPlayer?.CharacterId != ModelDb.Character<Defect>().Id) return;
        if (__instance.Progress.IsEpochObtained(NaiWaBirthEpoch.EpochId)) return;

        // Reuse the game's guard, discovery list and epoch notification.
        var obtain = AccessTools.Method(typeof(ProgressSaveManager), "TryObtainEpochPostRun");
        if ((bool)obtain.Invoke(__instance, [EpochModel.Get<NaiWaBirthEpoch>(), localPlayer, serializableRun])!)
            __instance.SaveProgress();
    }
}
[HarmonyPatch(typeof(ProgressSaveManager), "PostRunCharacterEpochChecks")]
public class NaiWaTimelineAscensionRunPatch
{
    [HarmonyPostfix]
    static void Postfix(ProgressSaveManager __instance, SerializablePlayer serializablePlayer, SerializableRun serializableRun, bool victory)
    {
        if (!victory || (serializableRun.Ascension != 1 && serializableRun.Ascension != 10)
            || serializablePlayer.CharacterId != ModelDb.Character<Characters.NaiWa>().Id
            || serializableRun.GameMode.AreAchievementsAndEpochsLocked()) return;
        var obtain = AccessTools.Method(typeof(ProgressSaveManager), "TryObtainEpochPostRun");
        var epoch = serializableRun.Ascension == 10
            ? EpochModel.Get<NaiWaLaughEpoch>() : EpochModel.Get<NaiWaNightSkyEpoch>();
        if ((bool)obtain.Invoke(__instance, [epoch, serializablePlayer, serializableRun])!)
            __instance.SaveProgress();
    }
}
#endif
