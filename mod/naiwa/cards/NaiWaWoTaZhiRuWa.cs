using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 卧榻之乳蛙 - 1费rare能力牌。每当召唤时，抽1/2张牌。
/// </summary>
public sealed class NaiWaWoTaZhiRuWa : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Draw", 1m),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_wo_ta_zhi_ru_wa.png";

    public NaiWaWoTaZhiRuWa() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<NaiWaWoTaZhiRuWaPower>(choiceContext, Owner.Creature,
            DynamicVars["Draw"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1m); // 1→2
    }
}
