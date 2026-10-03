using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 救赎 - 4费罕见技能。获得2（3）能量。本场自己每大笑一次，费用-1。
/// </summary>
public sealed class NaiWaJiushu : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar(2),
        new EnergyVar("Reduce", 1),
    };

    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaLaughPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_jiushu.png";

    public NaiWaJiushu() : base(4, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card == this && !IsClone)
        {
            // Read from CombatHistory (survives SL) — same pattern as Supermassive.
            int laughCount = CombatManager.Instance.History.Entries
                .OfType<CardPlayFinishedEntry>()
                .Count(e => NaiWaKeywords.IsLaughCard(e.CardPlay.Card));
            if (laughCount > 0)
                EnergyCost.AddThisCombat(-laughCount, reduceOnly: true);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 统一大笑入口联动：任意玩家大笑一次，本场所有救赎费用-1。
    /// 由 NaiWaXiao.TriggerLaughTracking 调用，覆盖打出大笑牌、
    /// 自画像回合开始大笑、糖分溢出、绷住等所有来源（天下无双封锁时入口不会到达这里）。
    /// </summary>
    internal static Task OnAnyLaugh(PlayerChoiceContext ctx, Player player)
    {
        if (player?.PlayerCombatState != null)
        {
            foreach (var card in player.PlayerCombatState.AllCards)
            {
                if (card is NaiWaJiushu j)
                    j.EnergyCost.AddThisCombat(-1, reduceOnly: true);
            }
        }
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Energy"].UpgradeValueBy(1m);
    }
}
