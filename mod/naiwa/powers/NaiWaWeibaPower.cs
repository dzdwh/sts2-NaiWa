using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.powers;

/// <summary>
/// 奶蛙尾巴 - 纯视觉 Power，用于奶蛙尾巴生效时在状态栏闪烁图标。
/// </summary>
public sealed class NaiWaWeibaPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
}
