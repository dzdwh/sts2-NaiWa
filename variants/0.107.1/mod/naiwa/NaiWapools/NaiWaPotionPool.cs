using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline.Epochs;

namespace NaiWa.NaiWapools;

public sealed class NaiWaPotionPool : PotionPoolModel
{
    public override string EnergyColorName => "nai_wa";
    public override Color LabOutlineColor => new("#7BC67E");

    protected override IEnumerable<PotionModel> GenerateAllPotions()
    {
        return Ironclad4Epoch.Potions;
    }
}
