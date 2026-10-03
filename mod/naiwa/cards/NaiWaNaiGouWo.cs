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

/// <summary>奶狗窝 - 1费Power，回合开始时若奶狗不在场则召唤3/4HP奶狗。</summary>
public sealed class NaiWaNaiGouWo : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("DogHp", 3m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaDogTankPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_gou_wo.png";

    public NaiWaNaiGouWo() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var hp = DynamicVars["DogHp"].IntValue;
        // 首次召唤
        await MinionCmd.AddMinion<NaiWaDogMinion>(ctx, Owner,
            new MinionSummonOptions(hp, Source: this, Position: MinionPosition.FrontUpper));
        // 加 Power，之后每回合开始检查是否需要重新召唤
        var power = await PowerCmd.Apply<NaiWaNaiGouWoPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        if (power != null)
            power.SourceCard = this;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DogHp"].UpgradeValueBy(1m); // 3→4
    }
}
