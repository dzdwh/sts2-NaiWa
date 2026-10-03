using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.powers;

/// <summary>
/// 跨越奶尔卑斯山 - 下一张带有召唤效果的牌耗能变为0。
/// 模仿 FreeAttackPower / 奶舞：TryModifyEnergyCostInCombatLate 改临时费用，BeforeCardPlayed 消耗层数。
/// </summary>
public sealed class NaiWaKuaYueNaiErBeiSiShanPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != base.Owner) return false;
        if (!NaiWaKeywords.SummonCardTypes.Contains(card.GetType())) return false;
        if (card.Pile?.Type is not (PileType.Hand or PileType.Play)) return false;
        modifiedCost = 0;
        return true;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature == base.Owner
            && NaiWaKeywords.SummonCardTypes.Contains(cardPlay.Card.GetType())
            && cardPlay.Card.Pile?.Type is PileType.Hand or PileType.Play)
        {
            await PowerCmd.Decrement(this);
        }
    }
}
