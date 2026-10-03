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
/// 奶蛙食物 - 0费Token技能。消耗。获得4格挡，获得1/2荆棘。
/// </summary>
public sealed class NaiWaNailiu : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ThornsPower>(1m),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<ThornsPower>()
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nailiu.png";

    public override CardPoolModel VisualCardPool => ModelDb.CardPool<NaiWaCardPool>();

    public NaiWaNailiu() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CreatureCmd.GainBlock(Owner.Creature, 4m, MegaCrit.Sts2.Core.ValueProps.ValueProp.Move, cardPlay);
        await PowerCmd.Apply<ThornsPower>(choiceContext, Owner.Creature,
            DynamicVars["ThornsPower"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ThornsPower"].UpgradeValueBy(1m); // 1→2
    }
}
