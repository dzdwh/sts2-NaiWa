using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.powers;

/// <summary>
/// 堕落奶蛙 - 每当你大笑1次，抽1张牌。
/// </summary>
public sealed class NaiWaDuoLuoNaiWaPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>每当拥有者（玩家）大笑时调用：抽1张牌。随从/敌人大笑不计。</summary>
    public static async Task OnAnyLaugh(PlayerChoiceContext ctx, Player? player)
    {
        if (!CombatManager.Instance.IsInProgress) return;
        if (player?.Creature == null || !player.Creature.IsAlive) return;
        var power = player.Creature.GetPowerInstances<NaiWaDuoLuoNaiWaPower>().FirstOrDefault();
        if (power == null) return;
        power.Flash();
        // Counter 叠加：叠了几层就抽几张（修复"不能叠加"——之前写死抽 1）
        await CardPileCmd.Draw(ctx, power.Amount, player);
    }
}
