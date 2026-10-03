using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NaiWa.cards;

namespace NaiWa.powers;

/// <summary>
/// 超糖形态 - 每回合将手牌抽至9张并消耗手牌中的所有糖化；糖分超过最大生命值时不再跳过回合。
/// </summary>
public sealed class NaiWaChaoTangXingTaiPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>每回合抽牌数改为"把手牌补足到9张"。</summary>
    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != base.Owner?.Player) return count;
        int hand = player.PlayerCombatState?.Hand.Cards.Count ?? 0;
        return Math.Max(0, 9 - hand);
    }

    /// <summary>回合开始时消耗手牌中的所有糖化。</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (player.Creature != base.Owner) return;
        if (player.PlayerCombatState == null) return;
        int count = player.PlayerCombatState.Hand.Cards.Count(c => c is NaiWaTangHua);
        if (count > 0)
            await NaiWaTangHua.RemoveFromHand(choiceContext, player, count);
    }
}
