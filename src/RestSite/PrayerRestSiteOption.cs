using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.RestSite;

public sealed class PrayerRestSiteOption : ModRestSiteOptionTemplate
{
    public override string OptionId => "MAIDEN_SUCCUBUS_PRAYER";

    public override RestSiteOptionAssetProfile AssetProfile => new(
        "res://images/ui/rest_site/option_heal.png");

    public override LocString? CustomTitle => new(
        "rest_site_ui", "OPTION_MAIDEN_SUCCUBUS_PRAYER.name");

    public override LocString Description => new(
        "rest_site_ui", "OPTION_MAIDEN_SUCCUBUS_PRAYER.description");

    public PrayerRestSiteOption(Player owner) : base(owner) { }

    public override Task<bool> OnSelect() =>
        MasturbateRestSiteOption.PerformActionAsync(Owner, prayer: true);
}
