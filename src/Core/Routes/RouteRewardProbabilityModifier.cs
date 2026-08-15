using MegaCrit.Sts2.Core.Entities.Players;

namespace MaidenSuccubus.Core.Routes;

public readonly record struct RouteRewardProbabilityBonus(
    decimal Holy,
    decimal Corrupt);

public interface IRouteRewardProbabilityModifier
{
    RouteRewardProbabilityBonus GetRouteRewardProbabilityBonus(Player player);
}

public static class RouteRewardProbabilityModifiers
{
    public static RouteRewardProbabilityBonus GetTotal(Player player)
    {
        decimal holy = 0m;
        decimal corrupt = 0m;
        foreach (var modifier in player.Relics
                     .OfType<IRouteRewardProbabilityModifier>())
        {
            var bonus = modifier.GetRouteRewardProbabilityBonus(player);
            holy += bonus.Holy;
            corrupt += bonus.Corrupt;
        }

        return new RouteRewardProbabilityBonus(holy, corrupt);
    }
}
