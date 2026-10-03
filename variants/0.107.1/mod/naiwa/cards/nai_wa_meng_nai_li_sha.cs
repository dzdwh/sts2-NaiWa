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
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 蒙奶丽莎 - 0费rare技能牌。丢弃所有手牌，每弃一张牌大笑一次。升级获得保留。
/// </summary>
public sealed class NaiWaMengNaiLiSha : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(10m, ValueProp.Move),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaLaughPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_meng_nai_li_sha.png";

    public NaiWaMengNaiLiSha() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        int discardable = handCards.Count;
        if (discardable > 0)
        {
            await CardCmd.Discard(choiceContext, handCards);
            for (int i = 0; i < handCards.Count; i++)
            {
                if (await NaiWaXiao.TriggerLaughTracking(choiceContext, Owner))
                    NaiWaXiao.TriggerLaughVisual(Owner);
                await Task.Delay(300);
            }
        }

        // 丢弃不少于5张牌 → 获得10格挡
        if (discardable >= 5)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["Block"].IntValue, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
