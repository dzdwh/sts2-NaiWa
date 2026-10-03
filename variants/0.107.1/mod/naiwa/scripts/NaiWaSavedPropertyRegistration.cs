#if !TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace NaiWa.scripts;

/// <summary>
/// SavedProperty 批量注册（移植自参考模组 YuWanCard）。
/// - RegisterAssembly: 扫描程序集内所有带 [SavedProperty] 的 AbstractModel 子类并注册，
///   按 FullName 排序保证注册顺序确定，防止以后新增卡/遗物/能力时漏注册
///   （漏注册 = 该属性不参与多人同步，双端各算各的 → 状态不同步）。
/// - GetNetIdForPropertyName Prefix: 属性名缺失时按需补注册，避免直接抛异常。
/// </summary>
internal static class NaiWaSavedPropertyRegistration
{
    private static readonly object LockObj = new();
    private static readonly HashSet<Type> RegisteredTypes = new();

    public static void RegisterAssembly(Assembly assembly)
    {
        var savedPropertyTypes = GetLoadableTypes(assembly)
            .Where(ShouldRegisterType)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        int registeredCount = 0;
        foreach (var type in savedPropertyTypes)
        {
            if (RegisterType(type))
                registeredCount++;
        }

        if (registeredCount > 0)
            Log.Info($"[NaiWa] SavedPropertyRegistration: registered {registeredCount} custom model types");
    }

    public static bool RegisterType(Type type)
    {
        lock (LockObj)
        {
            if (!RegisteredTypes.Add(type))
                return false;
        }

        NaiWaSavedPropertiesTypeCachePatch.EnsureTypeRegistered(type);
        return true;
    }

    private static bool ShouldRegisterType(Type type)
    {
        return !type.IsAbstract
            && typeof(AbstractModel).IsAssignableFrom(type)
            && type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(property => property.GetCustomAttribute<SavedPropertyAttribute>() != null);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }
}

/// <summary>
/// 按需补注册 + NetIdBitSize 刷新（移植自参考模组 YuWanCard）。
/// </summary>
[HarmonyPatch(typeof(SavedPropertiesTypeCache), nameof(SavedPropertiesTypeCache.GetNetIdForPropertyName))]
public static class NaiWaSavedPropertiesTypeCachePatch
{
    private static readonly Dictionary<string, int> PropertyNameToNetIdMap;
    private static readonly List<string> NetIdToPropertyNameMap;
    private static readonly PropertyInfo? NetIdBitSizeProperty;

    static NaiWaSavedPropertiesTypeCachePatch()
    {
        PropertyNameToNetIdMap = AccessTools.StaticFieldRefAccess<Dictionary<string, int>>(
            typeof(SavedPropertiesTypeCache), "_propertyNameToNetIdMap");
        NetIdToPropertyNameMap = AccessTools.StaticFieldRefAccess<List<string>>(
            typeof(SavedPropertiesTypeCache), "_netIdToPropertyNameMap");
        NetIdBitSizeProperty = AccessTools.Property(typeof(SavedPropertiesTypeCache), "NetIdBitSize");
    }

    public static void EnsureTypeRegistered(Type type)
    {
        SavedPropertiesTypeCache.InjectTypeIntoCache(type);
        RefreshNetIdBitSize();
    }

    public static void EnsurePropertyNameRegistered(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName) || PropertyNameToNetIdMap.ContainsKey(propertyName))
            return;

        int netId = NetIdToPropertyNameMap.Count;
        PropertyNameToNetIdMap[propertyName] = netId;
        NetIdToPropertyNameMap.Add(propertyName);
        RefreshNetIdBitSize();
    }

    private static void RefreshNetIdBitSize()
    {
        int newBitSize = (int)Math.Ceiling(Math.Log2(NetIdToPropertyNameMap.Count));
        NetIdBitSizeProperty?.SetValue(null, newBitSize);
    }

    [HarmonyPrefix]
    public static bool GetNetIdForPropertyName(string propertyName, ref int __result)
    {
        EnsurePropertyNameRegistered(propertyName);
        __result = PropertyNameToNetIdMap[propertyName];
        return false;
    }
}
#endif
