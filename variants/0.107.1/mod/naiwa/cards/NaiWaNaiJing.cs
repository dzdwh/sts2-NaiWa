using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace NaiWa.cards;

/// <summary>
/// 奶惊 - 2费uncommon技能牌。获得12/15格挡。所有意图为攻击的敌人失去1/2点力量。
/// </summary>
public sealed class NaiWaNaiJing : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(12m, ValueProp.Move),
        new DynamicVar("StrengthLoss", 1m),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_jing.png";

    public NaiWaNaiJing() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["Block"].IntValue, ValueProp.Move, cardPlay);

        int loss = DynamicVars["StrengthLoss"].IntValue;
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive && e.Monster?.IntendsToAttack == true))
            await PowerCmd.Apply<StrengthPower>(choiceContext, enemy, -loss, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Block"].UpgradeValueBy(3m);        // 12→15
        DynamicVars["StrengthLoss"].UpgradeValueBy(1m); // 1→2
    }
}
