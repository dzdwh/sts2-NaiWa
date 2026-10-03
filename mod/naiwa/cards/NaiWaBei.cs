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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace NaiWa.cards;

public sealed class NaiWaBei : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<VigorPower>(3m),
        new BlockVar(6m, ValueProp.Move),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaTangHua>(),
        HoverTipFactory.FromPower<VigorPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_bei.png";

    public NaiWaBei() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 获得6（9）点格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 选择手牌中任意张糖化移除
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

        int lost = picked.Count;
        if (lost <= 0) return;

        await NaiWaTangHua.RemoveSpecific(choiceContext, Owner, picked);

        // 每移除1张获得3点活力
        int vigor = lost * DynamicVars["VigorPower"].IntValue;
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, vigor, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m); // 6→9；活力固定3不随升级提高
    }
}
