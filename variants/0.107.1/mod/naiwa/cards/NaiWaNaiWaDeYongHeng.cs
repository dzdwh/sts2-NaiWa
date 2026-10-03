using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 奶蛙的永恒 - 0费rare攻击。永恒。对所有敌人造成33（44）伤害。
/// 耗能增加0。每次打出这个数值永久+1。
/// 完全模仿 GeneticAlgorithm/TheScythe/永远的奶鼠 的 [SavedProperty] + DeckVersion 模式。
/// TryModifyEnergyCostInCombat 确保 host/client 扣费一致。
/// </summary>
public sealed class NaiWaNaiWaDeYongHeng : CardModel
{
    private int _increasedCost;

    private int TotalCost => _increasedCost;

    [SavedProperty]
    public int IncreasedCost
    {
        get => _increasedCost;
        set
        {
            AssertMutable();
            _increasedCost = value;
            try { base.DynamicVars["Cost"].BaseValue = TotalCost; } catch { }
        }
    }

    /// <summary>Always return 0. Actual cost comes from TryModifyEnergyCostInCombat.
    /// If this returned TotalCost, EnergyCost._base would differ between host/client
    /// after deserialization → serialized Energy Cost mismatch → State Divergence.</summary>
    protected override int CanonicalEnergyCost => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(33m, ValueProp.Move),
        new EnergyVar("Cost", TotalCost),
        new DynamicVar("CostUp", 1),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Eternal,
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => System.Array.Empty<IHoverTip>();

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_wa_de_yong_heng.png";

    public NaiWaNaiWaDeYongHeng() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }

    /// <summary>
    /// Ensure host and client charge the same energy. Called after _base is read,
    /// before energy is spent. This is the only reliable cross-machine sync point.
    /// </summary>
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card == this && _increasedCost > 0)
        {
            modifiedCost = _increasedCost;
            return true;
        }
        modifiedCost = originalCost;
        return false;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);

        int costUp = base.DynamicVars["CostUp"].IntValue;
        IncreasedCost += costUp;
        base.DynamicVars["Cost"].BaseValue = TotalCost;
        (base.DeckVersion as NaiWaNaiWaDeYongHeng)?.BuffFromPlay(costUp);

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(11m); // 33→44
    }

    public void BuffFromPlay(int extra)
    {
        IncreasedCost += extra;
        base.DynamicVars["Cost"].BaseValue = TotalCost;
    }
}
