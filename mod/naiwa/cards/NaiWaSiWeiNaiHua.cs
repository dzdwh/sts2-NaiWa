using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 思维奶化 - 1费uncommon技能。获得2能量。若本回合获得过糖分则额外获得1/2能量。然后获得10糖分。
/// </summary>
public sealed class NaiWaSiWeiNaiHua : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar(2),
        new EnergyVar("Bonus", 1),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    /// <summary>Glow when 糖化 has already been gained this turn (bonus energy active).</summary>
    protected override bool ShouldGlowGoldInternal =>
        Owner != null && NaiWaTangHua.HasGainedThisTurn(Owner);

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_siweinaihua.png";

    public NaiWaSiWeiNaiHua() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue, Owner);

        // 检查本回合是否已获得过糖分（必须在加糖分之前判断）
        if (NaiWaTangHua.HasGainedThisTurn(Owner))
            await PlayerCmd.GainEnergy(DynamicVars["Bonus"].IntValue, Owner);

        // 最后才加10糖分，不影响上面的判断
        await NaiWaTangHua.GiveToHand(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Bonus"].UpgradeValueBy(1m); // 1→2
    }
}
