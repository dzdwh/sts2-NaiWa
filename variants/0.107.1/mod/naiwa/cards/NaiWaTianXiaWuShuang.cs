using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 天下无双 — 3费先古能力牌。固有。虚无。获得3力量+3敏捷。
/// 即将大笑时阻止并获得1力量。即将召唤随从时阻止并获得1敏捷。升级后阻止获得2力量+2敏捷。
/// </summary>
public sealed class NaiWaTianXiaWuShuang : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<StrengthPower>(3m),
        new PowerVar<DexterityPower>(3m),
        new DynamicVar("Gain", 1m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        MegaCrit.Sts2.Core.Entities.Cards.CardKeyword.Innate,
        MegaCrit.Sts2.Core.Entities.Cards.CardKeyword.Ethereal,
    };

    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaLaughPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_tian_xia_wu_shuang.png";

    public NaiWaTianXiaWuShuang() : base(2, CardType.Power, CardRarity.Ancient, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var strAmt = DynamicVars["StrengthPower"].IntValue;
        var dexAmt = DynamicVars["DexterityPower"].IntValue;
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, strAmt, Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, dexAmt, Owner.Creature, this);
        var txPower = await PowerCmd.Apply<NaiWaTianXiaWuShuangPower>(
            choiceContext, Owner.Creature, DynamicVars["Gain"].IntValue, Owner.Creature, this);
        if (txPower != null)
            txPower.DynamicVars["Gain"].BaseValue = txPower.Amount;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 3→2
        DynamicVars["Gain"].UpgradeValueBy(1m); // 1→2
    }
}
