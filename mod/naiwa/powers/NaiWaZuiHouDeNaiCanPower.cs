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
/// 最后的奶餐 - 回合开始时将一张奶蛙食物加入手牌。
/// Amount >= 2 表示升级版（加入升级过的食物）。
/// </summary>
public sealed class NaiWaZuiHouDeNaiCanPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (player.Creature != base.Owner) return;

        var rng = player.RunState.Rng.CombatCardSelection;
        var pick = new[] { 0, 1, 2, 3, 4, 5, 6 };
        rng.Shuffle(pick);
        CardModel food = pick[0] switch
        {
            0 => CombatState.CreateCard<NaiWaNailiu>(player),
            1 => CombatState.CreateCard<NaiWaNaiTaoGuanTou>(player),
            2 => CombatState.CreateCard<NaiWaNaiQiLin>(player),
            3 => CombatState.CreateCard<NaiWaHuoBaoNaiJiao>(player),
            4 => CombatState.CreateCard<NaiWaNaiGua>(player),
            5 => CombatState.CreateCard<NaiWaMoLiNaiLv>(player),
            _ => CombatState.CreateCard<NaiWaNaiYouGuo>(player),
        };

        var addResult = await CardPileCmd.AddGeneratedCardToCombat(food, PileType.Hand, player);
        if (base.Amount >= 2 && addResult.cardAdded != null)
            CardCmd.Upgrade(addResult.cardAdded);
    }
}
