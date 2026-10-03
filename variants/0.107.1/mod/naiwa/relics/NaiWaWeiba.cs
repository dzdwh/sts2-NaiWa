using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using NaiWa.cards;
using NaiWa.powers;

namespace NaiWa.relics;

/// <summary>
/// 奶蛙尾巴 - 糖分触发阈值改为最大生命值；回合开始时手牌每有1张糖化获得1层活力。
/// </summary>
public sealed class NaiWaWeiba : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override string PackedIconPath =>
        "res://images/relics/nai_wa_weiba.png";

    // Effect is implemented in NaiWaTangFenPower.AfterPlayerTurnStart —
    // runs BEFORE the overflow check, loses 50% sugar at turn start.
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player != base.Owner) return;
        int count = NaiWaTangHua.CountInHand(player);
        if (count > 0)
            await PowerCmd.Apply<VigorPower>(choiceContext, base.Owner.Creature, count, base.Owner.Creature, null);
    }
}
