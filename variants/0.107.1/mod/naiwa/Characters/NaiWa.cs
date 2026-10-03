using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Unlocks;
using NaiWa.cards;
using NaiWa.Events;
using NaiWa.NaiWapools;
using NaiWa.powers;
using NaiWa.relics;
using NaiWa.minions;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;

namespace NaiWa.Characters;

public sealed class NaiWa : CharacterModel
{
	public override CharacterGender Gender => CharacterGender.Neutral;
	protected override CharacterModel? UnlocksAfterRunAs => null;

	public override Color NameColor => new("7BC67EFF");
	public override int StartingHp => 78;
	public override int StartingGold => 99;

	public override CardPoolModel CardPool => ModelDb.CardPool<NaiWaCardPool>();
	public override PotionPoolModel PotionPool => ModelDb.PotionPool<NaiWaPotionPool>();
	public override RelicPoolModel RelicPool => ModelDb.RelicPool<NaiWaRelicPool>();

	public override IEnumerable<CardModel> StartingDeck => new CardModel[]
	{
		ModelDb.Card<NaiWaStrike>(), ModelDb.Card<NaiWaStrike>(),
		ModelDb.Card<NaiWaStrike>(), ModelDb.Card<NaiWaStrike>(),
		ModelDb.Card<NaiWaXiao>(),
		ModelDb.Card<NaiWaFengTi>(),
		ModelDb.Card<NaiWaDefend>(), ModelDb.Card<NaiWaDefend>(),
		ModelDb.Card<NaiWaDefend>(), ModelDb.Card<NaiWaDefend>(),
	};

	public override IReadOnlyList<RelicModel> StartingRelics => new RelicModel[]
	{
		ModelDb.Relic<NaiWaWeiba>(),
	};

	public override float AttackAnimDelay => 0.15f;
	public override float CastAnimDelay => 0.25f;
	public override Color EnergyLabelOutlineColor => new("2E7D32FF");
	public override Color DialogueColor => new("1B5E20");
	public override Color MapDrawingColor => new("F5C842");
	public override Color RemoteTargetingLineColor => new("81C784FF");
	public override Color RemoteTargetingLineOutline => new("2E7D32FF");

	public override string CharacterSelectSfx =>
		ModelDb.Character<Ironclad>().CharacterSelectSfx;

	/// <summary>Extra assets to preload at run start — prevents frame stutter from lazy loading.</summary>
	protected override IEnumerable<string> ExtraAssetPaths => new[]
	{
		"res://materials/cards/frames/card_frame_yellow_mat.tres",
	};

	public override List<string> GetArchitectAttackVfx()
	{
		return new List<string> { "vfx/vfx_attack_slash", "vfx/vfx_attack_blunt", "vfx/vfx_bloody_impact" };
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		var idle = new AnimState("idle_loop", true);
		var attack = new AnimState("attack");
		var cast = new AnimState("cast");
		var hurt = new AnimState("hurt");
		var die = new AnimState("die");
		var attackHeavy = new AnimState("attack_heavy");
		var relaxed = new AnimState("relaxed_loop", true);

		attack.NextState = idle;
		cast.NextState = idle;
		hurt.NextState = idle;
		attackHeavy.NextState = idle;
		relaxed.AddBranch("Idle", idle);

		var animator = new CreatureAnimator(idle, controller);
		animator.AddAnyState("Idle", idle);
		animator.AddAnyState("Dead", die);
		animator.AddAnyState("Hit", hurt);
		animator.AddAnyState("Attack", attack);
		animator.AddAnyState("Cast", cast);
		animator.AddAnyState("PowerUp", cast);
		animator.AddAnyState("heavyAttack", attackHeavy);
		animator.AddAnyState("Relaxed", relaxed);
		return animator;
	}
}

// === Clear laugh tracker at combat start (handles save/reload mid-combat) ===
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom), "_Ready")]
public static class NaiWaCombatStart
{
	static void Prefix()
	{
		LaughTracker.ClearForNewCombat();
		cards.NaiWaTangHua.ClearForNewCombat();
		// 遥测延迟到战斗开始初始化：RitsuLib 主 DLL 在 mod Init 之后才加载，
		// 战斗开始时必然就绪（未安装则静默跳过）
		scripts.NaiWaTelemetry.Initialize();
	}
}

// === Clear combat-scoped counters at combat end (covers victory/defeat/retreat) ===
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class NaiWaCombatEndCleanup
{
	static void Prefix()
	{
		LaughTracker.ClearCombat();
		cards.NaiWaTangHua.ClearCombat();
	}
}

// === Energy orb customization ===
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Combat.NEnergyCounter), "_Ready")]
public static class NaiWaEnergyOrbPatch
{
	static void Postfix(MegaCrit.Sts2.Core.Nodes.Combat.NEnergyCounter __instance)
	{
		// Only apply to NaiWa
		var player = Traverse.Create(__instance).Field("_player").GetValue<Player>();
		if (player?.Character is not NaiWa) return;

		// Layer1 �?naidan.png (custom egg image)
		var l1 = __instance.GetNodeOrNull<TextureRect>("Layers/Layer1");
		if (l1 != null)
		{
			var tex = GD.Load<Texture2D>("res://images/ui/combat/energy_counters/nai_wa/naiwanengliang.png");
			if (tex != null) l1.Texture = tex;
		}

		// Layer2-3 �?keep yellow orbs (in both Layers and RotationLayers)
		for (int i = 2; i <= 3; i++)
		{
			var layer = __instance.GetNodeOrNull<TextureRect>($"Layers/Layer{i}");
			if (layer != null)
			{
				var tex = GD.Load<Texture2D>($"res://images/ui/combat/energy_counters/nai_wa/nai_wa_orb_layer_{i}.png");
				if (tex != null) layer.Texture = tex;
			}
			var rotLayer = __instance.GetNodeOrNull<TextureRect>($"Layers/RotationLayers/Layer{i}");
			if (rotLayer != null)
			{
				var tex = GD.Load<Texture2D>($"res://images/ui/combat/energy_counters/nai_wa/nai_wa_orb_layer_{i}.png");
				if (tex != null) rotLayer.Texture = tex;
			}
		}

		// Scale down & move down Layer2-3 in RotationLayers
		for (int i = 2; i <= 3; i++)
		{
			var rotLayer = __instance.GetNodeOrNull<TextureRect>($"Layers/RotationLayers/Layer{i}");
			if (rotLayer != null)
			{
				rotLayer.Scale = new Vector2(0.7f, 0.7f);
				rotLayer.Position = new Vector2(0, 15);
			}
		}

		// Layer4-5 �?transparent (hide)
		for (int i = 4; i <= 5; i++)
		{
			var layer = __instance.GetNodeOrNull<TextureRect>($"Layers/Layer{i}");
			if (layer != null) layer.Visible = false;
		}

		// Move energy label down a bit (offset for anchor-based layout)
		var label = __instance.GetNodeOrNull<Label>("Label");
		if (label != null)
		{
			label.OffsetTop += 15;
			label.OffsetBottom += 15;
		}
	}
}

// === Harmony patches ===

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
public static class NaiWaPatchAdd
{
	public static void Postfix(ref IEnumerable<CharacterModel> __result)
	{
		var list = __result.ToList();
		list.Add(ModelDb.Character<NaiWa>());
		__result = list;
	}
}

[HarmonyPatch(typeof(UnlockState), nameof(UnlockState.Characters), MethodType.Getter)]
public static class NaiWaUnlockPatch
{
	public static void Postfix(ref IEnumerable<CharacterModel> __result)
	{
		var list = __result.ToList();
		list.Add(ModelDb.Character<NaiWa>());
		__result = list;
	}
}

[HarmonyPatch(typeof(ProgressSaveManager))]
public static class FixNaiWaEpochCrash
{
	[HarmonyPatch("CheckFifteenElitesDefeatedEpoch")]
	[HarmonyPrefix]
	public static bool Prefix_Elites(Player localPlayer) => localPlayer.Character is not NaiWa;

	[HarmonyPatch("CheckFifteenBossesDefeatedEpoch")]
	[HarmonyPrefix]
	public static bool Prefix_Bosses(Player localPlayer) => localPlayer.Character is not NaiWa;

	[HarmonyPatch("ObtainCharUnlockEpoch")]
	[HarmonyPrefix]
	public static bool Prefix_Unlock(Player localPlayer) => localPlayer.Character is not NaiWa;
}

// === Register custom event (时空裂隙, 仅第一层密�? ===
[HarmonyPatch(typeof(Underdocks), "AllEvents", MethodType.Getter)]
public static class ShiKongEventPatch
{
	static void Postfix(ref IEnumerable<EventModel> __result)
	{
		__result = __result.Append(ModelDb.Event<global::NaiWa.Events.ShiKongLieXi>());
		__result = __result.Append(ModelDb.Event<global::NaiWa.Events.NaiWaNaiWangShuiJing>());
	}
}

// === BGM for 时空裂隙 ===
[HarmonyPatch(typeof(global::NaiWa.Events.ShiKongLieXi), "GenerateInitialOptions")]
public static class ShiKongBgmPatch
{
	static void Postfix()
	{
		var old = global::NaiWa.Events.ShiKongLieXi.CurrentBgm;
		if (old != null && GodotObject.IsInstanceValid(old)) { old.Stop(); old.QueueFree(); }
		// Attach to current scene so BGM stops when scene changes
		var currentScene = ((SceneTree)Engine.GetMainLoop()).CurrentScene;
		if (currentScene == null) return;
		var bgm = new AudioStreamPlayer();
		bgm.Stream = ResourceLoader.Load<AudioStream>("res://audio/shikong_bgm.ogg");
		bgm.Finished += () =>
		{
			if (GodotObject.IsInstanceValid(bgm))
				bgm.Play(); // loop
		};
		currentScene.AddChild(bgm);
		bgm.Play();
		global::NaiWa.Events.ShiKongLieXi.CurrentBgm = bgm;
	}
}

/// <summary>事件未选择选项便退出房间或返回主界面时，清理时空裂隙 BGM。</summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NEventRoom), nameof(MegaCrit.Sts2.Core.Nodes.Rooms.NEventRoom._ExitTree))]
public static class ShiKongBgmCleanupPatch
{
	[HarmonyPrefix]
	static void Prefix()
	{
		global::NaiWa.Events.ShiKongLieXi.StopBgm();
	}
}

/// <summary>Call ApplyHpLoss on NaiWaShiTouPower + clear LaughTracker before combat end.</summary>
[HarmonyPatch(typeof(Creature), nameof(Creature.RemoveAllPowersInternalExcept))]
public static class ShiTouHpLossPatch
{
	[HarmonyPrefix]
	static void BeforeRemovePowers(Creature __instance)
	{
		foreach (var p in __instance.GetPowerInstances<NaiWaShiTouPower>())
			p.ApplyHpLoss();
		// Only clear at actual combat end, not during mid-combat power removal (e.g., Vigor expiry)
		if (!CombatManager.Instance.IsInProgress)
		{
			LaughTracker.ClearCombat();
			cards.NaiWaTangHua.ClearCombat();
			NaiWaTangFenPower.ClearStartOfTurnAmounts();
			NaiWaTangFenPower.ClearSugarDropTracker();
			NaiWaDuoBiShiJian.ClearHpSnapshots();
			MinionCmd.ClearCombat();
		}
	}
}

/// <summary>躲避时间：每回合开始记录玩家血量快照（"1回合前"）。</summary>
[HarmonyPatch(typeof(Hook), "AfterPlayerTurnStart")]
public static class DuoBiShiJianHpSnapshotPatch
{
	static void Prefix(ICombatState combatState, Player player)
	{
		// 每回合开始确定性清空"本回合大笑"标记（不依赖糖分 Power 是否存在，
		// 否则玩家无糖分时断臂奶龙的 0 费会跨回合残留）
		LaughTracker.ClearTurn();
		// 每回合开始清空该玩家的糖化计数（奶神赋/思维奶化按回合隔离）
		cards.NaiWaTangHua.ResetGainLossForTurn(player);
		if (player?.Creature != null && player.Creature.IsAlive)
		{
			NaiWaDuoBiShiJian.SetHpSnapshot(player.Creature);
		}
	}
}

// === Register NaiWa dialogue for ALL ancient events ===
// Clones Ironclad's dialogue structure but re-populates with NAI_WA loc keys
// so the custom text in ancients.json (e.g. "DARV.talk.NAI_WA.0-0.ancient") is used.
[HarmonyPatch(typeof(AncientEventModel), "get_DialogueSet")]
public static class NaiWaAncientDialogue
{
	static void Postfix(AncientEventModel __instance, ref AncientDialogueSet __result)
	{
		try
		{
			var naiwaId = ModelDb.Character<NaiWa>().Id.Entry;
			if (__result.CharacterDialogues.ContainsKey(naiwaId))
			{
				Log.Info($"[NaiWaAncientDialogue] {__instance.Id.Entry}: NAI_WA already present, skipping");
				return;
			}

			var ironcladId = ModelDb.Character<Ironclad>().Id.Entry;
			if (!__result.CharacterDialogues.TryGetValue(ironcladId, out var ironcladDialogues))
			{
				Log.Info($"[NaiWaAncientDialogue] {__instance.Id.Entry}: no Ironclad entries, skipping");
				return;
			}

			var ancientEntry = __instance.Id.Entry;
			Log.Info($"[NaiWaAncientDialogue] {ancientEntry}: cloning {ironcladDialogues.Count} Ironclad dialogues for NAI_WA");
			var naiwaDialogues = CloneAndRepopulate(ironcladDialogues, ancientEntry, naiwaId);
			__result.CharacterDialogues[naiwaId] = naiwaDialogues;
		}
		catch (Exception ex)
		{
			Log.Error($"[NaiWaAncientDialogue] Failed: {ex.Message}\n{ex.StackTrace}");
		}
	}

	public static IReadOnlyList<AncientDialogue> CloneAndRepopulate(
		IReadOnlyList<AncientDialogue> source, string ancientEntry, string charEntry)
	{
		var result = new List<AncientDialogue>();
		for (int i = 0; i < source.Count; i++)
		{
			var src = source[i];
			// Preserve original line count �� extract ALL SFX paths via Harmony Traverse
			var sfxPaths = new string[src.Lines.Count];
			for (int j = 0; j < src.Lines.Count; j++)
			{
				sfxPaths[j] = Traverse.Create(src.Lines[j]).Field("_sfxPath").GetValue<string>() ?? "";
			}

			var cloned = new AncientDialogue(sfxPaths)
			{
				VisitIndex = i,
				StartAttackers = src.StartAttackers,
				EndAttackers = src.EndAttackers,
			};
			result.Add(cloned);
		}

		// Populate loc keys for the new character entry
		for (int i = 0; i < result.Count; i++)
		{
			result[i].PopulateLines(ancientEntry, charEntry, i);
			result[i].IsRepeating |= source[i].IsRepeating;
			// Always make the last dialogue repeating so the multi-line conversation
			// continues to show on future visits
			if (i == source.Count - 1)
				result[i].IsRepeating = true;

			// For lines without NAI_WA loc entries, fall back to Ironclad original
			for (int j = 0; j < result[i].Lines.Count; j++)
			{
				if (result[i].Lines[j].LineText?.Exists() != true && j < source[i].Lines.Count)
				{
					result[i].Lines[j].LineText = source[i].Lines[j].LineText;
					result[i].Lines[j].Speaker = source[i].Lines[j].Speaker;
				}
			}
		}

		return result;
	}
}
// === Register NaiWa dialogue for TheArchitect (NOT covered by AncientEventModel patch above) ===
// TheArchitect extends EventModel directly, not AncientEventModel, so it needs its own patch.
[HarmonyPatch(typeof(TheArchitect), "get_DialogueSet")]
public static class NaiWaArchitectDialogue
{
	static void Postfix(TheArchitect __instance, ref AncientDialogueSet __result)
	{
		try
		{
			var naiwaId = ModelDb.Character<NaiWa>().Id.Entry;
			if (__result.CharacterDialogues.ContainsKey(naiwaId)) return;

			var ironcladId = ModelDb.Character<Ironclad>().Id.Entry;
			if (!__result.CharacterDialogues.TryGetValue(ironcladId, out var ironcladDialogues))
				return;

			var ancientEntry = __instance.Id.Entry;
			var naiwaDialogues = NaiWaAncientDialogue.CloneAndRepopulate(ironcladDialogues, ancientEntry, naiwaId);
			__result.CharacterDialogues[naiwaId] = naiwaDialogues;
		}
		catch (Exception ex)
		{
			Log.Error($"[NaiWaArchitectDialogue] Failed: {ex.Message}");
		}
	}
}

// === Trigger laugh animation + audio when NaiWa speaks in TheArchitect event ===
[HarmonyPatch(typeof(TheArchitect), "PlayCurrentLine")]
public static class NaiWaArchitectLaughPatch
{
	static void Postfix(TheArchitect __instance)
	{
		try
		{
			var owner = __instance.Owner;
			if (owner?.Character is not NaiWa) return;

			var dialogue = Traverse.Create(__instance).Field("_dialogue").GetValue<AncientDialogue>();
			var idx = Traverse.Create(__instance).Field("_currentLineIndex").GetValue<int>();
			if (dialogue == null || idx >= dialogue.Lines.Count) return;

			if (dialogue.Lines[idx].Speaker == AncientDialogueSpeaker.Character)
				NaiWaXiao.TriggerLaugh(owner);
		}
		catch (Exception ex)
		{
			Log.Error($"[NaiWaArchitectLaugh] Failed: {ex.Message}");
		}
	}
}
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), new Type[] { typeof(PileType), typeof(Creature) })]
public static class LaughKeywordGoldPilePatch
{
	internal static void ApplyGoldKeywords(ref string text)
	{
		if (string.IsNullOrEmpty(text)) return;
		if (!text.Contains("[gold]大笑")) text = text.Replace("大笑", "[gold]大笑[/gold]");
		if (!text.Contains("[gold]绷住")) text = text.Replace("绷住", "[gold]绷住[/gold]");
		if (!text.Contains("[gold]糖分")) text = text.Replace("糖分", "[gold]糖分[/gold]");
		if (!text.Contains("[gold]奶蛋")) text = text.Replace("奶蛋", "[gold]奶蛋[/gold]");
		if (!text.Contains("[gold]奶狗")) text = text.Replace("奶狗", "[gold]奶狗[/gold]");
		if (!text.Contains("[gold]奶蛇")) text = text.Replace("奶蛇", "[gold]奶蛇[/gold]");
		if (!text.Contains("[gold]奶兔")) text = text.Replace("奶兔", "[gold]奶兔[/gold]");
		if (!text.Contains("[gold]奶狮")) text = text.Replace("奶狮", "[gold]奶狮[/gold]");
		if (!text.Contains("[gold]奶龙")) text = text.Replace("奶龙", "[gold]奶龙[/gold]");
		if (!text.Contains("[gold]奶鸡")) text = text.Replace("奶鸡", "[gold]奶鸡[/gold]");
		if (!text.Contains("[gold]奶鼠")) text = text.Replace("奶鼠", "[gold]奶鼠[/gold]");
		if (!text.Contains("[gold]奶空")) text = text.Replace("奶空", "[gold]奶空[/gold]");
		if (!text.Contains("[gold]奶猪")) text = text.Replace("奶猪", "[gold]奶猪[/gold]");
		if (!text.Contains("[gold]奶蛙食物")) text = text.Replace("奶蛙食物", "[gold]奶蛙食物[/gold]");
		if (!text.Contains("[gold]死生契阔")) text = text.Replace("死生契阔", "[gold]死生契阔[/gold]");
		if (!text.Contains("[gold]奶鲸")) text = text.Replace("奶鲸", "[gold]奶鲸[/gold]");
		if (!text.Contains("[gold]抑制剂")) text = text.Replace("抑制剂", "[gold]抑制剂[/gold]");
		if (!text.Contains("[gold]抑制")) text = text.Replace("抑制", "[gold]抑制[/gold]");
	}

	static void Postfix(CardModel __instance, ref string __result)
	{
		ApplyGoldKeywords(ref __result);
	}
}

[HarmonyPatch(typeof(CardModel), "GetDescriptionForUpgradePreview")]
public static class LaughKeywordGoldPreviewPatch
{
	static void Postfix(CardModel __instance, ref string __result)
	{
		LaughKeywordGoldPilePatch.ApplyGoldKeywords(ref __result);
	}
}

// === Power icon fallback (redirect to power_atlas �?fallback to images/powers/*.png) ===
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Assets.AtlasResourceLoader), "ParsePath")]
public static class NaiWaPowerIconPatch
{
	static void Postfix(string path, ref (string? AtlasName, string? SpriteName) __result)
	{
		// TemporaryStrengthPower subclasses show the Strength icon instead of their own
		if (__result.SpriteName == "nai_wa_bei_power")
			__result = ("power_atlas", __result.SpriteName);
		if (__result.SpriteName == "nai_wa_shi_tou_power"
			|| __result.SpriteName == "nai_wa_beng_power"
			|| __result.SpriteName == "nai_wa_tang_fen_power"
			|| __result.SpriteName == "nai_wa_chou_xiang_power"
			|| __result.SpriteName == "nai_wa_tang_fen_nai_shou_power"
			|| __result.SpriteName == "nai_wa_egg_tank_power"
			|| __result.SpriteName == "nai_wa_mouse_buff_power"
			|| __result.SpriteName == "nai_wa_dog_tank_power"
			|| __result.SpriteName == "nai_wa_snake_tank_power"
			|| __result.SpriteName == "nai_wa_rabbit_tank_power"
			|| __result.SpriteName == "nai_wa_lion_tank_power"
			|| __result.SpriteName == "nai_wa_nai_long_tank_power"
			|| __result.SpriteName == "nai_wa_nai_gou_wo_power"
			|| __result.SpriteName == "nai_wa_chao_tang_xing_tai_power"
			|| __result.SpriteName == "nai_wa_wan_wu_gui_nai_power"
			|| __result.SpriteName == "nai_wa_peng_you_power"
			|| __result.SpriteName == "nai_wa_tian_xia_wu_shuang_power"
			|| __result.SpriteName == "nai_wa_nai_ji_tank_power"
			|| __result.SpriteName == "nai_wa_si_sheng_qi_kuo_power"
			|| __result.SpriteName == "nai_wa_mouse_buff_power"
			|| __result.SpriteName == "nai_wa_si_zhi_tang_xin_jian_power"
			|| __result.SpriteName == "nai_ri_kui_power"
			|| __result.SpriteName == "nai_wa_nai_liang_kong_chu_power"
			|| __result.SpriteName == "nai_wa_nai_zhu_tank_power"
			|| __result.SpriteName == "nai_wa_nai_wu_power"
			|| __result.SpriteName == "nai_wa_nai_kong_power"
			|| __result.SpriteName == "nai_wa_bu_zhi_nai_wu_power"
			|| __result.SpriteName == "nai_wa_laugh_power"
			|| __result.SpriteName == "nai_wa_laugh_card_power"
			|| __result.SpriteName == "nai_wa_kua_yue_nai_er_bei_si_shan_power"
			|| __result.SpriteName == "nai_wa_weiba_power"
			|| __result.SpriteName == "nai_wa_zi_hua_xiang_power"
			|| __result.SpriteName == "nai_wa_jing_tank_power"
			|| __result.SpriteName == "nai_wa_wo_ta_zhi_ru_wa_power"
			|| __result.SpriteName == "nai_wa_tang_ji_ke_de_power"
			|| __result.SpriteName == "nai_wa_nai_ge_la_di_zhi_si_power"
			|| __result.SpriteName == "nai_wa_zi_you_yin_dao_nai_wa_power"
			|| __result.SpriteName == "nai_wa_duo_luo_nai_wa_power"
			|| __result.SpriteName == "nai_wa_jiang_jun_ji_luo_le_yi_zhi_nai_wa_power"
			|| __result.SpriteName == "nai_wa_zui_hou_de_nai_can_power")
			__result = ("power_atlas", __result.SpriteName);
		if (__result.SpriteName == "card/energy_nai_wa")
			__result = ("power_atlas", __result.SpriteName);
	}
}

// === Energy icon: simple texture swap (COMBAT ONLY �?card library previews skip) ===
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Cards.NCard), "UpdateVisuals")]
public static class NaiWaEnergyIconPatch
{
	private static Texture2D? _energyTex;
	static void Postfix(MegaCrit.Sts2.Core.Nodes.Cards.NCard __instance)
	{
		// Only during combat �� card library rendering must NOT be touched
		if (CombatManager.Instance == null || !CombatManager.Instance.IsInProgress) return;

		try
		{
			var card = __instance.Model;
			if (card == null) return;
			if (card.Owner?.Character is not NaiWa) return;

			if (card.VisualCardPool?.IsColorless ?? false) return;
			if (card.Id.Entry.StartsWith("ANCIENT")) return;
			if (card.Type is MegaCrit.Sts2.Core.Entities.Cards.CardType.Status
				or MegaCrit.Sts2.Core.Entities.Cards.CardType.Curse) return;

			_energyTex ??= GD.Load<Texture2D>("res://images/card_portraits/nai_wa_energy.png");
			var icon = __instance.GetNodeOrNull<TextureRect>("%EnergyIcon");
			if (icon != null && _energyTex != null) icon.Texture = _energyTex;
		}
		catch (Exception) { /* skip �� canonical model access in non-combat context */ }
	}
}
// === Rest site: replace Ironclad Spine animations with NaiWa static image ===
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.RestSite.NRestSiteCharacter), "_Ready")]
public static class NaiWaRestSiteSpritePatch
{
	private static Texture2D? _restTex;

	static void Postfix(MegaCrit.Sts2.Core.Nodes.RestSite.NRestSiteCharacter __instance)
	{
		try
		{
			if (__instance.Player?.Character is not NaiWa) return;

			// Hide all Spine child nodes (Ironclad's animated bones)
			foreach (var child in __instance.GetChildren())
			{
				if (child is Node2D n2d)
					n2d.Visible = false;
			}

			// Add static NaiWa sprite on top
		_restTex ??= GD.Load<Texture2D>("res://images/rest_site/nai_wa_rest_site.png");
		if (_restTex == null) return;
		var sprite = new Sprite2D();
		sprite.Texture = _restTex;
		sprite.Position = new Vector2(-134, -361);
		sprite.Scale = new Vector2(0.35f, 0.35f);
		sprite.Centered = false;
			__instance.AddChild(sprite);
		}
		catch (Exception) { /* Act 4 or other unexpected state */ }
	}
}

// === Fix DustyTome crash on NaiWa (no Ancient cards in pool �?SetupForPlayer NRE) ===
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Relics.DustyTome), "SetupForPlayer")]
public static class DustyTomeNaiWaFix
{
	static bool Prefix(MegaCrit.Sts2.Core.Models.Relics.DustyTome __instance,
		MegaCrit.Sts2.Core.Entities.Players.Player player)
	{
		if (player.Character is not NaiWa) return true; // let original run for other characters

		var cards = player.Character.CardPool.GetUnlockedCards(
			player.UnlockState, player.RunState.CardMultiplayerConstraint);
		var ancientCards = cards
			.Where(c => c.Rarity == MegaCrit.Sts2.Core.Entities.Cards.CardRarity.Ancient
				&& !MegaCrit.Sts2.Core.Models.Relics.ArchaicTooth.TranscendenceCards.Contains(c))
			.ToList();
		if (ancientCards.Count == 0) return false; // skip �?no valid Ancient cards

		var pick = player.PlayerRng.Rewards.NextItem(ancientCards);
		if (pick == null) return false;

		__instance.AncientCard = pick.Id;
		return false; // already handled
	}
}

// === Orobas: offer �������� to NaiWa players (��Ц �� Զ�Ŵ�Ц transcendence) ===
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Events.Orobas), "get_OptionPool3")]
public static class OrobasNaiWaRelicPatch
{
	static void Postfix(MegaCrit.Sts2.Core.Models.Events.Orobas __instance,
		ref IEnumerable<MegaCrit.Sts2.Core.Events.EventOption> __result)
	{
		try
		{
			var owner = __instance.Owner;
			if (owner?.Character is not NaiWa) return;
			if (!owner.Deck.Cards.Any(c => c is cards.NaiWaXiao)) return;

			var list = __result.ToList();
			// Use protected RelicOption(RelicModel, string, string?) which creates OnChosen internally
			var method = typeof(MegaCrit.Sts2.Core.Models.EventModel).GetMethod(
				"RelicOption",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
				null,
				new[] { typeof(MegaCrit.Sts2.Core.Models.RelicModel), typeof(string), typeof(string) },
				null);
			if (method != null)
			{
				var relic = MegaCrit.Sts2.Core.Models.ModelDb.Relic<relics.NaiWaGuLaoYaChi>().ToMutable();
				var option = method.Invoke(__instance, new object?[] { relic, "INITIAL", null })
					as MegaCrit.Sts2.Core.Events.EventOption;
				if (option != null) list.Add(option);
			}
			__result = list;
		}
		catch (Exception ex)
		{
			Log.Error($"[OrobasNaiWaRelicPatch] Failed: {ex.Message}");
		}
	}
}
// === TouchOfOrobas: 奶蛙尾巴 �?始祖尾巴 升级映射 ===
// === TouchOfOrobas: ����β�� �� ʼ��β�� ����ӳ�� ===
// Use YuWanCard's approach: directly add to RefinementUpgrades dictionary
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Relics.TouchOfOrobas), "RefinementUpgrades", MethodType.Getter)]
public static class TouchOfOrobasNaiWaPatch
{
	static void Postfix(ref Dictionary<ModelId, MegaCrit.Sts2.Core.Models.RelicModel> __result)
	{
		try
		{
			var naiWaWeiba = MegaCrit.Sts2.Core.Models.ModelDb.Relic<relics.NaiWaWeiba>();
			var shiZuWeiBa = MegaCrit.Sts2.Core.Models.ModelDb.Relic<relics.ShiZuWeiBa>();
			if (!__result.ContainsKey(naiWaWeiba.Id))
			{
				__result[naiWaWeiba.Id] = shiZuWeiBa;
			}
		}
		catch (Exception ex)
		{
			Log.Error($"[TouchOfOrobasNaiWaPatch] Failed: {ex.Message}");
		}
	}
}
// === ������˫����ֹ��Ц��+1��������ֹ�ٻ���+1���� ===
[HarmonyPatch(typeof(FlavorSynchronizer), "CreateEndTurnPingDialogueIfNecessary")]
public static class NaiWaEndTurnPingPatch
{
	private static readonly string[] _laughAudios =
	{
		"res://audio/naiwa_laugh.mp3",
		"res://audio/naixiaohigh.mp3",
		"res://audio/naixiaolow.mp3",
	};
	private static int _audioIndex;

	static void Prefix(Player player)
	{
		if (player.Character is not NaiWa) return;

		// Play random laugh audio (NOT through game action system, won't count)
		var audio = new AudioStreamPlayer();
		audio.Stream = ResourceLoader.Load<AudioStream>(
			_laughAudios[(_audioIndex++) % _laughAudios.Length]);
		audio.Finished += () =>
		{
			if (GodotObject.IsInstanceValid(audio))
				audio.QueueFree();
		};
		if (NRun.Instance != null)
			NRun.Instance.AddChild(audio);
		audio.Play();
	}
}

// === 百科大全角色数据：NGeneralStatsGrid.LoadStats 硬编码了5个角色，需追加奶蛙 ===
[HarmonyPatch(typeof(NGeneralStatsGrid), nameof(NGeneralStatsGrid.LoadStats))]
public static class NaiWaStatsGridPatch
{
	static void Postfix(NGeneralStatsGrid __instance)
	{
		var progressSave = MegaCrit.Sts2.Core.Saves.SaveManager.Instance.Progress;
		var naiwaId = ModelDb.Character<NaiWa>().Id;
		// Call private CreateCharacterSection(ProgressState progressSave, ModelId id)
		Traverse.Create(__instance)
			.Method("CreateCharacterSection", progressSave, naiwaId)
			.GetValue();
	}
}
