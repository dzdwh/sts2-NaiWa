using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa;

namespace NaiWa.powers;

/// <summary>
/// 奶日葵 - 每大笑3次，获得1能量。DisplayAmount = 距离下次触发的剩余大笑次数。
/// 参考 OrbitPower 实现。
/// </summary>
public sealed class NaiRiKuiPower : PowerModel
{
    private class Data
    {
        public int laughCount;
        public int triggerCount;
    }

    private const int _laughsPerEnergy = 3;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override int DisplayAmount => _laughsPerEnergy - GetInternalData<Data>().laughCount % _laughsPerEnergy;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar(1),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.ForEnergy(this),
    };

    protected override object InitInternalData() => new Data();

    /// <summary>Called by any laugh source (card play, sugar overflow, etc).
    /// Counters increment synchronously (deterministic on host/client);
    /// the energy gain is awaited so both machines apply it in the same command order.</summary>
    public static async Task OnAnyLaugh(Creature creature)
    {
        // 只有玩家（拥有奶日葵的生物）大笑才计数；随从/敌人大笑不计入。
        if (creature == null || !creature.IsAlive || creature.Player == null) return;
        foreach (var power in creature.GetPowerInstances<NaiRiKuiPower>())
        {
            var data = power.GetInternalData<Data>();
            data.laughCount++;
            int triggers = data.laughCount / _laughsPerEnergy - data.triggerCount;
            if (triggers > 0)
            {
                power.Flash();
                await PlayerCmd.GainEnergy(power.Amount * triggers, creature.Player);
                data.triggerCount += triggers;
            }
            power.InvokeDisplayAmountChanged();
        }
    }

}
