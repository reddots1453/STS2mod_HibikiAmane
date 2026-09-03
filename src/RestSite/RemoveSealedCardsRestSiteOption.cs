using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Core.Seals;

namespace MaidenSuccubus.RestSite;

public sealed class RemoveSealedCardsRestSiteOption
    : ModRestSiteOptionTemplate
{
    public override string OptionId => "MAIDEN_SUCCUBUS_REMOVE_SEALED";

    public override RestSiteOptionAssetProfile AssetProfile => new(
        "res://images/ui/rest_site/option_smith.png");

    public override LocString? CustomTitle => new(
        "rest_site_ui",
        "OPTION_MAIDEN_SUCCUBUS_REMOVE_SEALED.name");

    public override LocString Description => new(
        "rest_site_ui",
        "OPTION_MAIDEN_SUCCUBUS_REMOVE_SEALED.description");

    public RemoveSealedCardsRestSiteOption(Player owner)
        : base(owner)
    {
    }

    public override async Task<bool> OnSelect()
    {
        IReadOnlyList<CardModel> sealedCards =
            CombatSealQuery.GetSealedDeckCards(Owner);
        if (sealedCards.Count == 0)
        {
            return false;
        }

        var prefs = new CardSelectorPrefs(
            new LocString(
                "card_selection",
                "MAIDEN_SUCCUBUS_REMOVE_SEALED"),
            sealedCards.Count)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
        };

        List<CardModel> confirmed = (await CardSelectCmd.FromDeckGeneric(
                Owner,
                prefs,
                card => sealedCards.Contains(card)))
            .ToList();
        if (confirmed.Count != sealedCards.Count)
        {
            return false;
        }

        await CardPileCmd.RemoveFromDeck(confirmed);
        RestSiteActionPolicy.PreserveRemainingOptionsOnce(Owner);
        return true;
    }
}
