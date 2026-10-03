using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.minions;

namespace NaiWa.powers;

/// <summary>
/// 奶格拉底之死 - 每回合失去1点生命值，所有随从召唤（提升）Amount 点生命值。
/// </summary>
public sealed class NaiWaNaiGeLaDiZhiSiPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (player.Creature != base.Owner) return;
        if (base.Amount <= 0) return;

        // 每回合失去1点生命值（无格挡生命值损失）
        await CreatureCmd.Damage(choiceContext, base.Owner, 1m,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);

        // 所有随从召唤（提升）Amount 点生命值
        var pets = base.Owner.CombatState?.Allies
            .Where(a => a.IsAlive && a.PetOwner?.Creature == base.Owner)
            .ToList();
        if (pets == null || pets.Count == 0) return;
        foreach (var pet in pets)
            await CreatureCmd.GainMaxHp(pet, base.Amount);
        if (base.Owner.Player != null)
            await MinionCmd.FireSummonEffect(base.Owner.Player); // 奶格拉底之死加最大生命值属于召唤效果
    }
}
