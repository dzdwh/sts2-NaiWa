#if !TOOLS
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;

namespace NaiWa.timeline;

public sealed class NaiWaBirthEpoch : EpochModel
{
    public const string EpochId = "NAI_WA1_EPOCH";
    public const string PortraitPath = "res://images/timeline/epoch_portraits/nai_wa1_epoch.png";

    public override string Id => EpochId;
    public override EpochEra Era => EpochEra.Prehistoria0;
    // Use a free slot in the vanilla era column; keep original epochs unchanged.
    public override int EraPosition => 1;
    public override string StoryId => "NaiWa";

    public override EpochModel[] GetTimelineExpansion() => [EpochModel.Get<NaiWaSearchEpoch>()];

    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueCharacterUnlock<Characters.NaiWa>(this);
        SaveManager.Instance.Progress.PendingCharacterUnlock = ModelDb.Character<Characters.NaiWa>().Id;
        EpochModel.QueueTimelineExpansion(GetTimelineExpansion());
    }
}

public sealed class NaiWaStory : StoryModel
{
    protected override string Id => "NAI_WA";
    public override EpochModel[] Epochs => [EpochModel.Get<NaiWaBirthEpoch>(), EpochModel.Get<NaiWaSearchEpoch>(), EpochModel.Get<NaiWaCluesEpoch>(), EpochModel.Get<NaiWaImprisonmentEpoch>(), EpochModel.Get<NaiWaSufferingEpoch>(), EpochModel.Get<NaiWaLaughterEpoch>(), EpochModel.Get<NaiWaNightSkyEpoch>(), EpochModel.Get<NaiWaLaughEpoch>()];
}
#endif
