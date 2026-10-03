using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace NaiWa.powers;

/// <summary>奶鸡存活期间，回合开始时给予主人1点能量，每10点生命额外+1。</summary>
public sealed class NaiWaNaiJiTankPower : NaiWaMinionPowerBase
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (player != base.Owner.PetOwner) return;
        await TriggerTurnEffect(ctx, player);
    }

    /// <summary>每回合效果：主人获得 1+自身生命/15 点能量。</summary>
    public override async Task TriggerTurnEffect(PlayerChoiceContext ctx, Player owner)
    {
        if (base.Owner == null || !base.Owner.IsAlive) return;
        var energy = 1 + base.Owner.CurrentHp / 15;
        await PlayerCmd.GainEnergy((decimal)energy, owner);
    }
}
