using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace NaiWa.minions;

public abstract class MinionModel : MonsterModel
{
    public override string DeathSfx => "event:/sfx/characters/osty/osty_die";
    public override bool HasDeathSfx => true;
    public override bool IsHealthBarVisible => base.Creature?.IsAlive ?? true;
    public MinionPosition Position { get; internal set; }

    /// <summary>本次召唤随机出的站位偏移（相对主人节点），由 MinionLayout 首次 Apply 时生成并缓存，避免布局重算时跳位。</summary>
    public Vector2? LayoutOffset { get; internal set; }

    /// <summary>召唤序号（递增），用于后召先挡的伤害优先级。</summary>
    public int SummonOrder { get; internal set; }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var idle = new MoveState("MINION_IDLE", _ => Task.CompletedTask);
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine([idle], idle);
    }

    public virtual Task OnSummon(PlayerChoiceContext ctx, Player owner, MinionSummonOptions options)
        => Task.CompletedTask;
}

public readonly record struct MinionSummonOptions(
    decimal? MaxHp = null,
    decimal? PrimaryStatAmount = null,
    CardModel? Source = null,
    MinionPosition Position = MinionPosition.Front,
    int AccumulatedHp = 0);

public enum MinionPosition { Front = 0, Back, FrontUpper, BackUpper, Upper }
