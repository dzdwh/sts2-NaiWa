using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa.cards;

namespace NaiWa.powers;

/// <summary>
/// 自画像 - 回合开始时，选择手牌中任意张糖化移除，每移除1张大笑1次。
/// </summary>
public sealed class NaiWaZiHuaXiangPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar(1),
    };

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (player.Creature != base.Owner) return;

        // 选择手牌中至多 2×层数 张糖化移除（每层至多2张）
        var handTangHua = player.PlayerCombatState.Hand.Cards
            .Where(c => c is NaiWaTangHua)
            .Cast<NaiWaTangHua>()
            .ToList();
        if (handTangHua.Count == 0) return;

        int maxRemove = 2 * base.Amount;
        var picked = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 0, Math.Min(maxRemove, handTangHua.Count)),
            context: choiceContext,
            player: player,
            filter: c => c is NaiWaTangHua,
            source: this)).Cast<NaiWaTangHua>().ToList();

        if (picked.Count == 0) return;
        await NaiWaTangHua.RemoveSpecific(choiceContext, player, picked);

        // 每移除1张大笑1次
        for (int i = 0; i < picked.Count; i++)
        {
            if (await NaiWaXiao.TriggerLaughTracking(choiceContext, player))
                NaiWaXiao.TriggerLaughVisual(player);
        }

        // 每消耗2张糖化 → 获得1能量（叠加后按张数比例）
        int energy = picked.Count / 2 * DynamicVars["Energy"].IntValue;
        if (energy > 0)
            await PlayerCmd.GainEnergy(energy, player);
    }
}
