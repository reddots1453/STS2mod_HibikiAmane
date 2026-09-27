using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Util;
using MaidenSuccubus.Core.Events;

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

int beforeScopes = checks;
var scopes = new WeakInstanceScope<Tuple<int>>();
var a = Tuple.Create(1);
var b = Tuple.Create(1); // Same value, distinct instance must remain unaffected.
Equal(true, a.Equals(b), "fixture is value-equal");
Equal(false, scopes.Contains(a), "initially inactive");
var outer = scopes.Enter(a);
Equal(true, scopes.Contains(a), "outer enters");
Equal(false, scopes.Contains(b), "scope uses identity not Equals");
var inner = scopes.Enter(a);
inner.Dispose();
inner.Dispose();
Equal(true, scopes.Contains(a), "double dispose does not remove outer");
outer.Dispose();
Equal(false, scopes.Contains(a), "outer exits");
outer.Dispose();
using (scopes.Enter(a))
{
    Equal(true, scopes.Contains(a), "reentry after repeated dispose");
    using (scopes.Enter(b))
    {
        await Task.Yield();
        Equal(true, scopes.Contains(a), "await retains original instance state");
        Equal(true, scopes.Contains(b), "independent scope across await");
    }
    Equal(false, scopes.Contains(b), "other scope exits separately");
    Equal(true, scopes.Contains(a), "original remains active");
}
Equal(false, scopes.Contains(a), "awaited scope cleaned up");
try
{
    using var failure = scopes.Enter(a);
    Equal(true, scopes.Contains(a), "failure path entered");
    throw new InvalidOperationException("scope cleanup fixture");
}
catch (InvalidOperationException ex) when (ex.Message == "scope cleanup fixture") { }
Equal(false, scopes.Contains(a), "exception releases scope");
var independentRegistry = new WeakInstanceScope<Tuple<int>>();
using (scopes.Enter(a))
    Equal(false, independentRegistry.Contains(a), "independent registries isolated");
Console.WriteLine($"PASS DS27 production instance scopes: {checks - beforeScopes} assertions; {checks} total.");

int beforeSnapshots = checks;
var snapshots = new WeakInstanceValueScope<Tuple<int>, int>();
Equal(false, snapshots.TryGet(a, out _), "no snapshot before entry");
var outerSnapshot = snapshots.Enter(a, 7);
Equal(true, snapshots.TryGet(a, out int snapshot), "snapshot entered");
Equal(7, snapshot, "outer value");
Equal(false, snapshots.TryGet(b, out _), "equal-value keys remain isolated");
using (snapshots.Enter(a, 14))
{
    await Task.Yield();
    Equal(true, snapshots.TryGet(a, out snapshot), "nested survives await");
    Equal(14, snapshot, "nested value");
}
Equal(true, snapshots.TryGet(a, out snapshot), "outer restored");
Equal(7, snapshot, "outer value restored");
var lastSnapshot = snapshots.Enter(a, 20);
outerSnapshot.Dispose(); // Even non-LIFO cleanup cannot remove the active event.
outerSnapshot.Dispose();
Equal(true, snapshots.TryGet(a, out snapshot), "active child survives early parent cleanup");
Equal(20, snapshot, "active child still correct");
lastSnapshot.Dispose();
lastSnapshot.Dispose();
Equal(false, snapshots.TryGet(a, out _), "idempotent cleanup empties stack");
try
{
    using var failedSnapshot = snapshots.Enter(a, 9);
    await Task.Yield();
    throw new InvalidOperationException("snapshot failure");
}
catch (InvalidOperationException ex) when (ex.Message == "snapshot failure") { }
Equal(false, snapshots.TryGet(a, out _), "exception cleans value scope");
using (snapshots.Enter(a, 2))
using (snapshots.Enter(b, 3))
{
    snapshots.TryGet(a, out int first);
    snapshots.TryGet(b, out int second);
    Equal(2, first, "separate event first value");
    Equal(3, second, "separate event second value");
}
Equal(false, snapshots.TryGet(b, out _), "second event cleaned");
Console.WriteLine($"PASS DS27 production snapshot scopes: {checks - beforeSnapshots} assertions; {checks} total.");

int beforeEvents = checks;
// Exhaust the boolean completion table using a fresh event for each row.
var ledger = new EventCompletionLedger<Tuple<int>>();
foreach (bool applicable in new[] { false, true })
foreach (bool wasFinished in new[] { false, true })
foreach (bool isFinished in new[] { false, true })
foreach (bool pageChanged in new[] { false, true })
{
    var instance = Tuple.Create(1);
    bool expectedCommit = applicable && !wasFinished && (isFinished || pageChanged);
    Equal(expectedCommit, ledger.TryCommit(instance, "choice", applicable, wasFinished, isFinished, pageChanged),
        $"event completion {applicable}/{wasFinished}/{isFinished}/{pageChanged}");
}
var firstEvent = Tuple.Create(1);
var nextEvent = Tuple.Create(1);
Equal(false, ledger.TryCommit(firstEvent, "choice", true, false, false, false), "no-op does not consume receipt");
await Task.Yield();
Equal(true, ledger.TryCommit(firstEvent, "choice", true, false, true, false), "later success can commit");
Equal(false, ledger.TryCommit(firstEvent, "choice", true, false, true, true), "rebuilt same-key option cannot duplicate");
Equal(true, ledger.TryCommit(nextEvent, "choice", true, false, true, false), "equal-valued independent event can commit");
Equal(true, ledger.TryCommit(firstEvent, "other choice", true, false, false, true), "different option key isolated");
Equal(false, ledger.TryCommit(firstEvent, "other choice", true, false, false, true), "page advance also exactly once");
Equal(false, ledger.TryCommit(firstEvent, "foreign", false, false, true, true), "foreign owner rejected");
Equal(true, ledger.TryCommit(firstEvent, "foreign", true, false, true, false), "rejection does not consume receipt");
Console.WriteLine($"PASS DS27 production event completion: {checks - beforeEvents} assertions; {checks} total.");
