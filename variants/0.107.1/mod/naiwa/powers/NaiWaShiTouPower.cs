using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NaiWa;

namespace NaiWa.powers;

public sealed class NaiWaShiTouPower : PowerModel
{
    private class Data
    {
        public int XiaoCount;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData() => new Data();

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != base.Owner) return;
        if (!NaiWaKeywords.IsLaughCard(cardPlay.Card)) return;
        if (NaiWaTianXiaWuShuangPower.IsActive(base.Owner)) return; // 天下无双封锁

        var data = GetInternalData<Data>();
        data.XiaoCount++;
        // Amount holds the strength value (2 base, 3 upgraded)
        await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner, base.Amount, base.Owner, null);
    }

    public void ApplyHpLoss()
    {
        var data = GetInternalData<Data>();
        if (data.XiaoCount <= 0 || base.Owner == null || !base.Owner.IsAlive) return;
        var newHp = base.Owner.CurrentHp - data.XiaoCount;
        if (newHp < 1) newHp = 1;
        base.Owner.SetCurrentHpInternal(newHp);
    }
}
