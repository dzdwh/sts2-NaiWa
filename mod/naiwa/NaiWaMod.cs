using System.Reflection;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using NaiWa.cards;
using NaiWa.powers;
using NaiWa.relics;
using NaiWa.scripts;
using SmartFormat;

namespace NaiWa;

[ModInitializer("Init")]
public static class NaiWaMod
{
    public static void Init()
    {
        Log.Info("奶蛙 mod 初始化中...");

        ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        // Token cards (奶蛙食物、抑制剂、奶王之卵选择牌等) must NOT be in the character card pool,
        // otherwise generic combat card generation (skill/attack/power potions, Discovery, etc.)
        // can offer them. ModelDb auto-registers every AbstractModel subtype in mod assemblies via
        // reflection, so they are still findable; we register them into the shared TokenCardPool
        // so that CardModel.Pool resolves (and the card library can display them).
        RegisterTokenCardsInSharedPool();

        // Initialize static event subscriptions (moved from static ctors for safety)
        NaiWaJingTankPower.Initialize();
        NaiWaWoTaZhiRuWaPower.Initialize();

        // Telemetry（依赖 RitsuLib；强类型引用由 JIT 自动加载）
        scripts.NaiWaTelemetry.Initialize();

        // 设置（大笑动画/音量）迁到 RitsuLib 设置框架（UI + 持久化都在 RitsuLib）
        scripts.NaiWaSettingsRitsuLib.Register();

        // Register custom localization formatters
        // Must use Harmony Postfix: LocManager.LoadLocFormatters creates a new SmartFormatter
        // AFTER mod Init, which would discard our registration.
        var locHarmony = new Harmony("NaiWa.LocFormatters");
        locHarmony.Patch(
            typeof(LocManager).GetMethod("LoadLocFormatters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(NaiWaDirectionFormatter).GetMethod(nameof(NaiWaDirectionFormatter.OnLocFormattersLoaded), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)));
        Log.Info("[NaiWa] Patched LoadLocFormatters for NaiWaDirectionFormatter");

        var harmony = new Harmony("naiwa.character_patch");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        Log.Info("奶蛙 mod 初始化完成");
    }

    private static void RegisterTokenCardsInSharedPool()
    {
        var tokenTypes = new[]
        {
            typeof(cards.NaiWaNaiQiLin),          // 奶淇淋
            typeof(cards.NaiWaHuoBaoNaiJiao),     // 火爆奶椒
            typeof(cards.NaiWaNaiGua),            // 奶瓜
            typeof(cards.NaiWaMoLiNaiLv),         // 茉莉奶绿
            typeof(cards.NaiWaNaiYouGuo),         // 奶油果
            typeof(cards.NaiWaNailiu),            // 奶榴
            typeof(cards.NaiWaNaiTaoGuanTou),     // 奶桃罐头
            typeof(cards.NaiWaGaGaDiLaXia),       // 嘎嘎滴辣虾
            typeof(cards.NaiWaGaJiaoSai),         // 嘎胶塞
            typeof(cards.NaiWaHouLa),             // 吼啦
            typeof(cards.NaiWaGanBaDe),           // 干爸得
            typeof(cards.NaiWaTangHua),           // 糖化
        };
        foreach (var type in tokenTypes)
            ModHelper.AddModelToPool(typeof(TokenCardPool), type);
    }
}
