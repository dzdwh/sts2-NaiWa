using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace NaiWa.cards;

/// <summary>
/// 过量抑制 - 0费罕见技能牌。消耗手牌中的所有糖化，每消耗1张下回合获得3点格挡。升级获得保留。
/// </summary>
public sealed class NaiWaGuoLiangYiZhi : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("BlockGain", 3m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaTangHua>(),
        HoverTipFactory.FromPower<BlockNextTurnPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_guoliangyizhi.png";

    public NaiWaGuoLiangYiZhi() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 消耗手牌中的所有糖化（不再选择）
        int count = NaiWaTangHua.CountInHand(Owner);
        if (count == 0) return;

        await NaiWaTangHua.RemoveFromHand(choiceContext, Owner, count);
        // 每消耗1张糖化，下回合获得 BlockGain 点格挡
        await PowerCmd.Apply<BlockNextTurnPower>(choiceContext, Owner.Creature,
            count * DynamicVars["BlockGain"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain); // 升级获得保留
    }
}
