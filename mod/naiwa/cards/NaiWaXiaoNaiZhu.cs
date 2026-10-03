using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa.minions;

using NaiWa.powers;
namespace NaiWa.cards;

/// <summary>
/// 小奶猪 - 2费rare技能。消耗所有手牌。每消耗1张牌，奶猪召唤3（4）。
/// </summary>
public sealed class NaiWaXiaoNaiZhu : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaNaiZhuTankPower>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("PerCardHp", 3m),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_xiao_nai_zhu.png";

    public NaiWaXiaoNaiZhu() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // Count and exhaust all other cards in hand
        var hand = Owner.PlayerCombatState.Hand;
        var otherCards = hand.Cards.Where(c => c != this).ToList();
        int count = otherCards.Count;

        foreach (var card in otherCards)
            await CardCmd.Exhaust(choiceContext, card, causedByEthereal: false);

        if (count > 0)
        {
            int hp = count * DynamicVars["PerCardHp"].IntValue;
            await MinionCmd.AddMinion<NaiWaNaiZhuMinion>(choiceContext, Owner,
                new MinionSummonOptions(hp, Source: this, Position: MinionPosition.Front));
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PerCardHp"].UpgradeValueBy(1m); // 3→4
    }
}
