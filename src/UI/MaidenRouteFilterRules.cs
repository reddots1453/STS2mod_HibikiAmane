using MaidenSuccubus.Core.Routes;

namespace MaidenSuccubus.UI;

/// <summary>The native library uses OR within each filter group; no selection means all.</summary>
public static class MaidenRouteFilterRules
{
    public static bool Allows(RouteCardKind route, bool neutral, bool corrupt, bool holy)
    {
        if (!neutral && !corrupt && !holy) return true;
        return route switch
        {
            RouteCardKind.Neutral => neutral,
            RouteCardKind.Corrupt => corrupt,
            RouteCardKind.Holy => holy,
            _ => false,
        };
    }
}
