using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.powers;

/// <summary>
/// 将军击落了一只奶娃 - 回合开始时，从抽牌堆选择一张牌加入手牌。
/// </summary>
public sealed class NaiWaJiangJunJiLuoLeYiZhiNaiWaPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player.Creature != base.Owner || !base.Owner.IsAlive) return;

        // 对齐原版 ForegoneConclusionPower：叠加后一次选择 Amount 张，
        // 只创建一个多人 choice id，避免连续选牌导致同步时序错位。
        var pickedCards = await CardSelectCmd.FromCombatPile(
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, base.Amount),
            context: choiceContext,
            pile: PileType.Draw.GetPile(player),
            player: player);
        foreach (var picked in pickedCards)
        {
            await CardPileCmd.Add(picked, PileType.Hand);
        }
    }
}
