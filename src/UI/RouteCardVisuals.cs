using Godot;
using MaidenSuccubus.Core.Routes;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.UI;

public static class RouteCardVisuals
{
    public static IEnumerable<CardOverlayContribution> GetOverlays(
        RouteCardKind routeKind) => routeKind switch
    {
        RouteCardKind.Holy => [CreateContribution(routeKind)],
        RouteCardKind.Corrupt => [CreateContribution(routeKind)],
        _ => [],
    };

    private static CardOverlayContribution CreateContribution(
        RouteCardKind routeKind) => CardOverlayContribution.FromFactory(
        "maiden_route_wing_v3",
        _ => CreateOverlay(routeKind),
        order: 0,
        fullRect: true);

    private static Control? CreateOverlay(RouteCardKind routeKind)
    {
        bool holy = routeKind == RouteCardKind.Holy;
        Texture2D? texture = RuntimeTextureAssets.Load(
            holy
                ? "ui/route_marks/route_holy_wing_v3.png"
                : "ui/route_marks/route_corrupt_wing_v3.png");
        if (texture == null)
        {
            return null;
        }

        Control root = new()
        {
            Name = holy
                ? "MaidenHolyRouteWingV3"
                : "MaidenCorruptRouteWingV3",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = false,
            ZIndex = 0,
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        TextureRect image = new()
        {
            Name = "RouteWing",
            Texture = texture,
            Position = holy ? new Vector2(-222f, -255f) : new Vector2(124f, -248f),
            Size = holy ? new Vector2(92f, 92f) : new Vector2(84f, 90f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.AddChild(image);
        return root;
    }
}
