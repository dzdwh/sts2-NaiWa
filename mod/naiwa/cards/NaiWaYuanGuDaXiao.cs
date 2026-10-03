using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NaiWa;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>远古大笑 - 先古卡，1费。打出后大笑，获得12/18活力，给予所有敌人3虚弱。升级0费。</summary>
public sealed class NaiWaYuanGuDaXiao : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<VigorPower>(12m),
        new PowerVar<WeakPower>(3m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaLaughPower>(),
        HoverTipFactory.FromPower<NaiWaLaughCardPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_yuan_gu_da_xiao.png";

    public NaiWaYuanGuDaXiao() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState;
        if (combatState == null) return;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        if (await NaiWaXiao.TriggerLaughTracking(choiceContext, Owner))
            NaiWaXiao.TriggerLaughVisual(Owner);

        var vigorAmount = DynamicVars["VigorPower"].IntValue;
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, vigorAmount, Owner.Creature, this);

        // Power hooks may mutate the combat creature collection while applying.
        // Snapshot targets before yielding to PowerCmd.
        var enemies = combatState.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count > 0)
            await PowerCmd.Apply<WeakPower>(choiceContext, enemies, 3, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["VigorPower"].UpgradeValueBy(3m); // 12→15
        EnergyCost.UpgradeBy(-1); // 1费→0费
    }
}
