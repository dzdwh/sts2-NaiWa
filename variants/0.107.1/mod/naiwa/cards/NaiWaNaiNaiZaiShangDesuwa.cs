using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace NaiWa.cards;

/// <summary>
/// 奶奶在上desuwa - 0费uncommon技能牌。若敌人意图攻击给予2层虚弱，否则2层易伤。消耗（升级去掉）。
/// </summary>
public sealed class NaiWaNaiNaiZaiShangDesuwa : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_nai_nai_zai_shang_desuwa.png";

    public NaiWaNaiNaiZaiShangDesuwa() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var target = cardPlay.Target!;
        if (target.Monster?.IntendsToAttack == true)
            await PowerCmd.Apply<WeakPower>(choiceContext, target, 2, Owner.Creature, this);
        else
            await PowerCmd.Apply<VulnerablePower>(choiceContext, target, 2, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
