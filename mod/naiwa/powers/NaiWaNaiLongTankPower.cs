using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.minions;

namespace NaiWa.powers;

/// <summary>
/// 奶龙存活期间，每当主人大笑，对一个随机敌人造成等于自身生命值的伤害。
/// </summary>
public sealed class NaiWaNaiLongTankPower : NaiWaMinionPowerBase
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    /// <summary>
    /// 每当拥有者（玩家）大笑时调用：每个存活奶龙对随机敌人造成等于自身生命值的伤害。
    /// 覆盖大笑牌、绷住、蒙奶丽莎、自画像等所有大笑来源。
    /// </summary>
    public static async Task OnAnyLaugh(PlayerChoiceContext ctx, Player? player)
    {
        if (!CombatManager.Instance.IsInProgress) return;
        if (player?.PlayerCombatState == null) return;
        foreach (var pet in player.PlayerCombatState.Pets)
        {
            if (pet == null || !pet.IsAlive || pet.Monster is not NaiWaNaiLongMinion) continue;
            var power = pet.GetPower<NaiWaNaiLongTankPower>();
            if (power == null) continue;
            var enemies = pet.CombatState?.Enemies.Where(e => e.IsAlive).ToList();
            if (enemies == null || enemies.Count == 0) continue;
            var target = enemies[pet.CombatState.RunState.Rng.CombatCardSelection.NextInt(enemies.Count)];
            try
            {
                await CreatureCmd.Damage(ctx, target, (decimal)pet.CurrentHp, ValueProp.Unpowered, pet);
            }
            catch (Exception ex)
            {
                Log.Error($"[NaiWaNaiLongTankPower] Damage failed: {ex.Message}");
            }
        }
    }

    /// <summary>每回合效果：对随机敌人造成等同自身生命值的伤害（奶脚触发走这里，大笑联动不算）。</summary>
    public override async Task TriggerTurnEffect(PlayerChoiceContext ctx, Player owner)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        var enemies = base.Owner.CombatState?.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies == null || enemies.Count == 0) return;

        var rng = base.Owner.CombatState.RunState.Rng.CombatCardSelection;
        var target = enemies[rng.NextInt(enemies.Count)];
        try
        {
            await CreatureCmd.Damage(ctx, target, (decimal)base.Owner.CurrentHp, ValueProp.Unpowered, base.Owner);
        }
        catch (Exception ex)
        {
            Log.Error($"[NaiWaNaiLongTankPower] Turn-start damage failed: {ex.Message}");
        }
    }
}
