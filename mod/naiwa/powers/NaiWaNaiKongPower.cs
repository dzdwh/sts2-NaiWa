using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NaiWa.cards;

namespace NaiWa.powers;

/// <summary>
/// 奶空 - 回合开始时获得1张糖化，能量上限+1。
/// </summary>
public sealed class NaiWaNaiKongPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override decimal ModifyMaxEnergy(Player player, decimal currentMax)
    {
        if (player != Owner?.Player) return currentMax;
        return currentMax + 1;
    }

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        await NaiWaTangHua.GiveToHand(choiceContext, player);
    }
}
