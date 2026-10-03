using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa;

namespace NaiWa.cards;

/// <summary>
/// 躲避时间 - 1费rare技能牌。将你的血量调整至1回合前。升级获得保留。
/// 血量快照由 NaiWa.cs 的 Hook.AfterPlayerTurnStart Prefix 每回合记录。
/// </summary>
public sealed class NaiWaDuoBiShiJian : CardModel
{
    private static readonly Dictionary<Creature, int> _hpSnapshots = new();
    private static readonly Dictionary<Creature, int> _prevHpSnapshots = new();

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromKeyword(NaiWaKeywords.YuanShu),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_dbsj.png";

    public NaiWaDuoBiShiJian() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    /// <summary>每回合开始记录玩家血量快照（由 Harmony Prefix 调用）。</summary>
    public static void SetHpSnapshot(Creature creature)
    {
        if (creature != null)
        {
            // 当前快照滚动为"上一回合"快照，再记录本回合开始的血量
            if (_hpSnapshots.TryGetValue(creature, out int last))
                _prevHpSnapshots[creature] = last;
            _hpSnapshots[creature] = creature.CurrentHp;
        }
    }

    /// <summary>战斗结束时清理，防止脏数据。</summary>
    public static void ClearHpSnapshots()
    {
        _hpSnapshots.Clear();
        _prevHpSnapshots.Clear();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var creature = Owner.Creature;
        // "1回合前" = 上一回合开始的血量（本回合开始的快照已被敌怪伤害污染，不能用于回血）
        int snapshot = _prevHpSnapshots.TryGetValue(creature, out int v)
            ? v
            : (_hpSnapshots.TryGetValue(creature, out int v2) ? v2 : creature.CurrentHp);
        int current = creature.CurrentHp;

        if (snapshot > current)
            await CreatureCmd.Heal(creature, snapshot - current);
        else if (snapshot < current)
            await CreatureCmd.Damage(choiceContext, creature, current - snapshot,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
    }
}
