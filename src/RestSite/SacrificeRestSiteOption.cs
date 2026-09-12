using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Core.Seals;
using MaidenSuccubus.Data;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.RestSite;

public sealed class SacrificeRestSiteOption : ModRestSiteOptionTemplate
{
    public override string OptionId => "MAIDEN_SUCCUBUS_SACRIFICE";

    public override RestSiteOptionAssetProfile AssetProfile => new(
        "res://images/ui/rest_site/option_smith.png");

    public override LocString? CustomTitle => new(
        "rest_site_ui",
        "OPTION_MAIDEN_SUCCUBUS_SACRIFICE.name");

    public override LocString Description => new(
        "rest_site_ui",
        "OPTION_MAIDEN_SUCCUBUS_SACRIFICE.description");

    public SacrificeRestSiteOption(Player owner)
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
                "MAIDEN_SUCCUBUS_SACRIFICE"),
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

        bool completesFourthRouteSacrifice =
            CompletesFourthRouteSacrifice(confirmed);
        await CardPileCmd.RemoveFromDeck(confirmed);
        if (completesFourthRouteSacrifice)
        {
            await FourthRouteProgressService.AdvanceStage(Owner, 2);
        }

        RestSiteActionPolicy.PreserveRemainingOptionsOnce(Owner);
        return true;
    }

    private bool CompletesFourthRouteSacrifice(
        IReadOnlyCollection<CardModel> sacrificedCards)
    {
        if (Owner.RunState is not RunState runState
            || M5Progress.Handle.Get(runState).FourthRouteRelicStage != 2
            || !FourthRouteProgressService.TryGetQuest(
                runState,
                out FourthRouteQuest quest))
        {
            return false;
        }

        bool dark = FourthRouteProgressService.AlignmentOf(quest)
            == FourthRouteAlignment.Dark;
        return sacrificedCards.Any(card => dark
            ? RouteCardQuery.IsHoly(card)
            : RouteCardQuery.IsCorrupt(card));
    }
}
