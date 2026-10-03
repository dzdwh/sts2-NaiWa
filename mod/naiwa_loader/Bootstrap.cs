using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace NaiWa.Loader;

/// <summary>
/// 奶蛙多版本引导器（模仿 RitsuLib.Loader 的做法）。
///
/// 游戏只会加载 mods/naiwa/naiwa.dll（本程序集，程序集名 NaiWa.Loader 与变体区分开），
/// 这里负责：
///   1. 读取游戏根目录 release_info.json 的版本号（如 v0.111.0）
///   2. 查 naiwa-variants.manifest，选匹配的变体（精确匹配，退而求其次取 ≤ 当前版本的最高变体）
///   3. 从 lib/&lt;版本&gt;/naiwa.dll 加载真正的 mod 程序集
///   4. 通过 ModManager.AssociateAssemblyWithMod 关联到奶蛙 mod（否则反射注册不到卡牌/角色）
///   5. 调用变体里 [ModInitializer] 标记的初始化方法
/// </summary>
[ModInitializer("Initialize")]
public static class Bootstrap
{
    private const string VariantManifestName = "naiwa-variants.manifest";

    public static void Initialize()
    {
        try
        {
            Run();
        }
        catch (Exception ex)
        {
            Log.Error($"[NaiWa.Loader] 初始化失败: {ex}");
        }
    }

    private static void Run()
    {
        var loaderDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(loaderDir))
        {
            Log.Error("[NaiWa.Loader] 无法定位引导器目录");
            return;
        }

        var hostVersion = ReadHostVersion();
        Log.Info($"[NaiWa.Loader] 游戏版本: {hostVersion ?? "<未知>"}");

        var candidates = LoadVariants(loaderDir);
        if (candidates.Count == 0)
        {
            Log.Error($"[NaiWa.Loader] 缺少或无法解析 {VariantManifestName}");
            return;
        }

        var pick = SelectVariant(candidates, hostVersion);
        if (pick == null)
        {
            Log.Error($"[NaiWa.Loader] 没有匹配的变体 (host={hostVersion ?? "?"})，可选: "
                      + string.Join(", ", candidates.Select(c => c.CompatTarget)));
            return;
        }

        // LoadVariants 已过滤空字符串，Directory/Assembly 此处必然非空
        var dllPath = Path.Combine(loaderDir, pick.Directory!, pick.Assembly!);
        if (!File.Exists(dllPath))
        {
            Log.Error($"[NaiWa.Loader] 变体 DLL 缺失: {dllPath}");
            return;
        }

        try
        {
            // 变体必须加载进游戏所在的 ALC，否则类型身份与游戏分离（Harmony/反射都会失效）
            var gameAsm = typeof(ModInitializerAttribute).Assembly;
            var context = AssemblyLoadContext.GetLoadContext(gameAsm) ?? AssemblyLoadContext.Default;
            var variant = context.LoadFromAssemblyPath(dllPath);
            AssociateWithMod(variant);
            var initializer = FindInitializer(variant);
            if (initializer == null)
            {
                Log.Error($"[NaiWa.Loader] 变体 {variant.FullName} 中没有 ModInitializer 方法");
                return;
            }
            Log.Info($"[NaiWa.Loader] 加载变体 {pick.CompatTarget} -> {variant.FullName}");
            NaiWaModTypesPatch.VariantAssembly = variant;
            InstallPatches(variant);
            initializer.Invoke(null, null);
        }
        catch (Exception ex)
        {
            Log.Error($"[NaiWa.Loader] 变体加载失败: {ex}");
        }
    }

    /// <summary>从游戏目录 release_info.json 读版本（如 "0.111.0"）。
    /// Windows/Linux 通常与可执行文件同层；macOS app bundle 位于 ../Resources。</summary>
    private static string? ReadHostVersion()
    {
        var runtimeVersion = ReadModManagerGameVersion();
        if (!string.IsNullOrWhiteSpace(runtimeVersion))
        {
            Log.Info("[NaiWa.Loader] 游戏版本来源: ModManager");
            return runtimeVersion;
        }

        foreach (var infoPath in GetReleaseInfoCandidates(
                     Environment.ProcessPath, AppContext.BaseDirectory, GetGodotDataDirectory()))
        {
            if (!File.Exists(infoPath)) continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(infoPath));
                if (!doc.RootElement.TryGetProperty("version", out var ver)) continue;
                var version = NormalizeHostVersion(ver.GetString());
                if (string.IsNullOrWhiteSpace(version)) continue;
                Log.Info($"[NaiWa.Loader] release_info.json: {infoPath}");
                return version;
            }
            catch (Exception ex)
            {
                Log.Warn($"[NaiWa.Loader] 读取 {infoPath} 失败: {ex.Message}");
            }
        }

        return null;
    }

    /// <summary>
    /// ModManager.Initialize 在加载任何用户 MOD 前已经接收并保存了游戏版本。
    /// 优先读取它可以避开 Windows/macOS/Android 各自不同的 release_info.json 布局。
    /// </summary>
    private static string? ReadModManagerGameVersion()
    {
        try
        {
            var field = typeof(ModManager).GetField("_gameVersion",
                BindingFlags.NonPublic | BindingFlags.Static);
            return NormalizeHostVersion(field?.GetValue(null)?.ToString());
        }
        catch (Exception ex)
        {
            Log.Warn($"[NaiWa.Loader] 读取 ModManager 游戏版本失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>反射调用 Godot.OS.GetDataDir，避免 Loader 对 GodotSharp 增加直接版本依赖。</summary>
    private static string? GetGodotDataDirectory()
    {
        try
        {
            var godotOs = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("Godot.OS", throwOnError: false))
                .FirstOrDefault(t => t != null);
            var method = godotOs?.GetMethod("GetDataDir",
                BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            return method?.Invoke(null, null) as string;
        }
        catch (Exception ex)
        {
            Log.Warn($"[NaiWa.Loader] 获取 Godot 数据目录失败: {ex.Message}");
            return null;
        }
    }

    private static string? NormalizeHostVersion(string? value)
    {
        value = value?.Trim().TrimStart('v', 'V');
        return Version.TryParse(value, out _) ? value : null;
    }

    /// <summary>列出跨平台版本文件候选路径。公开仅用于无需启动游戏的路径回归测试。</summary>
    public static IReadOnlyList<string> GetReleaseInfoCandidates(
        string? processPath, string? baseDirectory, string? godotDataDirectory = null)
    {
        var directories = new List<string>();
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            var processDirectory = Path.GetDirectoryName(processPath);
            if (!string.IsNullOrWhiteSpace(processDirectory)) directories.Add(processDirectory);
        }
        if (!string.IsNullOrWhiteSpace(baseDirectory)) directories.Add(baseDirectory);

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(godotDataDirectory))
        {
            AddCandidate(Path.Combine(godotDataDirectory, "release_info.json"));
            AddCandidate(Path.Combine(godotDataDirectory, "game", "release_info.json"));
        }

        foreach (var directory in directories)
        {
            AddCandidate(Path.Combine(directory, "release_info.json"));
            AddCandidate(Path.Combine(directory, "..", "Resources", "release_info.json"));
        }
        return result;

        void AddCandidate(string path)
        {
            try
            {
                path = Path.GetFullPath(path);
            }
            catch
            {
                return;
            }
            if (seen.Add(path)) result.Add(path);
        }
    }

    private sealed class VariantEntry
    {
        public string? CompatTarget { get; set; }
        public string? Directory { get; set; }
        public string? Assembly { get; set; }
    }

    private static List<VariantEntry> LoadVariants(string loaderDir)
    {
        try
        {
            var path = Path.Combine(loaderDir, VariantManifestName);
            if (!File.Exists(path)) return new List<VariantEntry>();
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("variants", out var arr)) return new List<VariantEntry>();
            return arr.EnumerateArray()
                .Select(e => new VariantEntry
                {
                    CompatTarget = e.TryGetProperty("compatTarget", out var t) ? t.GetString() : null,
                    Directory = e.TryGetProperty("directory", out var d) ? d.GetString() : null,
                    Assembly = e.TryGetProperty("assembly", out var a) ? a.GetString() : null,
                })
                .Where(e => !string.IsNullOrWhiteSpace(e.CompatTarget)
                            && !string.IsNullOrWhiteSpace(e.Directory)
                            && !string.IsNullOrWhiteSpace(e.Assembly))
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error($"[NaiWa.Loader] 解析 {VariantManifestName} 失败: {ex.Message}");
            return new List<VariantEntry>();
        }
    }

    /// <summary>精确匹配优先；否则取 ≤ 当前版本的最高变体（未来小版本兼容 0.111.x → 0.111.0 变体）。</summary>
    private static VariantEntry? SelectVariant(List<VariantEntry> candidates, string? hostVersion)
    {
        if (string.IsNullOrWhiteSpace(hostVersion)) return null;
        if (!Version.TryParse(hostVersion, out var host)) return null;

        var parsed = candidates
            .Select(c => (Entry: c, Version: Version.TryParse(c.CompatTarget, out var v) ? v : null))
            .Where(x => x.Version != null)
            .ToList();

        var exact = parsed.FirstOrDefault(x => x.Version == host);
        if (exact.Entry != null) return exact.Entry;

        return parsed.Where(x => x.Version <= host)
            .OrderByDescending(x => x.Version)
            .FirstOrDefault().Entry;
    }

    /// <summary>把变体程序集关联进 mod，让 ReflectionHelper.ModTypes 能扫到卡牌/角色类型。
    /// 0.111+ 用官方 AssociateAssemblyWithMod；0.107 没有该方法，
    /// 而它的 ModTypes 读的是 m.assembly（单数），所以直接把 naiwa 的 Mod.assembly 换成变体。</summary>
    private static void AssociateWithMod(Assembly variant)
    {
        try
        {
            var gameAsm = typeof(ModInitializerAttribute).Assembly;
            var modManager = gameAsm.GetType("MegaCrit.Sts2.Core.Modding.ModManager");
            if (modManager == null)
            {
                Log.Warn("[NaiWa.Loader] 找不到 ModManager，跳过程序集关联");
                return;
            }
            var method = modManager?.GetMethod(
                "AssociateAssemblyWithMod",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(Assembly) },
                null);
            if (method != null)
            {
                method.Invoke(null, new object[] { "naiwa", variant });
                return;
            }

            // 0.107 兜底：ModTypes = 所有 mod 的 m.assembly 类型；但 GetLoadedMods()
            // 只返回已 Loaded 的 mod，loader 初始化时 naiwa 还在 Loading，
            // 所以先试 GetLoadedMods，再直接读私有 _mods 列表。
            var getLoaded = modManager!.GetMethod("GetLoadedMods", BindingFlags.Public | BindingFlags.Static);
            bool swapped = getLoaded?.Invoke(null, null) is IEnumerable loaded
                           && TrySwapNaiWaAssembly(loaded, variant);
            if (!swapped)
            {
                var modsField = modManager.GetField("_mods", BindingFlags.NonPublic | BindingFlags.Static);
                swapped = modsField?.GetValue(null) is IEnumerable allMods
                          && TrySwapNaiWaAssembly(allMods, variant);
            }
            if (swapped)
            {
                // 清掉 ModTypes 缓存，下次访问会用新的 mod.assembly 重算
                var refl = gameAsm.GetType("MegaCrit.Sts2.Core.Helpers.ReflectionHelper");
                refl?.GetField("_modTypes", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
                return;
            }
            Log.Warn("[NaiWa.Loader] 找不到 naiwa 的 Mod 对象，跳过程序集关联");
        }
        catch (Exception ex)
        {
            Log.Warn($"[NaiWa.Loader] 关联变体程序集失败: {ex.Message}");
        }
    }

    private static bool TrySwapNaiWaAssembly(IEnumerable mods, Assembly variant)
    {
        foreach (var mod in mods)
        {
            if (mod == null) continue;
            var manifest = mod.GetType().GetField("manifest")?.GetValue(mod);
            var id = manifest?.GetType().GetField("id")?.GetValue(manifest) as string;
            if (id != "naiwa") continue;
            var asmField = mod.GetType().GetField("assembly");
            if (asmField != null)
            {
                asmField.SetValue(mod, variant);
                return true;
            }
        }
        return false;
    }

    private static MethodInfo? FindInitializer(Assembly variant)
    {
        Type[] types;
        try
        {
            types = variant.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray()!;
        }

        foreach (var t in types)
        {
            var attr = t.GetCustomAttribute<ModInitializerAttribute>();
            if (attr == null) continue;
            var method = t.GetMethod(attr.initializerMethod, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null) return method;
        }
        return null;
    }

    /// <summary>
    /// 强制把变体程序集类型追加进 ReflectionHelper.ModTypes。
    /// 0.107 上游戏在 loader 初始化之后才设置 mod.assembly（会覆盖我们的替换），
    /// 而 ModelDb 靠 ModTypes 扫描注册角色/卡牌，所以直接在 getter 上补丁最稳。
    /// </summary>
    [HarmonyLib.HarmonyPatch(typeof(MegaCrit.Sts2.Core.Helpers.ReflectionHelper), "get_ModTypes")]
    public static class NaiWaModTypesPatch
    {
        public static Assembly? VariantAssembly;

        static void Postfix(ref Type[] __result)
        {
            var v = VariantAssembly;
            if (v == null || __result == null) return;
            if (__result.Any(t => t.Assembly == v)) return;
            __result = __result.Concat(v.GetTypes()).ToArray();
        }
    }

    /// <summary>
    /// 0.107 的 ModelIdSerializationCache.Init 直接扫 mod.assembly 找模型，
    /// 而游戏在 mod 加载完成后把 mod.assembly 设成 loader（覆盖替换）。
    /// 在 Init 前把 naiwa 的 mod.assembly 换成变体，让遗物/卡牌/角色进入 net ID 缓存。
    /// </summary>
    [HarmonyLib.HarmonyPatch(typeof(MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache), "Init")]
    public static class ModelIdCacheInitPatch
    {
        static void Prefix()
        {
            var v = NaiWaModTypesPatch.VariantAssembly;
            if (v == null) return;
            try
            {
                var gameAsm = typeof(ModInitializerAttribute).Assembly;
                var mm = gameAsm.GetType("MegaCrit.Sts2.Core.Modding.ModManager");
                var getLoaded = mm?.GetMethod("GetLoadedMods", BindingFlags.Public | BindingFlags.Static);
                if (getLoaded?.Invoke(null, null) is IEnumerable mods && TrySwapNaiWaAssembly(mods, v)) return;
                var modsField = mm?.GetField("_mods", BindingFlags.NonPublic | BindingFlags.Static);
                if (modsField?.GetValue(null) is IEnumerable all) TrySwapNaiWaAssembly(all, v);
            }
            catch (Exception ex)
            {
                Log.Warn($"[NaiWa.Loader] ModelIdCacheInit 替换 mod.assembly 失败: {ex.Message}");
            }
        }
    }

    private static void InstallPatches(Assembly variant)
    {
        var harmony = new HarmonyLib.Harmony("naiwa.loader.patches");
        // 逐个安装，避免某个目标类型缺失（如测试 mock）导致全部失败
        try
        {
            var modTypesGetter = typeof(MegaCrit.Sts2.Core.Helpers.ReflectionHelper)
                .GetProperty("ModTypes")?.GetGetMethod();
            if (modTypesGetter != null)
                harmony.Patch(modTypesGetter,
                    postfix: new HarmonyLib.HarmonyMethod(
                        typeof(NaiWaModTypesPatch).GetMethod("Postfix",
                            BindingFlags.Static | BindingFlags.NonPublic)));
        }
        catch (Exception ex)
        {
            Log.Warn($"[NaiWa.Loader] ModTypes 补丁安装失败: {ex.Message}");
        }
        try
        {
            var cacheInit = typeof(MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache)
                .GetMethod("Init", BindingFlags.Public | BindingFlags.Static);
            if (cacheInit != null)
                harmony.Patch(cacheInit,
                    prefix: new HarmonyLib.HarmonyMethod(
                        typeof(ModelIdCacheInitPatch).GetMethod("Prefix",
                            BindingFlags.Static | BindingFlags.NonPublic)));
        }
        catch (Exception ex)
        {
            Log.Warn($"[NaiWa.Loader] ModelIdCache Init 补丁安装失败: {ex.Message}");
        }
    }
}
