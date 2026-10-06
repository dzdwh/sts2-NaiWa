#if !TOOLS
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace NaiWa.timeline;

public sealed class NaiWaLaughterEpoch : EpochModel
{
    public const string EpochId = "NAI_WA6_EPOCH";
    public const string PortraitPath = "res://images/timeline/epoch_portraits/nai_wa6_epoch.png";

    public override string Id => EpochId;
    public override EpochEra Era => EpochEra.Invitation2;
    public override int EraPosition => 3;
    public override string StoryId => "NaiWa";
    public static List<CardModel> Cards => [ModelDb.Card<cards.NaiWaZiYouYinDaoNaiWa>()];

    public override string UnlockText
    {
        get
        {
            var text = new LocString("epochs", Id + ".unlockText");
            text.Add("Card", ModelDb.Card<cards.NaiWaZiYouYinDaoNaiWa>().TitleLocString.GetRawText());
            return text.GetFormattedText();
        }
    }

    public override EpochModel[] GetTimelineExpansion() => [EpochModel.Get<NaiWaNightSkyEpoch>()];

    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueCardUnlock(Cards);
        EpochModel.QueueTimelineExpansion(GetTimelineExpansion());
    }
}
#endif
