#if !TOOLS
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace NaiWa.timeline;

internal static class NaiWaTimelineRegistry
{
    private static bool _registered;

    internal static void EnsureRegistered()
    {
        if (_registered) return;

        var types = AccessTools.StaticFieldRefAccess<Dictionary<string, Type>>(typeof(EpochModel), "_epochTypeDictionary");
        var ids = AccessTools.StaticFieldRefAccess<Dictionary<Type, string>>(typeof(EpochModel), "_typeToIdDictionary");
        var customEpochs = new[]
        {
            (NaiWaBirthEpoch.EpochId, typeof(NaiWaBirthEpoch)),
            (NaiWaSearchEpoch.EpochId, typeof(NaiWaSearchEpoch)),
            (NaiWaCluesEpoch.EpochId, typeof(NaiWaCluesEpoch)),
            (NaiWaImprisonmentEpoch.EpochId, typeof(NaiWaImprisonmentEpoch)),
            (NaiWaSufferingEpoch.EpochId, typeof(NaiWaSufferingEpoch)),
            (NaiWaLaughterEpoch.EpochId, typeof(NaiWaLaughterEpoch)),
            (NaiWaNightSkyEpoch.EpochId, typeof(NaiWaNightSkyEpoch)),
            (NaiWaLaughEpoch.EpochId, typeof(NaiWaLaughEpoch))
        };
        foreach (var (id, type) in customEpochs)
        {
            types[id] = type;
            ids[type] = id;
        }
        var stories = AccessTools.StaticFieldRefAccess<Dictionary<string, Type>>(typeof(StoryModel), "_storyTypeDictionary");
        stories["NAI_WA"] = typeof(NaiWaStory);

        // 0.111 enumerates types; 0.107 has a fixed ID list. Keep both caches in sync.
        if (AccessTools.Field(typeof(EpochModel), "_allEpochs")?.GetValue(null) is List<Type> allTypes)
            foreach (var (_, type) in customEpochs)
                if (!allTypes.Contains(type)) allTypes.Add(type);
        var allIds = EpochModel.AllEpochIds.ToList();
        foreach (var (id, _) in customEpochs)
            if (!allIds.Contains(id)) allIds.Add(id);
        AccessTools.Field(typeof(EpochModel), "_allEpochIds").SetValue(null, allIds);
        AccessTools.Field(typeof(EpochModel), "_epochIdsHashSet")?.SetValue(null, null);
        _registered = true;
    }
}

// Register before the game builds its multiplayer epoch IDs and handshake hash.
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache), "Init")]
public class NaiWaTimelineNetRegistrationPatch
{
    [HarmonyPrefix]
    static void Prefix() => NaiWaTimelineRegistry.EnsureRegistered();
}

[HarmonyPatch]
public class NaiWaTimelinePortraitPathPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.PropertyGetter(typeof(EpochModel), "PackedPortraitPath");
        yield return AccessTools.PropertyGetter(typeof(EpochModel), "RealPortraitPath");
    }

    [HarmonyPrefix]
    static bool Prefix(EpochModel __instance, ref string __result)
    {
        switch (__instance)
        {
            case NaiWaBirthEpoch: __result = NaiWaBirthEpoch.PortraitPath; break;
            case NaiWaSearchEpoch: __result = NaiWaSearchEpoch.PortraitPath; break;
            case NaiWaCluesEpoch: __result = NaiWaCluesEpoch.PortraitPath; break;
            case NaiWaImprisonmentEpoch: __result = NaiWaImprisonmentEpoch.PortraitPath; break;
            case NaiWaSufferingEpoch: __result = NaiWaSufferingEpoch.PortraitPath; break;
            case NaiWaLaughterEpoch: __result = NaiWaLaughterEpoch.PortraitPath; break;
            case NaiWaNightSkyEpoch: __result = NaiWaNightSkyEpoch.PortraitPath; break;
            case NaiWaLaughEpoch: __result = NaiWaLaughEpoch.PortraitPath; break;
            default: return true;
        }
        return false;
    }
}

#endif
