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
/// 始祖尾巴 — 糖分触发阈值改为最大生命值；回合开始时手牌每有1张糖化获得3层活力。
/// </summary>
public sealed class ShiZuWeiBa : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override string PackedIconPath =>
        "res://images/relics/shi_zu_wei_ba.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Percent", 50m),
        new DynamicVar("PerVigor", 5m),
    };

    /// <summary>Effect implemented in NaiWaTangFenPower.AfterPlayerTurnStart — loses 50% sugar + Vigor.</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player != base.Owner) return;
        int count = NaiWaTangHua.CountInHand(player);
        if (count > 0)
            await PowerCmd.Apply<VigorPower>(choiceContext, base.Owner.Creature, count * 3, base.Owner.Creature, null);
    }
}
