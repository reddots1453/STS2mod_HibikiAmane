using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Core.Relics;

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

int probabilityChecks = checks;
// Literal design oracle (corruption -5 through +5), not the production formula.
bool[] maxHp = [false, false, false, false, false, false, false, false, false, true, true];
bool[] multiPick = [true, true, false, false, false, false, false, false, false, false, false];
for (int corruption = -5; corruption <= 5; corruption++)
{
    Equal(maxHp[corruption + 5], OrbRules.GrantsMaxHp(corruption, false), $"orb hp {corruption}");
    Equal(true, OrbRules.GrantsMaxHp(corruption, true), $"sky hp {corruption}");
    foreach (int offered in new[] { 0, 1, 2, 3, 5 })
    {
        bool hasAnother = offered is 2 or 3 or 5;
        Equal(hasAnother && multiPick[corruption + 5], OrbRules.AllowsMoreCards(corruption, false, offered),
            $"orb continue {corruption}/{offered}");
        Equal(hasAnother, OrbRules.AllowsMoreCards(corruption, true, offered), $"sky continue {corruption}/{offered}");
    }
}
Console.WriteLine($"PASS DS27 production orb contracts: {checks - probabilityChecks} assertions; {checks} total.");

int beforeRetention = checks;
RetentionOrbRule[] retention =
[
    new(true, false, false), new(true, false, false),
    new(false, false, false), new(false, false, false), new(false, false, false),
    new(false, false, false), new(false, false, false), new(false, false, false), new(false, false, false),
    new(false, true, false), new(false, true, false),
];
for (int corruption = -5; corruption <= 5; corruption++)
{
    RetentionOrbRule actual = RetentionOrbRule.At(corruption, false);
    Equal(retention[corruption + 5].PermanentRetain, actual.PermanentRetain, "hero retain keyword band");
    Equal(retention[corruption + 5].Upgrade, actual.Upgrade, "hero upgrade band");
    Equal(false, actual.Enchant, "hero never enchants");
    var eternal = RetentionOrbRule.At(corruption, true);
    Equal(true, eternal.PermanentRetain, "eternal keyword in every band");
    Equal(true, eternal.Upgrade, "eternal upgrade in every band");
    Equal(true, eternal.Enchant, "eternal enchant in every band");
}
Console.WriteLine($"PASS DS27 production retention contracts: {checks - beforeRetention} assertions; {checks} total.");
