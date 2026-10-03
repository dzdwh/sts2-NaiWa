using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using NaiWa;

namespace NaiWa.powers;

/// <summary>
/// 绷住 - 任意大笑清空所有层数；若由大笑牌触发，该牌额外打出层数次。
/// </summary>
public sealed class NaiWaBengPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner?.Creature != base.Owner) return playCount;
        if (!NaiWaKeywords.IsLaughCard(card)) return playCount;
        return playCount + base.Amount;
    }

    public override async Task AfterModifyingCardPlayCount(CardModel card)
    {
        await PowerCmd.Remove(this);
    }
}
