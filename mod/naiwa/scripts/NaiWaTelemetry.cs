#if !TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;
using STS2RitsuLib.Telemetry;

namespace NaiWa.scripts;

/// <summary>
/// 奶蛙遥测：通过 RitsuLib 上报卡牌获取、对局结果（胜负/进阶）和异常。
/// RitsuLib 为依赖（强类型引用，Init 时由 JIT 自动加载）。
/// </summary>
public static class NaiWaTelemetry
{
    // token 走查询参数（RitsuLib 的 HttpJsonTelemetryAdapter 只接受 URL，不支持自定义 header）
    private const string Endpoint = ""; // Public builds do not send telemetry.
    private const string ApplicantId = "naiwa";

    private static ITelemetryClient? _client;
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized || string.IsNullOrWhiteSpace(Endpoint)) return;
        _initialized = true;
        try
        {
            RitsuLibFramework.RegisterTelemetryApplicant(new TelemetryApplicant
            {
                ApplicantId = ApplicantId,
                OwnerModId = "naiwa",
                DisplayName = "奶蛙 (NaiWa)",
                Adapter = new HttpJsonTelemetryAdapter(Endpoint),
                Requests = new[]
                {
                    TelemetryRequest.BasicUsage("基础使用统计（卡牌获取、对局）"),
                    TelemetryRequest.RunHistory("对局结果（胜负、进阶、角色）"),
                    TelemetryRequest.Diagnostics("诊断与异常"),
                },
            });
            _client = RitsuLibFramework.GetTelemetryClient(ApplicantId);
            ApplyCardPickPatch();
            Log.Info("[NaiWaTelemetry] initialized");
        }
        catch (Exception ex)
        {
            Log.Error($"[NaiWaTelemetry] Init failed: {ex.Message}");
        }
    }

    public static bool IsEnabled(string requestId)
    {
        try { return _client != null && _client.IsEnabled(requestId); }
        catch { return false; }
    }

    public static void Capture(string eventName, string requestId, IReadOnlyDictionary<string, object?>? props = null)
    {
        try
        {
            if (!IsEnabled(requestId)) return;
            _client!.Capture(eventName, requestId, props);
        }
        catch { }
    }

    public static void CaptureException(Exception ex, string context)
    {
        try
        {
            if (!IsEnabled("diagnostics")) return;
            _client!.CaptureException(ex, new Dictionary<string, object?> { ["context"] = context });
        }
        catch { }
    }

    /// <summary>
    /// 手动挂卡牌获取上报补丁：0.107 的 CardPileCmd.Add 没有 0.111 新增的
    /// isChangingOwners 参数，按 0.111 签名写死 [HarmonyPatch] 会找不到目标方法
    /// 导致整个 mod 初始化失败。这里运行时按参数特征选择重载，两版通用。
    /// </summary>
    private static void ApplyCardPickPatch()
    {
        var target = typeof(CardPileCmd).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(CardPileCmd.Add)
                && m.GetParameters().Length >= 5
                && m.GetParameters()[0].ParameterType == typeof(IEnumerable<CardModel>)
                && m.GetParameters()[1].ParameterType == typeof(CardPile));
        if (target == null)
        {
            Log.Warn("[NaiWaTelemetry] CardPileCmd.Add(IEnumerable<CardModel>, CardPile, ...) not found; card pick telemetry disabled");
            return;
        }
        var harmony = new Harmony("NaiWa.Telemetry.CardPick");
        harmony.Patch(target, postfix: new HarmonyMethod(
            typeof(NaiWaCardPickTelemetryPatch).GetMethod(nameof(NaiWaCardPickTelemetryPatch.Postfix))));
    }
}

/// <summary>卡牌加入牌组时上报（卡牌选取/获取统计，统计全部角色，带 character_id）。</summary>
/// <remarks>由 NaiWaTelemetry.ApplyCardPickPatch 手动挂载（避免版本签名写死）。</remarks>
public static class NaiWaCardPickTelemetryPatch
{
    public static void Postfix(ref Task<IReadOnlyList<CardPileAddResult>> __result, CardPile newPile)
    {
        // 只在任务成功完成时读取 Result；失败任务读 .Result 会把原始异常二次抛出
        // （实测其他 mod 的 Add 失败时被我们的补丁重抛成未观察 AggregateException）
        if (newPile.Type != PileType.Deck || __result == null || !__result.IsCompletedSuccessfully) return;
        try
        {
            foreach (var r in __result.Result)
            {
                if (!r.success || r.cardAdded == null) continue;
                var card = r.cardAdded;
                var characterId = card.Owner?.Character?.Id?.ToString();
                if (characterId == null) continue;
                NaiWaTelemetry.Capture("card_picked", "basic_usage", new Dictionary<string, object?>
                {
                    ["card_id"] = card.Id.Entry,
                    ["upgraded"] = card.IsUpgraded,
                    ["floor"] = card.FloorAddedToDeck,
                    ["character_id"] = characterId,
                });
            }
        }
        catch
        {
            // 遥测绝不能影响游戏本体
        }
    }
}
#endif
