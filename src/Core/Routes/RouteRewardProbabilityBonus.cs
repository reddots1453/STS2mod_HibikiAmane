namespace MaidenSuccubus.Core.Routes;

public readonly record struct RouteRewardProbabilityBonus(decimal Holy, decimal Corrupt)
{
    public static RouteRewardProbabilityBonus ForSoulCompass(int corruption) =>
        corruption is > -3 and < 3
            ? new(Holy: 0.20m, Corrupt: 0.20m)
            : default;
}
