using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NaiWa.cards;

namespace NaiWa.powers;

/// <summary>
/// 奶悲临时力量 - 本回合获得力量，回合结束消失。
/// </summary>
public sealed class NaiWaBeiPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<NaiWaBei>();
}
