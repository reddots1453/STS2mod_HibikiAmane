using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Core.Seals;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.RestSite;

public sealed class FourthRouteSacrificeOption : ModRestSiteOptionTemplate
{
    public override string OptionId => "MAIDEN_SUCCUBUS_FOURTH_ROUTE_SACRIFICE";
    public override RestSiteOptionAssetProfile AssetProfile => new(
        "res://MaidenSuccubus/images/ui/remove_sealed.svg");
    public override LocString? CustomTitle => new("rest_site_ui", "OPTION_MAIDEN_SUCCUBUS_FOURTH_ROUTE_SACRIFICE.name");
    public override LocString Description => new("rest_site_ui", "OPTION_MAIDEN_SUCCUBUS_FOURTH_ROUTE_SACRIFICE.description");
    public FourthRouteSacrificeOption(Player owner) : base(owner) { }

    public override async Task<bool> OnSelect()
    {
        if (Owner.RunState is not MegaCrit.Sts2.Core.Runs.RunState runState
            || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)) return false;
        bool dark = FourthRouteProgressService.AlignmentOf(quest) == FourthRouteAlignment.Dark;
        var prefs = new CardSelectorPrefs(new LocString("card_selection", "MAIDEN_SUCCUBUS_FOURTH_ROUTE_SACRIFICE"), 1)
        { Cancelable = true, RequireManualConfirmation = true };
        CardModel? selected = (await CardSelectCmd.FromDeckGeneric(Owner, prefs,
            card => dark ? RouteCardQuery.IsHoly(card) : RouteCardQuery.IsCorrupt(card))).FirstOrDefault();
        if (selected == null) return false;
        await CardPileCmd.RemoveFromDeck(selected);
        await FourthRouteProgressService.AdvanceStage(Owner, 2);
        RestSiteActionPolicy.PreserveRemainingOptionsOnce(Owner);
        return true;
    }
}
