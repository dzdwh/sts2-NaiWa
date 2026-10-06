#if !TOOLS
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace NaiWa.timeline;

public sealed class NaiWaCluesEpoch : EpochModel
{
    public const string EpochId = "NAI_WA3_EPOCH";
    public const string PortraitPath = "res://images/timeline/epoch_portraits/nai_wa3_epoch.png";

    public override string Id => EpochId;
    public override EpochEra Era => EpochEra.Seeds3;
    public override int EraPosition => 3;
    public override string StoryId => "NaiWa";

    public override EpochModel[] GetTimelineExpansion() => [EpochModel.Get<NaiWaImprisonmentEpoch>()];

    public static List<CardModel> Cards =>
        [ModelDb.Card<cards.NaiWaNaiLong>(), ModelDb.Card<cards.NaiWaDaKouNaiJing>(), ModelDb.Card<cards.NaiWaXiaoNaiZhu>()];

    public override string UnlockText => CreateCardUnlockText(Cards);
    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueCardUnlock(Cards);
        EpochModel.QueueTimelineExpansion(GetTimelineExpansion());
    }
}
#endif
