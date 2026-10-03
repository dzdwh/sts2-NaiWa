using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using NaiWa;
using NaiWa.powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace NaiWa.cards;

public sealed class NaiWaXiao : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<VigorPower>(4m),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaLaughPower>(),
        HoverTipFactory.FromPower<NaiWaLaughCardPower>(),
        HoverTipFactory.FromPower<VigorPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_xiao.png";

    public NaiWaXiao() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState;
        if (combatState == null) return;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        if (await TriggerLaughTracking(choiceContext, Owner))
            TriggerLaughVisual(Owner);

        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature,
            DynamicVars["VigorPower"].IntValue, Owner.Creature, this);

        foreach (var enemy in combatState.Enemies.Where(e => e.IsAlive).ToList())
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, 1, Owner.Creature, this);
    }

    public static bool InterruptLaughAudio { get; set; } = false;

    private static readonly Dictionary<ulong, CancellationTokenSource> _laughCtsMap = new();

    /// <summary>
    /// Trigger laugh animation for a player. Uses an overlay SpineSprite so the
    /// original skeleton and Scale are never modified — direction changes and
    /// other game-driven visual updates are preserved.
    /// </summary>
    /// <summary>
    /// 大笑的确定性状态结算（大笑计数、奶日葵能量、奶蛙的世界格挡）。
    /// 卡牌流程必须 await 本方法，保证双端在动作校验前完成同样的状态修改，避免"数据不同步"。
    /// 返回 false 表示被天下无双封锁（已转化为力量）。
    /// </summary>
    public static async Task<bool> TriggerLaughTracking(PlayerChoiceContext choiceContext, Player? player)
    {
        if (CombatManager.Instance?.IsInProgress == true && player != null)
        {
            if (NaiWaTianXiaWuShuangPower.IsActive(player.Creature)) // 天下无双封锁
            {
                await NaiWaTianXiaWuShuangPower.OnLaughBlocked(player);
                return false;
            }
            LaughTracker.RecordLaugh();
            LaughTracker.RecordPlayerLaugh(player);
            await NaiRiKuiPower.OnAnyLaugh(player.Creature);
            await NaiWaShiJiePower.OnAnyLaugh(choiceContext, player);
            await NaiWaDuoLuoNaiWaPower.OnAnyLaugh(choiceContext, player);
            await NaiWaNaiLongTankPower.OnAnyLaugh(choiceContext, player);
            await NaiWaJiushu.OnAnyLaugh(choiceContext, player); // 救赎：每大笑一次费用-1（含自画像等非打出大笑）
        }
        return true;
    }

    /// <summary>大笑动画 + 音频（纯视觉，fire-and-forget，不参与同步）。</summary>
    public static async void TriggerLaughVisual(Player? player)
    {
        if (player == null || player.Character is not Characters.NaiWa) return;
        // 天下无双封锁兜底：即使调用方漏判返回值，被封锁时也不播大笑动画。
        if (NaiWaTianXiaWuShuangPower.IsActive(player.Creature)) return;

        bool enableAnim = NaiWaSettings.EnableLaughAnim;
        float volume = NaiWaSettings.LaughVolume;

        var room = NCombatRoom.Instance;
        Node2D? visualsNode = null;
        if (room != null)
        {
            var cn = room.GetCreatureNode(player.Creature);
            visualsNode = cn?.Visuals?.GetNodeOrNull<Node2D>("%Visuals");
        }
        if (visualsNode == null)
        {
            var tree = (SceneTree)Godot.Engine.GetMainLoop();
            var naiwaNode = FindPlayerNode(tree.Root, player);
            visualsNode = naiwaNode?.GetNodeOrNull<Node2D>("%Visuals");
        }
        if (visualsNode == null) return;

        var nodeId = visualsNode.GetInstanceId();

        // Cancel previous laugh for this node
        if (_laughCtsMap.TryGetValue(nodeId, out var oldCts))
        {
            oldCts.Cancel();
            _laughCtsMap.Remove(nodeId);
        }
        var cts = new CancellationTokenSource();
        _laughCtsMap[nodeId] = cts;
        var ct = cts.Token;

        // Audio
        AudioStreamPlayer? audio = null;
        if (volume > 0f)
        {
            audio = new AudioStreamPlayer();
            audio.Stream = ResourceLoader.Load<AudioStream>("res://audio/naiwa_laugh.mp3");
            audio.VolumeDb = Mathf.LinearToDb(volume);
            visualsNode.AddChild(audio);
            audio.Play();
        }

        if (enableAnim)
        {
            var laughSkel = ResourceLoader.Load<Resource>(
                "res://animations/vfx/laugh/naiwa_laugh_skel_data.tres");
            if (laughSkel != null)
            {
                // Check ancestor Scale.X for direction flip
                bool mirrored = false;
                var node = (Godot.Node)visualsNode;
                while (node != null) { if (node is Node2D n2 && n2.Scale.X < 0) { mirrored = true; break; } node = node.GetParent(); }
                await NaiWaZhongBin.SwapAndRestore(visualsNode, laughSkel, mirrored, scale: 1.05f);
            }
        }
        else
        {
            try { await Task.Delay(5000, ct); } catch (TaskCanceledException) { }
        }

        _laughCtsMap.Remove(nodeId);
        if (audio != null && GodotObject.IsInstanceValid(audio))
            audio.QueueFree();
    }

    /// <summary>兼容入口：状态结算 + 视觉（用于非战斗场景如建筑师对话）。</summary>
    public static async void TriggerLaugh(Player? player)
    {
        if (await TriggerLaughTracking(new ThrowingPlayerChoiceContext(), player))
            TriggerLaughVisual(player);
    }

    private static Godot.Node? FindPlayerNode(Godot.Node parent, Player player)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is not Node c) continue;
            if (c.Name.ToString().StartsWith("NaiWa") && c.Name.ToString().Contains(player.NetId.ToString()))
                return c;
            var found = FindPlayerNode(c, player);
            if (found != null) return found;
        }
        return null;
    }
}
