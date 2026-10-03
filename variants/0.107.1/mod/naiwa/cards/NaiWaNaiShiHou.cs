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

namespace NaiWa.cards;

/// <summary>
/// 奶时候 - 1费罕见技能牌。选择手牌中至多3张糖化移除；
/// 选择抽牌堆至多2（3）张牌消耗，每移除1张糖化可额外选择1张。
/// </summary>
public sealed class NaiWaNaiShiHou : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("ExhaustMax", 2m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        NaiWaKeywords.YuanShu,
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_shi_hou.png";

    public NaiWaNaiShiHou() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 1. 选择手牌中至多3张糖化移除
        int removed = 0;
        var handTangHua = Owner.PlayerCombatState.Hand.Cards
            .Where(c => c is NaiWaTangHua)
            .Cast<NaiWaTangHua>()
            .ToList();
        if (handTangHua.Count > 0)
        {
            var picked = (await CardSelectCmd.FromHand(
                prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 0, Math.Min(3, handTangHua.Count)),
                context: choiceContext,
                player: Owner,
                filter: c => c is NaiWaTangHua,
                source: this)).Cast<NaiWaTangHua>().ToList();
            if (picked.Count > 0)
            {
                removed = picked.Count;
                await NaiWaTangHua.RemoveSpecific(choiceContext, Owner, picked);
            }
        }

        // 2. 选择抽牌堆至多 (2/3 + 移除糖化数) 张牌消耗
        int max = DynamicVars["ExhaustMax"].IntValue + removed;
        var exhausted = (await CardSelectCmd.FromCombatPile(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, max),
            context: choiceContext,
            pile: PileType.Draw.GetPile(Owner),
            player: Owner)).ToList();
        foreach (var card in exhausted)
            await CardCmd.Exhaust(choiceContext, card);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ExhaustMax"].UpgradeValueBy(1m); // 2→3
    }
}
