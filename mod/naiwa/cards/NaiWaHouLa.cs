using System;
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
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.NaiWapools;

namespace NaiWa.cards;

/// <summary>
/// 齁辣 - 0费Token攻击。保留。本回合至少获得20格挡后可打出。造成6/10伤害。
/// </summary>
public sealed class NaiWaHouLa : CardModel
{
    private const string _bgmPath = "res://audio/hl.mp3";

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_houla.png";

    public override CardPoolModel VisualCardPool => ModelDb.CardPool<NaiWaCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    public NaiWaHouLa() : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        PlayBgm();
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
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
