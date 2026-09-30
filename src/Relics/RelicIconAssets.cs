using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Acts;
using MaidenSuccubus.UI;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Relics;

/// <summary>Prepared loose PNG resources for mod relics, including native outline and large views.</summary>
internal static class RelicIconAssets
{
    private const string FallbackIcon = "res://images/atlases/relic_atlas.sprites/circlet.tres";
    private const string FallbackOutline = "res://images/atlases/relic_outline_atlas.sprites/circlet.tres";
    private static readonly Dictionary<string, RelicAssetProfile> Profiles =
        new(StringComparer.Ordinal);

    internal static RelicAssetProfile For(string assetName)
    {
        if (Profiles.TryGetValue(assetName, out RelicAssetProfile? profile)) return profile;
        string basePath = $"relics/icons/{assetName}";
        string icon = RuntimeTextureAssets.PrepareResource(
            basePath + "_small.png", $"user://maiden_relic_{assetName}_small.tres", FallbackIcon);
        string outline = RuntimeTextureAssets.PrepareResource(
            basePath + "_outline.png", $"user://maiden_relic_{assetName}_outline.tres", FallbackOutline);
        string big = RuntimeTextureAssets.PrepareResource(
            basePath + "_big.png", $"user://maiden_relic_{assetName}_big.tres", FallbackIcon);
        profile = new RelicAssetProfile(IconPath: icon, IconOutlinePath: outline, BigIconPath: big);
        Profiles[assetName] = profile;
        return profile;
    }

    internal static RelicAssetProfile ForRoute(FourthRouteQuest quest) =>
        For("route_relic_" + quest);

    internal static RelicAssetProfile ForFragment(string routeTitleKey)
    {
        foreach (FourthRouteQuest quest in Enum.GetValues<FourthRouteQuest>())
        {
            if (routeTitleKey.Contains("_" + quest.ToString().ToUpperInvariant() + "_",
                    StringComparison.Ordinal))
                return For("route_fragment_" + quest);
        }
        // The model prototype has no selected route. A generic fragment is used
        // until the shop assigns RouteTitleKey to its mutable copy.
        return For("legacy_fourth_route_fragment_relic_v1");
    }
}
