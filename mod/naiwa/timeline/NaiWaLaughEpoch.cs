#if !TOOLS
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace NaiWa.timeline;

public sealed class NaiWaLaughEpoch : EpochModel
{
    public const string EpochId = "NAI_WA8_EPOCH";
    public const string PortraitPath = "res://images/timeline/epoch_portraits/nai_wa8_epoch.png";

    public override string Id => EpochId;
    public override EpochEra Era => EpochEra.Invitation7;
    public override int EraPosition => 2;
    public override string StoryId => "NaiWa";
    public static List<CardModel> Cards => [ModelDb.Card<cards.NaiWaNaiWaDeYongHeng>()];

    public override string UnlockText
    {
        get
        {
            var text = new LocString("epochs", Id + ".unlockText");
            text.Add("Card", ModelDb.Card<cards.NaiWaNaiWaDeYongHeng>().TitleLocString.GetRawText());
            return text.GetFormattedText();
        }
    }

    public override void QueueUnlocks() => NTimelineScreen.Instance.QueueCardUnlock(Cards);
}
#endif
