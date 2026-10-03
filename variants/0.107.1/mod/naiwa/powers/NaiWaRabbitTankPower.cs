using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace NaiWa.powers;

/// <summary>
/// 奶兔存活期间，主人获得等于兔当前生命值的敏捷。
/// 兔 HP 变化（受伤/治疗/死亡/复活）实时反映到敏捷上。
/// </summary>
public sealed class NaiWaRabbitTankPower : NaiWaMinionPowerBase
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (base.Owner == null) return;
        if (creature != base.Owner) return;
        var player = base.Owner.PetOwner?.Creature;
        if (player == null) return;
        if (delta == 0m) return;

        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), player, delta, base.Owner, null);
    }
}
