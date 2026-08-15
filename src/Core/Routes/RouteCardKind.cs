namespace MaidenSuccubus.Core.Routes;

public enum RouteCardKind
{
    Neutral,
    Corrupt,
    Holy,
}

public interface IMaidenSuccubusRouteCard
{
    RouteCardKind RouteKind { get; }
}
