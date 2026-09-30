namespace MaidenSuccubus.Core.Routes;

/// <summary>Reward weights plus a minimum of one card from each alignment in five colored shop slots.</summary>
public static class MerchantRoutePlan
{
    public static RouteCardKind[] Create(RouteRewardProbabilities probabilities, Func<float> roll, Func<int, int> index)
    {
        RouteCardKind[] routes = Enumerable.Range(0, 5).Select(_ => probabilities.RollRoute(roll())).ToArray();
        Ensure(RouteCardKind.Holy);
        Ensure(RouteCardKind.Corrupt);
        return routes;

        void Ensure(RouteCardKind required)
        {
            if (routes.Contains(required)) return;
            int[] donors = Enumerable.Range(0, routes.Length).Where(i => routes[i] == RouteCardKind.Neutral
                || routes.Count(route => route == routes[i]) > 1).ToArray();
            int[] neutral = donors.Where(i => routes[i] == RouteCardKind.Neutral).ToArray();
            if (neutral.Length > 0) donors = neutral;
            routes[donors[index(donors.Length)]] = required;
        }
    }
}
