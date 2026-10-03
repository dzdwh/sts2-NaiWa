using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 石头与奶奶弗斯 - 1费能力。每打出一张大笑牌获得1力量，战斗结束失去等量HP。升级后获得固有。
/// </summary>
public sealed class NaiWaShiTou : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<NaiWaShiTouPower>(2m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaLaughPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_shitou.png";

    public NaiWaShiTou() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<NaiWaShiTouPower>(choiceContext, Owner.Creature,
            DynamicVars["NaiWaShiTouPower"].IntValue, Owner.Creature, this);
    }
}
