using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.cards;

namespace NaiWa.powers;

/// <summary>
/// 抽象主义 - 常驻：每消耗1张糖化，对随机敌人造成等同于当前消耗堆中糖化数量的伤害。
/// </summary>
public sealed class NaiWaChouXiangPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    /// <summary>每当拥有者（玩家）消耗1张糖化时调用：对随机敌人造成等同于当前消耗堆中糖化数量的伤害。</summary>
    public static async Task OnTangHuaExhausted(PlayerChoiceContext ctx, Player? player)
    {
        if (!CombatManager.Instance.IsInProgress) return;
        if (player?.Creature == null || !player.Creature.IsAlive) return;
        if (player.PlayerCombatState == null) return;
        // 伤害 = 当前消耗堆中糖化数量
        int damage = player.PlayerCombatState.ExhaustPile.Cards.Count(c => c is NaiWaTangHua);
        if (damage <= 0) return;
        var enemies = player.Creature.CombatState?.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies == null || enemies.Count == 0) return;
        foreach (var power in player.Creature.GetPowerInstances<NaiWaChouXiangPower>())
        {
            if (power.Amount <= 0) continue;
            var target = enemies[player.Creature.CombatState.RunState.Rng.CombatCardSelection.NextInt(enemies.Count)];
            power.Flash();
            await CreatureCmd.Damage(ctx, target, damage, ValueProp.Unpowered, player.Creature);
        }
    }

    /// <summary>升级后：每回合开始时向消耗堆添加1张糖化（不触发消耗钩子）。</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (player.Creature != base.Owner) return;
        if (base.Amount < 2) return; // 仅升级生效
        if (player.PlayerCombatState == null || player.Creature.CombatState == null) return;
        var card = player.Creature.CombatState.CreateCard<NaiWaTangHua>(player);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Exhaust, player);
    }
}
