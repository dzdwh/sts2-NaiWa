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
using NaiWa;

namespace NaiWa.powers;

/// <summary>
/// 奶蛙的世界 - 每当你大笑1次，获得 Amount 点格挡。随从/敌人大笑不计。
/// </summary>
public sealed class NaiWaShiJiePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>每当拥有者（玩家）大笑时调用：获得 Amount 点格挡。</summary>
    public static async Task OnAnyLaugh(PlayerChoiceContext ctx, Player? player)
    {
        if (!CombatManager.Instance.IsInProgress) return;
        if (player?.Creature == null || !player.Creature.IsAlive) return;
        var power = player.Creature.GetPowerInstances<NaiWaShiJiePower>().FirstOrDefault();
        if (power == null || power.Amount <= 0) return;
        power.Flash();
        await CreatureCmd.GainBlock(player.Creature, power.Amount, 0, null);
    }
}
