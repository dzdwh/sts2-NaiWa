using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.minions;

using NaiWa.powers;
namespace NaiWa.cards;

public sealed class NaiWaNaiLong : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(5m, ValueProp.Move),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaNaiLongTankPower>(),
    };

    public override string PortraitPath => "res://images/card_portraits/nai_wa/nai_wa_nailong.png";

    public NaiWaNaiLong() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        // 1. Deal damage to target enemy, then read the actual damage dealt (Fisticuffs pattern)
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);
        decimal dmg = attack.Results.SelectMany(r => r).Sum(r => r.TotalDamage + r.OverkillDamage);

        // 2. If nailong alive → buff, else → summon
        var nailong = Owner.PlayerCombatState.Pets
            .FirstOrDefault(p => p.IsAlive && p.Monster is NaiWaNaiLongMinion
                && p.PetOwner == Owner);
        if (nailong != null)
        {
            // Use SetMaxHp + SetCurrentHp instead of GainMaxHp to avoid Heal being
            // short-circuited when combat ends (e.g. killing blow on last enemy).
            await CreatureCmd.SetMaxHp(nailong, nailong.MaxHp + dmg);
            await CreatureCmd.SetCurrentHp(nailong, nailong.CurrentHp + dmg);
        }
        else
        {
            await MinionCmd.AddMinion<NaiWaNaiLongMinion>(ctx, Owner,
                new MinionSummonOptions(dmg, Source: this, Position: MinionPosition.Front));
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 5→7
    }
}
