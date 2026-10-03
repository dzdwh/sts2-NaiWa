using Godot;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.NaiWapools;

public sealed class NaiWaRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "nai_wa";
    public override Color LabOutlineColor => new("#7BC67E");

    protected override IEnumerable<RelicModel> GenerateAllRelics() => new RelicModel[]
    {
        ModelDb.Relic<relics.NaiWaWeiba>(),
        ModelDb.Relic<relics.ShiZuWeiDai>(),
        ModelDb.Relic<relics.ShiZuWeiBa>(),
        ModelDb.Relic<relics.NaiWaNaiWangZhiLuan>(),
    };
}
