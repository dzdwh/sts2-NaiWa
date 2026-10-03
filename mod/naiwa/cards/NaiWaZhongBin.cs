using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa;
using NaiWa.powers;

namespace NaiWa.cards;

public sealed class NaiWaZhongBin : CardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaLaughPower>(),
        HoverTipFactory.FromPower<NaiWaLaughCardPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_zhongbin.png";

    public NaiWaZhongBin() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.None) { }

    /// <summary>大体型怪物白名单（C# class name），优先于血条检查。</summary>
    private static readonly HashSet<string> LargeMonsterWhitelist = new()
    {
        "KaiserCrabBoss",
    };

    /// <summary>黑名单 — 即使血条宽也不按大体型处理。</summary>
    private static readonly HashSet<string> LargeMonsterBlacklist = new()
    {
        "Toadpole",
    };

    /// <summary>Bounds width threshold for large monsters (血条宽度超过此值视为大型)。</summary>
    private const float LargeBoundsThreshold = 300f;

    private static bool IsLargeMonster(NCreature node)
    {
        if (node?.Visuals == null) return false;
        // 玩家角色的 Bounds 也可能达到大型怪物阈值（奶蛙正好是 300），
        // 但大笑替身应始终按普通玩家尺寸显示。
        if (node.Entity?.Player != null) return false;
        // Blacklist check first
        if (node.Entity?.Monster != null
            && LargeMonsterBlacklist.Contains(node.Entity.Monster.GetType().Name))
            return false;
        // Whitelist check
        if (node.Entity?.Monster != null
            && LargeMonsterWhitelist.Contains(node.Entity.Monster.GetType().Name))
            return true;
        // Fallback to bounds check
        var bounds = node.Visuals.GetNodeOrNull<Control>("%Bounds");
        return bounds != null && bounds.Size.X >= LargeBoundsThreshold;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;
        int creatureCount = CombatState.Enemies.Count(e => e.IsAlive)
            + CombatState.Allies.Count(a => a.IsAlive);
        await LaughAllCreatures(choiceContext, cardPlay.Card.Owner?.Creature);

        try
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(creatureCount)
                .FromCard(this, cardPlay)
                .TargetingRandomOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
        catch (Exception ex)
        {
            Log.Error($"[NaiWaZhongBin] Attack failed: {ex.Message}");
        }
    }

    private static async Task LaughOne(Creature creature, NCombatRoom room, Resource laughSkel,
        Creature? laughSource, bool mirrored)
    {
        var node = room.GetCreatureNode(creature);
        if (node?.Visuals == null) return;

        // Minions: the visible content is a Sprite2D direct child of the
        // NCreatureVisuals root (node.Visuals). Search there, not on NCreature.
        // Enemies: visible content is inside %Visuals, use that.
        var sprite = node.Visuals.GetNodeOrNull<Sprite2D>("Sprite");
        bool isLarge = IsLargeMonster(node);
        Node2D targetVisuals;
        float scale;
        if (sprite != null)
        {
            targetVisuals = sprite;
            scale = 0.5f;
        }
        else if (isLarge)
        {
            targetVisuals = node.Visuals.GetNodeOrNull<Node2D>("%Visuals");
            if (targetVisuals == null) return;
            scale = 2.0f;
        }
        else
        {
            targetVisuals = node.Visuals.GetNodeOrNull<Node2D>("%Visuals");
            if (targetVisuals == null) return;
            scale = 1.05f;
        }

        if (NaiWaSettings.EnableLaughAnim)
            _ = SwapAndRestore(targetVisuals, laughSkel, mirrored, scale);
        if (laughSource != null && !NaiWaTianXiaWuShuangPower.IsActive(laughSource))
        {
            LaughTracker.RecordLaugh();
            // 只把"实际大笑的生物"传给奶日葵：随从/敌人没有奶日葵 power，天然不计数；
            // 之前传打牌玩家会把随从大笑也算进玩家的奶日葵。
            await NaiRiKuiPower.OnAnyLaugh(creature);
        }
        float volume = NaiWaSettings.LaughVolume;
        if (volume > 0f)
        {
            string audioPath;
            if (sprite != null)
                audioPath = "res://audio/naixiaohigh.mp3";
            else if (isLarge)
                audioPath = "res://audio/naixiaolow.mp3";
            else
                audioPath = "res://audio/naiwa_laugh.mp3";
            var audio = new AudioStreamPlayer();
            audio.Stream = ResourceLoader.Load<AudioStream>(audioPath);
            audio.VolumeDb = Godot.Mathf.LinearToDb(volume);
            audio.Finished += () =>
            {
                if (GodotObject.IsInstanceValid(audio))
                    audio.QueueFree();
            };
            node.AddChild(audio);
            audio.Play();
        }
    }

    public static readonly Dictionary<ulong, CancellationTokenSource> CtsMap = new();
    public static readonly Dictionary<ulong, Color> OrigModulateCache = new();

    /// <summary>让战斗中所有存活生物依次大笑，不执行众宾欢也的后续伤害。</summary>
    public static async Task LaughAllCreatures(PlayerChoiceContext ctx, Creature? sourceCreature)
    {
        var combatState = sourceCreature?.CombatState;
        if (combatState == null) return;
        var laughSkel = ResourceLoader.Load<Resource>(
            "res://animations/vfx/laugh/naiwa_laugh_skel_data.tres");
        var room = NCombatRoom.Instance;
        if (laughSkel == null || room == null) return;

        try
        {
            foreach (var ally in combatState.Allies.Where(a => a.IsAlive && a.PetOwner == null))
            {
                var node = room.GetCreatureNode(ally);
                var visuals = node?.Visuals?.GetNodeOrNull<Node2D>("%Visuals");
                if (visuals != null)
                {
                    bool mirrored = false;
                    var current = (Node)visuals;
                    while (current != null)
                    {
                        if (current is Node2D node2D && node2D.Scale.X < 0)
                        {
                            mirrored = true;
                            break;
                        }
                        current = current.GetParent();
                    }
                    _ = SwapAndRestore(visuals, laughSkel, mirrored, scale: 1.05f);
                    if (ally.Player != null)
                    {
                        await NaiWaXiao.TriggerLaughTracking(ctx, ally.Player);
                    }
                    else if (!NaiWaTianXiaWuShuangPower.IsActive(ally))
                    {
                        LaughTracker.RecordLaugh();
                        await NaiRiKuiPower.OnAnyLaugh(ally);
                    }
                    var audio = new AudioStreamPlayer
                    {
                        Stream = ResourceLoader.Load<AudioStream>("res://audio/naiwa_laugh.mp3")
                    };
                    audio.Finished += () =>
                    {
                        if (GodotObject.IsInstanceValid(audio))
                            audio.QueueFree();
                    };
                    node.AddChild(audio);
                    audio.Play();
                }
                await Task.Delay(500);
            }

            foreach (var ally in combatState.Allies.Where(a => a.IsAlive && a.PetOwner != null))
            {
                await LaughOne(ally, room, laughSkel, sourceCreature, mirrored: false);
                await Task.Delay(500);
            }
            foreach (var enemy in combatState.Enemies.Where(e => e.IsAlive))
            {
                await LaughOne(enemy, room, laughSkel, sourceCreature, mirrored: true);
                await Task.Delay(500);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[NaiWaZhongBin] Laugh effect failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 让战斗中所有存活生物在同一帧启动大笑动画与音频，再按确定性顺序结算大笑状态。
    /// 奶之刻专用；众宾欢也仍保留依次大笑的原节奏。
    /// </summary>
    public static async Task LaughAllCreaturesSimultaneously(
        PlayerChoiceContext ctx, Creature? sourceCreature)
    {
        var combatState = sourceCreature?.CombatState;
        if (combatState == null) return;
        var laughSkel = ResourceLoader.Load<Resource>(
            "res://animations/vfx/laugh/naiwa_laugh_skel_data.tres");
        var room = NCombatRoom.Instance;
        if (laughSkel == null || room == null) return;

        try
        {
            var regularAllies = combatState.Allies
                .Where(a => a.IsAlive && a.PetOwner == null)
                .ToList();
            var pets = combatState.Allies
                .Where(a => a.IsAlive && a.PetOwner != null)
                .ToList();
            var enemies = combatState.Enemies.Where(e => e.IsAlive).ToList();

            var laughTargets = new List<(Creature Creature, bool Mirrored)>();
            foreach (var ally in regularAllies)
                laughTargets.Add((ally, IsCreatureVisualMirrored(room, ally)));
            laughTargets.AddRange(pets.Select(pet => (pet, false)));
            laughTargets.AddRange(enemies.Select(enemy => (enemy, true)));

            // 纯视觉先在同一帧全部启动；不在生物之间插入 Delay/await。
            foreach (var (creature, mirrored) in laughTargets)
                StartLaughVisualAndAudio(creature, room, laughSkel, mirrored);

            // 状态修改仍按固定列表顺序 await，保证联机锁步结果一致。
            foreach (var ally in regularAllies)
            {
                if (ally.Player != null)
                {
                    await NaiWaXiao.TriggerLaughTracking(ctx, ally.Player);
                }
                else if (!NaiWaTianXiaWuShuangPower.IsActive(ally))
                {
                    LaughTracker.RecordLaugh();
                    await NaiRiKuiPower.OnAnyLaugh(ally);
                }
            }

            if (sourceCreature != null &&
                !NaiWaTianXiaWuShuangPower.IsActive(sourceCreature))
            {
                foreach (var creature in pets.Concat(enemies))
                {
                    LaughTracker.RecordLaugh();
                    await NaiRiKuiPower.OnAnyLaugh(creature);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[NaiWaZhongBin] Simultaneous laugh effect failed: {ex.Message}");
        }
    }

    private static bool IsCreatureVisualMirrored(NCombatRoom room, Creature creature)
    {
        var visuals = room.GetCreatureNode(creature)?.Visuals?.GetNodeOrNull<Node2D>("%Visuals");
        var current = (Node?)visuals;
        while (current != null)
        {
            if (current is Node2D node2D && node2D.Scale.X < 0)
                return true;
            current = current.GetParent();
        }
        return false;
    }

    private static void StartLaughVisualAndAudio(
        Creature creature, NCombatRoom room, Resource laughSkel, bool mirrored)
    {
        var node = room.GetCreatureNode(creature);
        if (node?.Visuals == null) return;

        var sprite = node.Visuals.GetNodeOrNull<Sprite2D>("Sprite");
        bool isLarge = IsLargeMonster(node);
        Node2D? targetVisuals = sprite ?? node.Visuals.GetNodeOrNull<Node2D>("%Visuals");
        if (targetVisuals == null) return;

        float scale = sprite != null ? 0.5f : isLarge ? 2.0f : 1.05f;
        if (NaiWaSettings.EnableLaughAnim)
            _ = SwapAndRestore(targetVisuals, laughSkel, mirrored, scale);

        float volume = NaiWaSettings.LaughVolume;
        if (volume <= 0f) return;

        string audioPath = sprite != null
            ? "res://audio/naixiaohigh.mp3"
            : isLarge
                ? "res://audio/naixiaolow.mp3"
                : "res://audio/naiwa_laugh.mp3";
        var audio = new AudioStreamPlayer
        {
            Stream = ResourceLoader.Load<AudioStream>(audioPath),
            VolumeDb = Mathf.LinearToDb(volume),
        };
        audio.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(audio))
                audio.QueueFree();
        };
        node.AddChild(audio);
        audio.Play();
    }

    /// <summary>让所有存活敌人播放大笑（奶蛙的忧郁等复用同一套大笑机制）。</summary>
    public static async Task LaughAllEnemies(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var combatState = cardPlay.Card.Owner?.Creature?.CombatState;
        if (combatState == null) return;
        var laughSkel = ResourceLoader.Load<Resource>(
            "res://animations/vfx/laugh/naiwa_laugh_skel_data.tres");
        var room = NCombatRoom.Instance;
        if (laughSkel == null || room == null) return;
        foreach (var enemy in combatState.Enemies.Where(e => e.IsAlive))
        {
            await LaughOne(enemy, room, laughSkel, cardPlay.Card.Owner?.Creature, mirrored: true);
            await Task.Delay(500);
        }
    }

    /// <summary>让单个敌人播放笑动画（糖分触发等）。</summary>
    public static void LaughSingleEnemy(Creature enemy)
    {
        bool enableAnim = NaiWaSettings.EnableLaughAnim;
        float volume = NaiWaSettings.LaughVolume;
        if (!enableAnim && volume <= 0f) return;

        var laughSkel = ResourceLoader.Load<Resource>(
            "res://animations/vfx/laugh/naiwa_laugh_skel_data.tres");
        var room = NCombatRoom.Instance;
        if (room == null) return;
        var node = room.GetCreatureNode(enemy);
        if (node == null) return;
        var visuals = node.Visuals?.GetNodeOrNull<Node2D>("%Visuals");
        if (visuals == null) return;

        if (enableAnim && laughSkel != null)
        {
            bool isLarge = IsLargeMonster(node);
            float scale = isLarge ? 2.0f : 1.05f;
            _ = SwapAndRestore(visuals, laughSkel, mirrored: true, scale);
        }

        if (volume > 0f)
        {
            bool isLarge = IsLargeMonster(node);
            string audioPath = isLarge ? "res://audio/naixiaolow.mp3" : "res://audio/naiwa_laugh.mp3";
            var audio = new AudioStreamPlayer();
            audio.Stream = ResourceLoader.Load<AudioStream>(audioPath);
            audio.VolumeDb = Godot.Mathf.LinearToDb(volume);
            audio.Finished += () =>
            {
                if (GodotObject.IsInstanceValid(audio))
                    audio.QueueFree();
            };
            node.AddChild(audio);
            audio.Play();
        }
    }

    public static async Task SwapAndRestore(Node2D visuals, Resource laughSkel, bool mirrored,
        float scale = 1.05f)
    {
        var nodeId = visuals.GetInstanceId();
        // Cancel previous restore on this specific node
        if (CtsMap.TryGetValue(nodeId, out var oldCts))
        {
            oldCts.Cancel();
            CtsMap.Remove(nodeId);
        }
        var cts = new CancellationTokenSource();
        CtsMap[nodeId] = cts;
        var ct = cts.Token;

        // Remove any existing laugh overlay
        var creatureNode = visuals.GetParent();
        if (creatureNode != null)
        {
            foreach (var child in creatureNode.GetChildren())
            {
                if (child.Name.ToString() == "LaughOverlay")
                    child.QueueFree();
            }
        }

        // Save original modulate only on first call
        if (!OrigModulateCache.ContainsKey(nodeId))
            OrigModulateCache[nodeId] = visuals.Modulate;
        visuals.Modulate = new Color(1, 1, 1, 0);

        // Add fresh laugh overlay（从场景实例化：场景里 SpineSprite 的 Scale 就是
        // Godot 可调的大笑整体大小，默认 1.05；这里只按目标类型乘相对系数并翻转 X）
        var laughScene = ResourceLoader.Load<PackedScene>(
            "res://scenes/vfx/naiwa_laugh_overlay.tscn");
        var laughNode = laughScene?.Instantiate<Node2D>();
        if (laughNode != null && creatureNode != null)
        {
            float baseScale = laughNode.Scale.Y > 0f ? laughNode.Scale.Y : 1.05f;
            float relative = scale / 1.05f;
            Vector2 sceneOffset = laughNode.Position; // 场景里的 Position = Godot 可调的偏移
            laughNode.Name = "LaughOverlay";
            laughNode.Scale = new Vector2(
                (mirrored ? -1 : 1) * baseScale * relative,
                baseScale * relative);
            float yOffset = scale <= 0.5f ? 100 * scale : 0;
            laughNode.Position = visuals.Position + sceneOffset + new Vector2(0, yOffset);
            creatureNode.AddChild(laughNode);
            var anim = laughNode.Call("get_animation_state").AsGodotObject();
            anim?.Call("set_animation", "laugh", false);
        }

        bool cancelled = false;
        try { await Task.Delay(5000, ct); } catch (TaskCanceledException) { cancelled = true; }

        // Only remove if this CTS is still the current one (not replaced by a new laugh)
        if (CtsMap.TryGetValue(nodeId, out var cur) && cur == cts)
            CtsMap.Remove(nodeId);
        Color origMod = default;
        bool hasOrigMod = !cancelled && OrigModulateCache.TryGetValue(nodeId, out origMod);
        if (hasOrigMod) OrigModulateCache.Remove(nodeId);

        // Clean up overlay — always run (even cancelled) since new laugh removes it too
        if (laughNode != null && GodotObject.IsInstanceValid(laughNode))
            laughNode.QueueFree();

        if (!GodotObject.IsInstanceValid(visuals)) return;
        if (creatureNode == null || !GodotObject.IsInstanceValid(creatureNode)) return;

        if (hasOrigMod)
            visuals.Modulate = origMod;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
