using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 心 - 1费uncommon技能。执行2（3）次奶鼠效果（无需奶鼠在场）。消耗。
/// </summary>
public sealed class NaiWaXin : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Times", 2m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        NaiWaKeywords.YuanShu, CardKeyword.Exhaust
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaMouseBuffPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_xin.png";

    public NaiWaXin() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var times = DynamicVars["Times"].IntValue;
        for (int i = 0; i < times; i++)
        {
            await NaiWaMouseBuffPower.TriggerOneEffect(Owner, null);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Times"].UpgradeValueBy(1m); // 2→3
    }
}
