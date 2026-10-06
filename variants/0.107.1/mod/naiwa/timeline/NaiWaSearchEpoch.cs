#if !TOOLS
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace NaiWa.timeline;

public sealed class NaiWaSearchEpoch : EpochModel
{
    public const string EpochId = "NAI_WA2_EPOCH";
    public const string PortraitPath = "res://images/timeline/epoch_portraits/nai_wa2_epoch.png";

    public override string Id => EpochId;
    public override EpochEra Era => EpochEra.Seeds0;
    public override int EraPosition => 2;
    public override string StoryId => "NaiWa";

    public override EpochModel[] GetTimelineExpansion() => [EpochModel.Get<NaiWaCluesEpoch>()];

    public static List<CardModel> Cards =>
        [ModelDb.Card<cards.NaiWaDao>(), ModelDb.Card<cards.NaiWaJiangTang>(), ModelDb.Card<cards.NaiWaShenFu>()];

    public override string UnlockText => CreateCardUnlockText(Cards);
    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueCardUnlock(Cards);
        EpochModel.QueueTimelineExpansion(GetTimelineExpansion());
    }
}
#endif
