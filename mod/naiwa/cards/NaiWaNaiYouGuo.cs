using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NaiWa.NaiWapools;

namespace NaiWa.cards;

/// <summary>
/// 奶油果 - 4费Token技能。虚无。消耗。获得2层无实体。升级-1费。
/// </summary>
public sealed class NaiWaNaiYouGuo : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<SlipperyPower>(2m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Ethereal,
        CardKeyword.Exhaust,
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<SlipperyPower>()
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_you_guo.png";

    public override CardPoolModel VisualCardPool => ModelDb.CardPool<NaiWaCardPool>();

    public NaiWaNaiYouGuo() : base(4, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<SlipperyPower>(choiceContext, Owner.Creature,
            DynamicVars["SlipperyPower"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 4→3
    }
}
