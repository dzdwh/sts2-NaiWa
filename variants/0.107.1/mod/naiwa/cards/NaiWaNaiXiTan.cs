using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 奶兮叹 - 3费uncommon技能牌。获得29/38点格挡；消耗手牌中所有糖化，
/// 若消耗不少于3张，下回合获得原格挡一半（14/19）的格挡。
/// </summary>
public sealed class NaiWaNaiXiTan : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(29m, ValueProp.Move),
        new DynamicVar("HalfBlock", 14m),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaLaughPower>(),
        HoverTipFactory.FromPower<NaiWaLaughCardPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_xi_tan.png";

    public NaiWaNaiXiTan() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        if (await NaiWaXiao.TriggerLaughTracking(choiceContext, Owner))
            NaiWaXiao.TriggerLaughVisual(Owner);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["Block"].IntValue, ValueProp.Move, cardPlay);

        // 消耗手牌中所有糖化；若不少于3张，下回合获得原格挡一半的格挡
        int handCount = NaiWaTangHua.CountInHand(Owner);
        if (handCount > 0)
            await NaiWaTangHua.RemoveFromHand(choiceContext, Owner, handCount);
        if (handCount >= 3)
            await PowerCmd.Apply<BlockNextTurnPower>(choiceContext, Owner.Creature,
                DynamicVars["HalfBlock"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Block"].UpgradeValueBy(9m); // 29→38
        DynamicVars["HalfBlock"].UpgradeValueBy(5m); // 14→19
    }
}
