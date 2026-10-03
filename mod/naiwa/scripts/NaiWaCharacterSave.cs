using System.Linq;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace NaiWa.scripts;

/// <summary>NaiWa 角色数据存档，对标 Mordekaiser 的保存系统。</summary>
public class NaiWaCharacterSave
{
    private static NaiWaCharacterSave? _instance;
    private static ISaveStore? _saveStore;

    private const string SaveVersion = "0.1.0";
    private static ModelId NaiWaId => ModelDb.Character<Characters.NaiWa>().Id;

    private static string SavePath =>
        UserDataPathProvider.GetProfileScopedPath(SaveManager.Instance.CurrentProfileId, "naiwa_save.save");

    public static NaiWaCharacterSave Instance => _instance ??= new NaiWaCharacterSave();

    public NaiWaSaveData Data { get; private set; } = new() { Version = SaveVersion };

    private NaiWaCharacterSave() { }

    public static ISaveStore GetSaveStore()
    {
        if (_saveStore != null) return _saveStore;
        var basePath = UserDataPathProvider.GetAccountScopedBasePath(null);
        var local = new GodotFileIo(basePath);
        if (SteamInitializer.Initialized)
            _saveStore = new CloudSaveStore(local, new SteamRemoteSaveStore());
        else
            _saveStore = local;
        return _saveStore;
    }

    public void Load()
    {
        var store = GetSaveStore();
        var json = store.ReadFile(SavePath);
        if (string.IsNullOrEmpty(json))
        {
            Data = new NaiWaSaveData { Version = SaveVersion };
            return;
        }
        try
        {
            var wrapper = JsonSerializer.Deserialize<NaiWaSaveWrapper>(json);
            Data = wrapper?.NaiWa ?? new NaiWaSaveData { Version = SaveVersion };
        }
        catch
        {
            Data = new NaiWaSaveData { Version = SaveVersion };
        }

        // Sync ascension from game to our save
        var stats = SaveManager.Instance.Progress.GetOrCreateCharacterStats(NaiWaId);
        if (stats.MaxAscension > Data.MaxAscension)
            Data.MaxAscension = stats.MaxAscension;
        else if (Data.MaxAscension > stats.MaxAscension)
            stats.MaxAscension = Data.MaxAscension;

        SaveInternal();
    }

    public void SaveAscension(int ascension)
    {
        if (ascension <= Data.MaxAscension) return;
        Data.MaxAscension = ascension;
        SaveInternal();
    }

    public void Delete(int profileId)
    {
        var path = UserDataPathProvider.GetProfileScopedPath(profileId, "naiwa_save.save");
        var store = GetSaveStore();
        if (store.FileExists(path)) store.DeleteFile(path);
    }

    public void Save() { SaveInternal(); }

    private void SaveInternal()
    {
        var store = GetSaveStore();
        var wrapper = new NaiWaSaveWrapper { NaiWa = Data };
        store.WriteFile(SavePath, JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true }));
    }
}

public class NaiWaSaveData
{
    public string Version { get; set; } = "";
    public int MaxAscension { get; set; }
    // EternalPlayCount removed — now stored as [SavedProperty] on NaiWaNaiWaDeYongHeng card instance
}

public class NaiWaSaveWrapper
{
    public NaiWaSaveData NaiWa { get; set; } = new();
}

[HarmonyPatch]
public static class NaiWaCharacterSavePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.LoadProgress))]
    static void OnLoadProgress()
    {
        NaiWaCharacterSave.Instance.Load();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.DeleteProfile))]
    static void OnDeleteProfile(int profileId)
    {
        NaiWaCharacterSave.Instance.Delete(profileId);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.UpdateProgressWithRunData))]
    static void OnUpdateProgress(SerializableRun serializableRun, bool victory)
    {
        if (!victory) return;
        if (serializableRun.Players.All(p => p.CharacterId != ModelDb.Character<Characters.NaiWa>().Id)) return;
        var stats = SaveManager.Instance.Progress.GetOrCreateCharacterStats(ModelDb.Character<Characters.NaiWa>().Id);
        NaiWaCharacterSave.Instance.SaveAscension(stats.MaxAscension);
    }
}
