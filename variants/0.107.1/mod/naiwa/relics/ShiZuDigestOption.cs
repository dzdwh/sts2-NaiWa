using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.relics;

public class ShiZuDigestOption : RestSiteOption
{
    public override string OptionId => "SHI_ZU_DIGEST";

    public override bool IsEnabled => PileType.Deck.GetPile(base.Owner).Cards.Any(c => c.IsRemovable);

    public override LocString Description
    {
        get
        {
            var desc = new LocString("rest_site_ui", IsEnabled
                ? "OPTION_SHI_ZU_DIGEST.description"
                : "OPTION_SHI_ZU_DIGEST.descriptionDisabled");
            desc.Add("RelicName", ModelDb.Relic<ShiZuWeiDai>().Title);
            return desc;
        }
    }

    public ShiZuDigestOption(Player owner) : base(owner) { }

    public override async Task<bool> OnSelect()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        var cards = await CardSelectCmd.FromDeckForRemoval(base.Owner, prefs);
        if (!cards.Any()) return false;

        foreach (var c in cards)
            await CardPileCmd.RemoveFromDeck(c);

        // Activate countdown
        base.Owner.GetRelic<ShiZuWeiDai>()?.Activate();
        return true;
    }

    public override Task DoLocalPostSelectVfx(CancellationToken ct = default) => Task.CompletedTask;
    public override Task DoRemotePostSelectVfx() => Task.CompletedTask;
}
