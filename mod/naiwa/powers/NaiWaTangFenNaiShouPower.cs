using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.powers;

/// <summary>
/// 糖分耐受（怪物）- 每层使糖分溢出阈值 ×2；怪物溢出时 +1 层，未溢出时移除。
/// </summary>
public sealed class NaiWaTangFenNaiShouPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Threshold", 0m),
    };

    /// <summary>层数或生命值变化时，刷新"溢出阈值"显示为计算后的结果。</summary>
    private void RefreshThreshold()
    {
        if (Owner == null) return;
        DynamicVars["Threshold"].BaseValue = NaiWaTangFenPower.GetMonsterThreshold(Owner);
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
            RefreshThreshold();
        return Task.CompletedTask;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner)
            RefreshThreshold();
        return Task.CompletedTask;
    }
}
