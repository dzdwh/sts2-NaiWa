using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.minions;

namespace NaiWa.cards;

/// <summary>
/// 奶怒 - 4费uncommon攻击牌。对所有敌人造成28/36点伤害。
/// 每有一名自己的存活随从，耗能减少1。
/// </summary>
public sealed class NaiWaNaiNu : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(28m, ValueProp.Move),
        new EnergyVar("Reduce", 1),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_nu.png";

    public NaiWaNaiNu() : base(4, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override bool ShouldGlowGoldInternal => GetMinionCount() > 0;

    public int GetMinionCount()
    {
        if (Owner?.Creature?.CombatState == null) return 0;
        return Owner.Creature.CombatState.Allies.Count(creature =>
            creature.IsAlive
            && creature.PetOwner == Owner
            && creature.Monster is MinionModel);
    }

    /// <summary>随从进出场时通知所有奶怒刷新手牌费用。</summary>
    public static void RefreshCostVisuals(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player?.PlayerCombatState == null) return;
        foreach (var card in player.PlayerCombatState.AllCards.OfType<NaiWaNaiNu>())
            card.InvokeEnergyCostChanged();
    }

    /// <summary>权威扣费：每名自己的存活随从使费用减少1，最低为0。</summary>
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this || Owner == null) return false;
        int minionCount = GetMinionCount();
        if (minionCount == 0) return false;
        modifiedCost = decimal.Max(0m, originalCost - minionCount * DynamicVars["Reduce"].BaseValue);
        return modifiedCost != originalCost;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m); // 28→36
    }
}
