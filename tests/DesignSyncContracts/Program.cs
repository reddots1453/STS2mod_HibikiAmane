using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Util;
using MaidenSuccubus.Core.Events;
using MaidenSuccubus.Acts;

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

int beforeFourthAct = checks;
// Independent literal boundary tables, indexed by corruption -5..5. Sin routes
// challenge Light; Virtue routes challenge Dark. Qualification never opens Act4.
bool[] sinEligible = [false, false, false, true, true, true, true, true, true, true, true];
bool[] virtueEligible = [true, true, true, true, true, true, true, true, false, false, false];
FourthRouteQuest[] sins = [FourthRouteQuest.Pride, FourthRouteQuest.Greed, FourthRouteQuest.Lust,
    FourthRouteQuest.Envy, FourthRouteQuest.Gluttony, FourthRouteQuest.Wrath, FourthRouteQuest.Sloth];
FourthRouteQuest[] virtues = [FourthRouteQuest.Humility, FourthRouteQuest.Generosity, FourthRouteQuest.Chastity,
    FourthRouteQuest.Benevolence, FourthRouteQuest.Temperance, FourthRouteQuest.Patience, FourthRouteQuest.Diligence];
Equal(14, sins.Concat(virtues).Distinct().Count(), "fourteen route oracle entries");
foreach (FourthRouteQuest quest in sins.Concat(virtues))
{
    var alignment = FourthActEntryRules.AlignmentOf(quest);
    bool isSin = sins.Contains(quest);
    Equal<FourthRouteAlignment?>(isSin ? FourthRouteAlignment.Dark : FourthRouteAlignment.Light,
        alignment, $"{quest} goddess mapping");
    for (int corruption = -5; corruption <= 5; corruption++)
    {
        bool eligible = (isSin ? sinEligible : virtueEligible)[corruption + 5];
        Equal(eligible, FourthActEntryRules.Qualifies(true, true, alignment, corruption), $"{quest}/{corruption}");
        Equal(false, FourthActEntryRules.Qualifies(true, false, alignment, corruption), "unawakened rejected");
        Equal(false, FourthActEntryRules.Qualifies(false, true, alignment, corruption), "foreign character rejected");
        Equal(false, FourthActEntryRules.NormalEntryEnabled, "even qualified route cannot open unfinished act");
    }
}
Equal<FourthRouteAlignment?>(null, FourthActEntryRules.AlignmentOf((FourthRouteQuest)999), "unknown quest not virtue");
Equal(false, FourthActEntryRules.Qualifies(true, true, null, 0), "unselected rejected");
Equal(false, FourthActEntryRules.Qualifies(true, true, (FourthRouteAlignment)999, 0), "unknown alignment rejected");
foreach (bool maiden in new[] { false, true })
foreach (bool checkedAlready in new[] { false, true })
foreach (int act in new[] { -1, 0, 1, 2, 3, 4 })
    Equal(maiden && !checkedAlready && act == 2,
        FourthActEntryRules.ShouldRecordEnding(maiden, act, checkedAlready), "checkpoint only once at third-act end");

// Actual production list transformation: same objects, order and current index.
var actOne = new object(); var actTwo = new object(); var actThree = new object();
var placeholder = new object(); var otherMod = new object();
IReadOnlyList<object> vanillaActs = new[] { actOne, actTwo, actThree };
IReadOnlyList<object> oldActs = new[] { actOne, actTwo, actThree, placeholder, otherMod, placeholder };
bool IsPlaceholder(object act) => ReferenceEquals(act, placeholder);
for (int act = 0; act < 3; act++)
{
    Equal(true, ReferenceEquals(vanillaActs,
        FourthActEntryRules.WithoutPendingPlaceholder(vanillaActs, act, IsPlaceholder)), "vanilla list untouched");
    var cleaned = FourthActEntryRules.WithoutPendingPlaceholder(oldActs, act, IsPlaceholder);
    Equal(4, cleaned.Count, "remove only future placeholders, including duplicates");
    Equal(true, cleaned.SequenceEqual(new[] { actOne, actTwo, actThree, otherMod }), "other mod/order/identity preserved");
    Equal(true, ReferenceEquals(oldActs[act], cleaned[act]), "current act identity/index preserved");
    Equal(true, ReferenceEquals(cleaned,
        FourthActEntryRules.WithoutPendingPlaceholder(cleaned, act, IsPlaceholder)), "migration idempotent");
    Equal(6, oldActs.Count, "source collection unmodified");
}
foreach (int current in new[] { 3, 4 })
{
    var cleaned = FourthActEntryRules.WithoutPendingPlaceholder(oldActs, current, IsPlaceholder);
    Equal(5, cleaned.Count, "entered old debug act preserved, later duplicate removed");
    Equal(true, ReferenceEquals(cleaned[current], oldActs[current]), "entered room never retargeted");
    Equal(true, ReferenceEquals(cleaned[3], placeholder), "past/current placeholder remains");
}
Equal(true, ReferenceEquals(oldActs,
    FourthActEntryRules.WithoutPendingPlaceholder(oldActs, 5, IsPlaceholder)), "no future entries unchanged");
foreach (int invalidIndex in new[] { -1, 6, int.MaxValue })
    Equal(true, ReferenceEquals(oldActs,
        FourthActEntryRules.WithoutPendingPlaceholder(oldActs, invalidIndex, IsPlaceholder)), "invalid current index safe no-op");
IReadOnlyList<object> emptyActs = Array.Empty<object>();
Equal(true, ReferenceEquals(emptyActs,
    FourthActEntryRules.WithoutPendingPlaceholder(emptyActs, 0, IsPlaceholder)), "empty list safe no-op");
Console.WriteLine($"PASS DS27 production fourth-act boundaries: {checks - beforeFourthAct} assertions; {checks} total.");
int trialChecks = FourthRouteTrialContracts.Run();
checks += trialChecks;
Console.WriteLine($"PASS DS27 production three-trial flow: {trialChecks} assertions; {checks} total.");

int beforeVirtues = checks;
int[] virtueStages = [0, 1, 2, 3, 4, -1, 5];
int[] benevolencePickups = [0, 2, 2, 0, 0, 0, 0];
DiligenceRewardRule[] diligenceRewards = [default, new(2, false, false), new(2, true, false),
    new(3, true, true), new(3, true, true), default, default];
for (int i = 0; i < virtueStages.Length; i++)
{
    int stage = virtueStages[i];
    Equal(benevolencePickups[i], VirtuePickupRules.BenevolencePickupCount(stage), "benevolence pickup upgrades");
    Equal(diligenceRewards[i], VirtuePickupRules.Diligence(stage), "diligence full reward spec");
    foreach (bool sameOwner in new[] { false, true })
    foreach (bool wasDeck in new[] { false, true })
    foreach (bool isDeck in new[] { false, true })
        Equal((stage == 3 || stage == 4) && sameOwner && !wasDeck && isDeck,
            VirtuePickupRules.BenevolenceOnAdded(stage, sameOwner, wasDeck, isDeck), "permanent-card addition filter");
}
(string Name, int Min, int Max)[] enchantmentRanges =
[
    ("Sharp", 1, 5), ("Nimble", 1, 5), ("Adroit", 2, 4), ("Momentum", 3, 8),
    ("Sown", 1, 2), ("Swift", 1, 2), ("Vigorous", 6, 12), ("Glam", 1, 1), ("Instinct", 1, 1)
];
foreach (var range in enchantmentRanges)
{
    Equal((range.Min, range.Max), VirtuePickupRules.EnchantmentRange(range.Name), "Chinese-name mapping " + range.Name);
    for (int value = range.Min; value <= range.Max; value++)
    {
        int rngCalls = 0;
        int Roll(int min, int exclusiveMax)
        {
            rngCalls++;
            Equal(range.Min, min, "inclusive lower RNG bound");
            Equal(range.Max + 1, exclusiveMax, "exclusive upper RNG bound includes design max");
            return value;
        }
        Equal(value, VirtuePickupRules.RollEnchantmentAmount(range.Name, Roll), "each attainable enchantment amount");
        Equal(range.Min == range.Max ? 0 : 1, rngCalls, "fixed amount does not consume RNG");
    }
}
Console.WriteLine($"PASS DS27 production virtue pickups: {checks - beforeVirtues} assertions; {checks} total.");

int beforeCombatVirtues = checks;
(int Stage, int Max, bool AtCombat, bool AtTurn, bool Discount)[] combatVirtues =
[
    (0, 0, false, false, false), (1, 2, true, false, false), (2, 3, true, false, true),
    (3, 3, false, true, true), (4, 3, false, true, true),
    (-1, 0, false, false, false), (5, 0, false, false, false)
];
foreach (var row in combatVirtues)
{
    Equal(row.Max, VirtueCombatRules.TemperanceMaximum(row.Stage), "temperance optional maximum");
    Equal(row.AtCombat, VirtueCombatRules.PatienceAtCombatStart(row.Stage), "patience combat timing");
    Equal(row.AtTurn, VirtueCombatRules.PatienceAtTurnStart(row.Stage, true), "patience own turn timing");
    Equal(false, VirtueCombatRules.PatienceAtTurnStart(row.Stage, false), "other player's turn ignored");
    Equal(row.Discount, VirtueCombatRules.PatienceDiscount(row.Stage), "patience discount stage");
    // First-turn flow calls both hooks. Awakened receives one card, not two.
    int generated = VirtueCombatRules.PatienceAtCombatStart(row.Stage) ? 1 : 0;
    generated += VirtueCombatRules.PatienceAtTurnStart(row.Stage, true) ? 1 : 0;
    Equal(row.Stage is >= 1 and <= 4 ? 1 : 0, generated, "first turn no double generation");
    for (int turn = 2; turn <= 4; turn++)
        generated += VirtueCombatRules.PatienceAtTurnStart(row.Stage, true) ? 1 : 0;
    Equal(row.AtTurn ? 4 : row.AtCombat ? 1 : 0, generated, "four-turn cumulative generation");
}
Console.WriteLine($"PASS DS27 production combat virtues: {checks - beforeCombatVirtues} assertions; {checks} total.");
