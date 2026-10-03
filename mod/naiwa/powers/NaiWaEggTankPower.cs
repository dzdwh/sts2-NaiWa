using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace NaiWa.powers;

/// <summary>承伤由 NaiWaMinionPowerBase 统一处理（蛋永远最高优先级）。</summary>
public sealed class NaiWaEggTankPower : NaiWaMinionPowerBase
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (player != base.Owner.PetOwner) return;
        await TriggerTurnEffect(ctx, player);
    }

    /// <summary>每回合效果：奶蛋 +2 最大生命。</summary>
    public override async Task TriggerTurnEffect(PlayerChoiceContext ctx, Player owner)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        await CreatureCmd.GainMaxHp(base.Owner, 2);
    }
}
