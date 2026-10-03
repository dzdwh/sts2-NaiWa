using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.powers;

/// <summary>
/// 大笑 — 仅用于卡面侧边预览，无实际逻辑。实际效果由各 Power 检索 CardKeyword.Laugh 触发。
/// </summary>
public sealed class NaiWaLaughPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
}
