namespace MaidenSuccubus.Core.Routes;

public readonly record struct RouteRewardProbabilities(
    decimal Holy,
    decimal Corrupt,
    decimal Neutral)
{
    private static readonly decimal[] FavoredByMagnitude =
        [0.10m, 0.15m, 0.20m, 0.30m, 0.45m, 0.65m];

    private static readonly decimal[] OpposedByMagnitude =
        [0.10m, 0.10m, 0.08m, 0.05m, 0.00m, 0.00m];

    public decimal Total => Holy + Corrupt + Neutral;

    public static RouteRewardProbabilities Calculate(
        int corruption,
        decimal holyBonus = 0m,
        decimal corruptBonus = 0m)
    {
        int clamped = Math.Clamp(corruption, -5, 5);
        int magnitude = Math.Abs(clamped);

        decimal holy = clamped < 0
            ? FavoredByMagnitude[magnitude]
            : clamped > 0
                ? OpposedByMagnitude[magnitude]
                : FavoredByMagnitude[0];

        decimal corrupt = clamped > 0
            ? FavoredByMagnitude[magnitude]
            : clamped < 0
                ? OpposedByMagnitude[magnitude]
                : FavoredByMagnitude[0];

        holy = Math.Clamp(holy + holyBonus, 0m, 1m);
        corrupt = Math.Clamp(corrupt + corruptBonus, 0m, 1m);

        decimal routeTotal = holy + corrupt;
        if (routeTotal > 1m)
        {
            holy /= routeTotal;
            corrupt /= routeTotal;
            routeTotal = 1m;
        }

        return new RouteRewardProbabilities(
            Holy: holy,
            Corrupt: corrupt,
            Neutral: 1m - routeTotal);
    }
}
