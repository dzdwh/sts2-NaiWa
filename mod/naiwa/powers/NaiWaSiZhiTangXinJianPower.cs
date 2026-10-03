using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace NaiWa.powers;

/// <summary>
/// 四指糖心剑 - 储存糖分值，每次打出额外造成已储存伤害，每第4次打出后重置。
/// Amount = 打出次数，StoredSugar = 累计储存的糖分值。
/// </summary>
public sealed class NaiWaSiZhiTangXinJianPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("StoredSugar", 0m),
    };

    private int _storedSugar;

    [SavedProperty]
    public int StoredSugar
    {
        get => _storedSugar;
        set
        {
            AssertMutable();
            _storedSugar = value;
            DynamicVars["StoredSugar"].BaseValue = value;
        }
    }
}
