using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NaiWa.cards;

/// <summary>
/// 梦龙 - 1费罕见技能牌。选择手牌中至多3张糖化移除，每移除1张获得6（8）点格挡。
/// </summary>
public sealed class NaiWaMengLong : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(6m, ValueProp.Move),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_meng_long.png";

    public NaiWaMengLong() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Do not open a synchronized selection for a player no longer present
        // in the run (GetChoiceId otherwise indexes slot -1).
        if (Owner.RunState.GetPlayerSlotIndex(Owner) < 0) return;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 选择手牌中任意张糖化移除，每移除1张获得格挡
        var handTangHua = Owner.PlayerCombatState.Hand.Cards
            .Where(c => c is NaiWaTangHua)
            .Cast<NaiWaTangHua>()
            .ToList();
        if (handTangHua.Count == 0) return;

        var picked = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 0, handTangHua.Count),
            context: choiceContext,
            player: Owner,
            filter: c => c is NaiWaTangHua,
            source: this)).Cast<NaiWaTangHua>().ToList();

        if (picked.Count == 0) return;
        await NaiWaTangHua.RemoveSpecific(choiceContext, Owner, picked);

        int block = DynamicVars.Block.IntValue * picked.Count;
        await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m); // 6→8
    }
}
