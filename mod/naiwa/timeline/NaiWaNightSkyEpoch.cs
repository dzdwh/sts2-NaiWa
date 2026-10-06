#if !TOOLS
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace NaiWa.timeline;

public sealed class NaiWaNightSkyEpoch : EpochModel
{
    public const string EpochId = "NAI_WA7_EPOCH";
    public const string PortraitPath = "res://images/timeline/epoch_portraits/nai_wa7_epoch.png";

    public override string Id => EpochId;
    public override EpochEra Era => EpochEra.Invitation4;
    public override int EraPosition => 2;
    public override string StoryId => "NaiWa";
    public static List<CardModel> Cards => [ModelDb.Card<cards.NaiWaChuangZaoNaiWa>()];

    public override string UnlockText
    {
        get
        {
            var text = new LocString("epochs", Id + ".unlockText");
            text.Add("Card", ModelDb.Card<cards.NaiWaChuangZaoNaiWa>().TitleLocString.GetRawText());
            return text.GetFormattedText();
        }
    }

    public override EpochModel[] GetTimelineExpansion() => [EpochModel.Get<NaiWaLaughEpoch>()];

    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueCardUnlock(Cards);
        EpochModel.QueueTimelineExpansion(GetTimelineExpansion());
    }
}
#endif
