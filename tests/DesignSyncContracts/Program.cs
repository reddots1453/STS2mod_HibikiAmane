using MaidenSuccubus.Core.Routes;

// Compiles and executes production pure rules, not copied implementations.
// Independent literal expectations: SYS-COR-003 and RELIC-EVENT-005, 2026-09-27.
(decimal holy, decimal corrupt, decimal neutral)[] expected =
[
    (.65m, .00m, .35m), (.45m, .00m, .55m), (.30m, .05m, .65m),
    (.40m, .28m, .32m), (.35m, .30m, .35m), (.30m, .30m, .40m),
    (.30m, .35m, .35m), (.28m, .40m, .32m), (.05m, .30m, .65m),
    (.00m, .45m, .55m), (.00m, .65m, .35m),
];
int checks = 0;
void Equal<T>(T want, T got, string name)
{
    if (!EqualityComparer<T>.Default.Equals(want, got))
        throw new InvalidOperationException($"{name}: expected={want}, actual={got}");
    checks++;
}
for (int corruption = -5; corruption <= 5; corruption++)
{
    RouteRewardProbabilityBonus bonus = RouteRewardProbabilityBonus.ForSoulCompass(corruption);
    RouteRewardProbabilities actual = RouteRewardProbabilities.Calculate(corruption, bonus.Holy, bonus.Corrupt);
    var want = expected[corruption + 5];
    Equal(want.holy, actual.Holy, $"{corruption} holy");
    Equal(want.corrupt, actual.Corrupt, $"{corruption} corrupt");
    Equal(want.neutral, actual.Neutral, $"{corruption} neutral");
    Equal(1m, actual.Total, $"{corruption} total");
    var ordinary = RouteRewardProbabilities.Calculate(corruption);
    decimal added = Math.Abs(corruption) < 3 ? .20m : 0m;
    Equal(added, actual.Holy - ordinary.Holy, $"{corruption} additive not multiplicative holy");
    Equal(added, actual.Corrupt - ordinary.Corrupt, $"{corruption} additive not multiplicative corrupt");
    Equal(-2 * added, actual.Neutral - ordinary.Neutral, $"{corruption} neutral pays both bonuses");
    Equal(true, actual.Holy >= 0 && actual.Corrupt >= 0 && actual.Neutral >= 0, $"{corruption} nonnegative");
}
foreach (int outside in new[] { int.MinValue, -6, 6, int.MaxValue })
    Equal(default(RouteRewardProbabilityBonus), RouteRewardProbabilityBonus.ForSoulCompass(outside), "outside band inactive");
Console.WriteLine($"PASS DS27 production probability contracts: {checks} assertions.");
