using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NaiWa.minions;

namespace NaiWa.powers;

/// <summary>奶狗窝 — 每回合开始时，若奶狗不在场则召唤 3/4 HP 奶狗。</summary>
public sealed class NaiWaNaiGouWoPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    /// <summary>召唤 HP 来源卡牌。</summary>
    public cards.NaiWaNaiGouWo? SourceCard { get; set; }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (player.Creature != base.Owner) return;
        if (SourceCard == null) return;

        // 检查奶狗是否已存在
        var existing = player.PlayerCombatState?.Pets?.FirstOrDefault(p =>
            p.IsAlive && p.Monster is NaiWaDogMinion && p.PetOwner == player);
        if (existing != null) return;

        // 多张奶狗窝累计召唤/复活血量（每张窝的 HP 相加）
        int hp = player.Creature.GetPowerInstances<NaiWaNaiGouWoPower>()
            .Where(p => p.SourceCard != null)
            .Sum(p => p.SourceCard.DynamicVars["DogHp"].IntValue);
        if (hp <= 0) return;
        await MinionCmd.AddMinion<NaiWaDogMinion>(ctx, player,
            new MinionSummonOptions(hp, Source: SourceCard, Position: MinionPosition.FrontUpper));
    }
}
