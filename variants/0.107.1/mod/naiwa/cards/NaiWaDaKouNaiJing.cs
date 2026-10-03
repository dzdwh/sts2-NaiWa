using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa.minions;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 大口奶鲸 - 1费uncommon技能牌。奶鲸召唤1。升级获得保留。
/// </summary>
public sealed class NaiWaDaKouNaiJing : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("JingHp", 1m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaJingTankPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_da_kou_nai_jing.png";

    public NaiWaDaKouNaiJing() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await MinionCmd.AddMinion<NaiWaJingMinion>(choiceContext, Owner,
            new MinionSummonOptions(DynamicVars["JingHp"].IntValue, Source: this, Position: MinionPosition.Front));
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain); // 升级获得保留
    }
}
