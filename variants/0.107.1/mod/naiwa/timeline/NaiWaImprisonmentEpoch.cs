#if !TOOLS
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace NaiWa.timeline;

public sealed class NaiWaImprisonmentEpoch : EpochModel
{
    public const string EpochId = "NAI_WA4_EPOCH";
    public const string PortraitPath = "res://images/timeline/epoch_portraits/nai_wa4_epoch.png";

    public override string Id => EpochId;
    public override EpochEra Era => EpochEra.Blight2;
    public override int EraPosition => 4;
    public override string StoryId => "NaiWa";

    public override EpochModel[] GetTimelineExpansion() => [EpochModel.Get<NaiWaSufferingEpoch>()];

    public static List<CardModel> Cards =>
        [ModelDb.Card<cards.NaiWaYongYuanDeNaiShu>(), ModelDb.Card<cards.NaiWaSiZhiTangXinJian>(), ModelDb.Card<cards.NaiWaDaiZhenZhuErHuanDeNaiWa>()];

    public override string UnlockText => CreateCardUnlockText(Cards);
    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueCardUnlock(Cards);
        EpochModel.QueueTimelineExpansion(GetTimelineExpansion());
    }
}
#endif
