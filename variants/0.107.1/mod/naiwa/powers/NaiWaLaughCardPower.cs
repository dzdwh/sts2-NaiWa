using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.powers;

/// <summary>
/// 大笑牌 — 仅用于卡面侧边预览，无实际逻辑。标记该牌打出后会触发大笑。
/// </summary>
public sealed class NaiWaLaughCardPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
}
