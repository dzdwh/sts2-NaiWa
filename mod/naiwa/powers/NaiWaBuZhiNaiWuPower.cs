using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using NaiWa.cards;

namespace NaiWa.powers;

/// <summary>
/// 不知奶舞Power - 追踪4张Token牌是否按顺序打出，触发全员奖励+BGM。
/// </summary>
public sealed class NaiWaBuZhiNaiWuPower : PowerModel
{
    private const string _bgmPath = "res://audio/dnkxs.mp3";

    private static readonly Type[] _sequence = new[]
    {
        typeof(NaiWaGaGaDiLaXia),
        typeof(NaiWaGaJiaoSai),
        typeof(NaiWaHouLa),
        typeof(NaiWaGanBaDe),
    };

    private static AudioStreamPlayer? _activeBgm;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _progress;

    [SavedProperty]
    public int Progress
    {
        get => _progress;
        set { AssertMutable(); _progress = value; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar(1),
        new CardsVar(2),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaGaGaDiLaXia>(),
        HoverTipFactory.FromCard<NaiWaGaJiaoSai>(),
        HoverTipFactory.FromCard<NaiWaHouLa>(),
        HoverTipFactory.FromCard<NaiWaGanBaDe>(),
    };

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var playedType = cardPlay.Card.GetType();

        if (Progress < _sequence.Length && playedType == _sequence[Progress])
        {
            Progress++; // 走 [SavedProperty] setter，保证双端同步
            if (Progress >= _sequence.Length)
            {
                Progress = 0;
                // Reward: all players gain 2 energy + draw 3
                foreach (var player in CombatState.Players)
                {
                    await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue * base.Amount, player);
                    await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue * base.Amount, player);
                }
                // Play victory BGM
                PlayBgm();
            }
        }
        else if (playedType == _sequence[0])
        {
            Progress = 1;
        }
        else
        {
            Progress = 0;
        }
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player.Creature == Owner)
        {
            Progress = 0;
            StopBgm();
        }
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // 玩家回合结束时停止顺序奖励 BGM
        if (side == CombatSide.Player)
            StopBgm();
        return Task.CompletedTask;
    }

    private static void PlayBgm()
    {
        StopBgm();

        float volume = NaiWaSettings.LaughVolume;
        if (volume <= 0f) return;

        var stream = ResourceLoader.Load<AudioStream>(_bgmPath);
        if (stream == null) return;

        _activeBgm = new AudioStreamPlayer();
        _activeBgm.Stream = stream;
        _activeBgm.VolumeDb = Mathf.LinearToDb(volume);
        _activeBgm.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(_activeBgm))
                _activeBgm.QueueFree();
            _activeBgm = null;
        };

        var room = NCombatRoom.Instance;
        if (room != null)
        {
            room.AddChild(_activeBgm);
        }
        else
        {
            ((SceneTree)Godot.Engine.GetMainLoop()).Root.AddChild(_activeBgm);
        }

        _activeBgm.Play();
    }

    private static void StopBgm()
    {
        if (_activeBgm != null && GodotObject.IsInstanceValid(_activeBgm))
        {
            _activeBgm.Stop();
            _activeBgm.QueueFree();
        }
        _activeBgm = null;
    }
}
