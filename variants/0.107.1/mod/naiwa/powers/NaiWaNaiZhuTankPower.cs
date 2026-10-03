using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NaiWa.powers;

/// <summary>
/// 奶猪存活期间，回合开始时对所有敌人造成等同于自身生命值的伤害。
/// </summary>
public sealed class NaiWaNaiZhuTankPower : NaiWaMinionPowerBase
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (player.Creature != base.Owner.PetOwner?.Creature) return;
        await TriggerTurnEffect(choiceContext, player);
    }

    /// <summary>每回合效果：对所有敌人造成等同自身生命值的伤害。</summary>
    public override async Task TriggerTurnEffect(PlayerChoiceContext ctx, Player owner)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        var enemies = base.Owner.CombatState?.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies == null || enemies.Count == 0) return;

        foreach (var enemy in enemies)
        {
            try
            {
                await CreatureCmd.Damage(ctx, enemy, (decimal)base.Owner.CurrentHp, ValueProp.Unpowered, base.Owner, null);
            }
            catch (Exception ex)
            {
                Log.Error($"[NaiWaNaiZhuTankPower] Damage failed: {ex.Message}");
            }
        }
    }
}
