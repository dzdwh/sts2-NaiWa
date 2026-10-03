using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using NaiWa.NaiWapools;

namespace NaiWa.cards;

/// <summary>
/// 干爸得 - 0费Token技能。保留。本回合至少抽4张牌后方可打出。获得1/2能量。
/// </summary>
public sealed class NaiWaGanBaDe : CardModel
{
    private const string _bgmPath = "res://audio/gbd.mp3";

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_ganbade.png";

    public override CardPoolModel VisualCardPool => ModelDb.CardPool<NaiWaCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar(1),
    };

    public NaiWaGanBaDe() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        PlayBgm();
        await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Energy"].UpgradeValueBy(1m);
    }

    private void PlayBgm()
    {
        float volume = NaiWaSettings.LaughVolume;
        if (volume <= 0f) return;

        var stream = ResourceLoader.Load<AudioStream>(_bgmPath);
        if (stream == null) return;

        var audioPlayer = new AudioStreamPlayer();
        audioPlayer.Stream = stream;
        audioPlayer.VolumeDb = Mathf.LinearToDb(volume);
        audioPlayer.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(audioPlayer))
                audioPlayer.QueueFree();
        };

        var room = NCombatRoom.Instance;
        if (room != null)
        {
            var cn = room.GetCreatureNode(Owner.Creature);
            var parent = cn?.Visuals?.GetNodeOrNull<Node2D>("%Visuals") ?? (Godot.Node)room;
            parent.AddChild(audioPlayer);
        }
        else
        {
            ((SceneTree)Godot.Engine.GetMainLoop()).Root.AddChild(audioPlayer);
        }

        audioPlayer.Play();
    }
}
