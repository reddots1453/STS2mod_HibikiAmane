using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Util;
using MaidenSuccubus.Core.Events;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Core.Seals;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Powers;

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

int beforeGluttony = checks;
(int Stage, int Hp, int Slots, bool Fill, int OnUse)[] gluttonyStages =
[
    (0, 0, 0, false, 0), (1, 4, 1, false, 0), (2, 4, 1, false, 0),
    (3, 0, 0, true, 4), (4, 0, 0, true, 4), (-1, 0, 0, false, 0), (5, 0, 0, false, 0)
];
foreach (var row in gluttonyStages)
{
    Equal(row.Hp, GluttonyRules.PickupMaxHp(row.Stage), "gluttony pickup hp");
    Equal(row.Slots, GluttonyRules.PickupSlots(row.Stage), "gluttony pickup slots");
    Equal(row.Fill, GluttonyRules.FillOnPickup(row.Stage), "gluttony fill only awakened");
    Equal(row.OnUse, GluttonyRules.MaxHpOnPotionUsed(row.Stage, true), "own potion use");
    Equal(0, GluttonyRules.MaxHpOnPotionUsed(row.Stage, false), "other potion owner ignored");
}
for (int capacity = 0; capacity <= 8; capacity++)
for (int occupied = 0; occupied <= 10; occupied++)
    Equal(occupied >= capacity ? 0 : capacity - occupied, GluttonyRules.EmptySlots(capacity, occupied), "only empty slots filled");
Equal(0, GluttonyRules.EmptySlots(int.MinValue, int.MaxValue), "invalid negative capacity safe");
Equal(3, GluttonyRules.EmptySlots(3, -1), "invalid occupancy bounded at zero");
Equal(int.MaxValue, GluttonyRules.EmptySlots(int.MaxValue, int.MinValue), "no subtraction overflow");
Console.WriteLine($"PASS DS27 production gluttony: {checks - beforeGluttony} assertions; {checks} total.");

int beforeWrath = checks;
foreach (bool used in new[] { false, true })
foreach (bool powered in new[] { false, true })
    Equal(!used && powered ? 6 : 0, WrathRules.FirstPlayBonus(used, powered), "wrath initial bonus only");
foreach (int stage in new[] { -1, 0, 1, 2, 3, 4, 5 })
{
    Equal(stage is 1 or 2, WrathRules.EnchantOnPickup(stage), "wrath two independent pickup stages");
    foreach (bool powered in new[] { false, true })
    foreach (bool own in new[] { false, true })
    foreach (bool attack in new[] { false, true })
    foreach (bool wrath in new[] { false, true })
        Equal((stage is 3 or 4) && powered && own && attack && wrath ? 6 : 0,
            WrathRules.AwakenedBonus(stage, powered, own, attack, wrath), "wrath awakened source filters");
}
for (int layers = 1; layers <= 3; layers++)
{
    Equal(layers * 6 + 6, layers * WrathRules.FirstPlayBonus(false, true)
        + WrathRules.AwakenedBonus(4, true, true, true, true), "awakened flat bonus not per enchantment layer");
    Equal(6, layers * WrathRules.FirstPlayBonus(true, true)
        + WrathRules.AwakenedBonus(4, true, true, true, true), "later play retains only awakened bonus");
}
Console.WriteLine($"PASS DS27 production wrath: {checks - beforeWrath} assertions; {checks} total.");

int beforeSloth = checks;
foreach (int stage in new[] { 0, 1, 2, 3, 4 })
foreach (int spent in new[] { 0, 1, 2, 3, 20 })
{
    SlothTurnState turn = new();
    Equal(0, turn.TakeEnergy(stage, true), "sloth no free first-turn energy");
    turn.StartTurn(true);
    turn.RecordPayment(spent, true);
    turn.RecordPayment(99, false);
    turn.RecordPayment(0, true);
    turn.RecordPayment(-99, true);
    Equal(spent, turn.EnergySpent, "actual own positive payment only");
    turn.StartTurn(false);
    Equal(spent, turn.EnergySpent, "other player cannot reset spending");
    Equal(0, turn.ResolveEnd(stage, false), "other turn gives no block");
    Equal(false, turn.PendingEnergy, "other turn creates no pending reward");
    bool eligible = stage > 0 && spent <= 2;
    int block = eligible && stage >= 3 ? 12 : 0;
    Equal(block, turn.ResolveEnd(stage, true), "sloth end block");
    Equal(eligible, turn.PendingEnergy, "sloth threshold inclusive two");
    Equal(0, turn.ResolveEnd(stage, true), "duplicate end never gives block twice");
    Equal(0, turn.TakeEnergy(stage, false), "foreign reset cannot steal reward");
    Equal(eligible, turn.PendingEnergy, "foreign reset preserves reward");
    turn.StartTurn(true); // Includes an extra turn in the same global round.
    Equal(0, turn.EnergySpent, "new own turn clears spending");
    Equal(false, turn.EndResolved, "new own turn reopens end settlement");
    Equal(eligible, turn.PendingEnergy, "new own turn preserves previous reward until energy reset");
    int reward = eligible ? Math.Min(stage, 3) : 0;
    Equal(reward, turn.TakeEnergy(stage, true), "sloth next own turn energy");
    Equal(0, turn.TakeEnergy(stage, true), "duplicate energy reset cannot pay twice");
    turn.RecordPayment(1, true);
    turn.RecordPayment(1, true);
    turn.RecordPayment(1, true);
    Equal(0, turn.ResolveEnd(stage, true), "separate payments accumulate above threshold");
    Equal(0, turn.TakeEnergy(stage, true), "over threshold no next reward");
}
var pendingSloth = new SlothTurnState { EnergySpent = 2 };
Equal(12, pendingSloth.ResolveEnd(4, true), "awakened block at boundary");
pendingSloth.RecordPayment(9, true);
Equal(2, pendingSloth.EnergySpent, "closed turn cannot accrue later side's payments");
var clonedSloth = pendingSloth;
Equal(3, clonedSloth.TakeEnergy(4, true), "clone can consume its own reward");
Equal(true, pendingSloth.PendingEnergy, "value clone leaves original pending");
pendingSloth = default;
Equal(0, pendingSloth.EnergySpent, "battle reset clears spending");
Equal(false, pendingSloth.EndResolved, "battle reset clears settled flag");
Equal(0, pendingSloth.TakeEnergy(4, true), "battle reset clears pending reward");
pendingSloth.RecordPayment(int.MaxValue, true);
pendingSloth.RecordPayment(int.MaxValue, true);
Equal(int.MaxValue, pendingSloth.EnergySpent, "large spending cannot wrap into eligibility");
Equal(0, pendingSloth.ResolveEnd(4, true), "overflow remains ineligible");
foreach (int stage in new[] { -1, 5, int.MaxValue })
{
    var invalid = new SlothTurnState();
    Equal(0, invalid.ResolveEnd(stage, true), "unknown stage no block");
    Equal(false, invalid.PendingEnergy, "unknown stage no reward");
}
Console.WriteLine($"PASS DS27 production sloth: {checks - beforeSloth} assertions; {checks} total.");

int beforeEnvy = checks;
foreach (int stage in new[] { -1, 0, 1, 2, 3, 4, 5 })
{
    bool active = stage is >= 1 and <= 4;
    bool perTurn = stage is 3 or 4;
    Equal(stage is >= 2 and <= 4 ? 1 : 0, EnvyTriggerState.CardsToDraw(stage), "envy stage draw");
    foreach (bool own in new[] { false, true })
    foreach (bool harmful in new[] { false, true })
    foreach (bool used in new[] { false, true })
    {
        var state = new EnvyTriggerState { Used = used };
        bool fires = active && own && harmful && !used;
        Equal(fires, state.TryUse(stage, own, harmful), "envy stage/owner/type/receipt predicate");
        Equal(used || fires, state.Used, "nonqualifying change cannot consume first use");
    }
    var sequence = new EnvyTriggerState();
    Equal(false, sequence.TryUse(stage, true, false), "purification or buff does not spend trigger");
    Equal(false, sequence.TryUse(stage, false, true), "foreign caster does not spend trigger");
    Equal(active, sequence.TryUse(stage, true, true), "first actual harmful change");
    Equal(false, sequence.TryUse(stage, true, true), "multiple targets or layers trigger once");
    sequence.StartTurn(stage, false);
    Equal(false, sequence.TryUse(stage, true, true), "enemy/foreign turn cannot refresh window");
    sequence.StartTurn(stage, true);
    Equal(perTurn, sequence.TryUse(stage, true, true), "only awakened refreshes on own next turn");
    sequence = default;
    Equal(active, sequence.TryUse(stage, true, true), "new battle resets per-combat trigger");
}
var envyOriginal = new EnvyTriggerState { Used = true };
var envyClone = envyOriginal;
envyClone.StartTurn(4, true);
Equal(true, envyOriginal.Used, "clone reset cannot change original receipt");
Equal(false, envyClone.Used, "clone owns independent receipt");
Console.WriteLine($"PASS DS27 production envy: {checks - beforeEnvy} assertions; {checks} total.");

int beforeGreed = checks;
foreach (int stage in new[] { 0, 1, 2, 3, 4, 5 })
foreach (bool pending in new[] { false, true })
foreach (bool activeShop in new[] { false, true })
foreach (bool unknown in new[] { false, true })
foreach (bool reserved in new[] { false, true })
{
    var state = new GreedShopState { Pending = pending, Active = activeShop, Location = "1:7" };
    bool eligible = stage is 3 or 4 && unknown && !reserved;
    Equal(eligible && (pending || activeShop), state.CanSelect(stage, "1:7", unknown, reserved), "greed select same location");
    Equal(eligible && pending, state.CanSelect(stage, "1:8", unknown, reserved), "greed later location needs unspent entitlement");
    Equal(pending, state.Pending, "query/reservation never consumes pending");
    Equal(activeShop, state.Active, "query does not expire current room");
}
foreach (int stage in new[] { 0, 1, 2, 3, 4, 5 })
{
    var state = new GreedShopState { Pending = true };
    bool awake = stage is 3 or 4;
    Equal(false, state.CanSelect(stage, "", true, false), "empty location cannot reserve");
    Equal(false, state.Commit(stage, "0:4", false), "failed or nonmerchant creation preserves entitlement");
    Equal(true, state.Pending, "failed creation keeps pending");
    Equal(awake, state.Commit(stage, "0:4", true), "successful merchant creation consumes only awakened");
    Equal(!awake, state.Pending, "consume at creation not selection");
    Equal(awake, state.IsFree(stage, "0:4", true, true), "exact room and owner free");
    Equal(false, state.IsFree(stage, "0:4", false, true), "other shop at same floor not free");
    Equal(false, state.IsFree(stage, "0:4", true, false), "other player not free");
    Equal(false, state.IsFree(stage, "1:4", true, true), "same floor other act not free");
    Equal(false, state.IsFree(stage, "0:5", true, true), "next floor not free");
    var restored = state;
    Equal(awake, restored.CanSelect(stage, "0:4", true, false), "saved active can rebuild same appointed room");
    Equal(awake, restored.Commit(stage, "0:4", true), "rebuild active location idempotent");
    state.Leave();
    Equal(false, state.Active, "leaving clears active");
    Equal(!awake, state.Pending, "unconsumed entitlement survives other rooms");
    Equal(awake, restored.Active, "value copies do not share mutable room state");
}
var legacyGreed = new GreedShopState { Active = true };
Equal(false, legacyGreed.CanSelect(4, "1:2", true, false), "legacy active without location cannot grant arbitrary shop");
Equal(false, legacyGreed.IsFree(4, "1:2", true, true), "legacy unbound active cannot change prices");
legacyGreed.Pending = true;
Equal(true, legacyGreed.CanSelect(4, "1:2", true, false), "legacy pending still grants next unknown");
var orderedGreed = new GreedShopState { Pending = true };
Equal(false, orderedGreed.CanSelect(4, "0:5", false, false), "ordinary shop does not spend queued unknown");
orderedGreed.Leave();
Equal(false, orderedGreed.CanSelect(4, "0:6", true, true), "reserved event wins");
orderedGreed.Leave();
Equal(true, orderedGreed.CanSelect(4, "0:7", true, false), "deferred next unknown still eligible");
Equal(true, orderedGreed.Commit(4, "0:7", true), "deferred next unknown commits");
orderedGreed.Leave();
Equal(false, orderedGreed.CanSelect(4, "0:8", true, false), "one entitlement never creates two shops");
Console.WriteLine($"PASS DS27 production greed shop: {checks - beforeGreed} assertions; {checks} total.");

int beforeOpening = checks;
Equal(true, FourthRouteOpeningState.NeedsOpening(false, null), "fresh run needs opening");
Equal(false, FourthRouteOpeningState.NeedsOpening(true, null), "legacy selected route not replayed");
foreach (var dark in new[] { FourthRouteQuest.Pride, FourthRouteQuest.Greed, FourthRouteQuest.Lust, FourthRouteQuest.Envy,
    FourthRouteQuest.Gluttony, FourthRouteQuest.Wrath, FourthRouteQuest.Sloth })
foreach (var light in new[] { FourthRouteQuest.Humility, FourthRouteQuest.Generosity, FourthRouteQuest.Chastity,
    FourthRouteQuest.Benevolence, FourthRouteQuest.Temperance, FourthRouteQuest.Patience, FourthRouteQuest.Diligence })
foreach (bool pickDark in new[] { true, false })
{
    var opening = new FourthRouteOpeningState();
    Equal(false, opening.Finish(), "cannot continue before selection");
    Equal(false, opening.Choose(dark), "cannot select before offers");
    Equal(false, opening.Offer(light, dark), "reject reversed alignments");
    Equal(true, opening.Offer(dark, light), "one dark one light offered");
    Equal(false, opening.Offer(FourthRouteQuest.Pride, FourthRouteQuest.Diligence), "cannot reroll saved offers");
    var restored = System.Text.Json.JsonSerializer.Deserialize<FourthRouteOpeningState>(System.Text.Json.JsonSerializer.Serialize(opening))!;
    Equal(opening.Dark, restored.Dark, "saved dark offer retained");
    Equal(opening.Light, restored.Light, "saved light offer retained");
    Equal(false, restored.Choose((FourthRouteQuest)1000), "unknown quest rejected");
    foreach (var other in Enum.GetValues<FourthRouteQuest>().Where(q => q != dark && q != light))
        Equal(false, restored.Choose(other), "unoffered quest rejected");
    var chosen = pickDark ? dark : light;
    Equal(true, restored.Choose(chosen), "offered choice locks immediately");
    Equal(false, restored.Completed, "choice is not narrative completion");
    Equal(false, restored.Choose(pickDark ? light : dark), "cannot switch choice");
    Equal(false, restored.Choose(chosen), "cannot confirm twice");
    Equal(true, FourthRouteOpeningState.NeedsOpening(true, restored), "chosen unfinished narrative resumes");
    restored = System.Text.Json.JsonSerializer.Deserialize<FourthRouteOpeningState>(System.Text.Json.JsonSerializer.Serialize(restored))!;
    Equal((FourthRouteQuest?)chosen, restored.Chosen, "saved chosen route retained");
    Equal(true, restored.Finish(), "narrative completion succeeds once");
    Equal(false, restored.Finish(), "double continue idempotent");
    Equal(false, FourthRouteOpeningState.NeedsOpening(true, restored), "completed opening not replayed");
    Equal(false, opening.Completed, "saved copy independent");
}
var corruptOpening = new FourthRouteOpeningState { Dark = (FourthRouteQuest)1000, Light = FourthRouteQuest.Humility,
    Chosen = (FourthRouteQuest)1000 };
Equal(false, corruptOpening.HasOffers, "invalid saved enum not legal offer");
Equal(false, corruptOpening.Finish(), "invalid saved offer cannot finish");
Console.WriteLine($"PASS DS27 production opening flow: {checks - beforeOpening} assertions; {checks} total.");

int beforeReward = checks;
foreach (var quest in Enum.GetValues<FourthRouteQuest>())
foreach (var phase in Enum.GetValues<FourthTrialPhase>())
{
    var offer = new FourthRouteRewardOffer(quest, phase);
    bool pending = phase is FourthTrialPhase.FirstReward or FourthTrialPhase.SecondReward or FourthTrialPhase.ThirdReward;
    Equal(pending, offer.IsValid, "only completed unclaimed trials have a reward offer");
    Equal(phase switch { FourthTrialPhase.FirstReward => 1, FourthTrialPhase.SecondReward => 2,
        FourthTrialPhase.ThirdReward => 4, _ => 0 }, offer.Stage, "offer uses exact awarded relic form");
    foreach (var now in Enum.GetValues<FourthTrialPhase>())
        Equal(pending && now == phase, offer.Matches(quest, now), "stale UI cannot claim another phase");
    var other = quest == FourthRouteQuest.Pride ? FourthRouteQuest.Humility : FourthRouteQuest.Pride;
    Equal(false, offer.Matches(other, phase), "stale UI cannot claim another route");
    var restored = System.Text.Json.JsonSerializer.Deserialize<FourthRouteRewardOffer>(System.Text.Json.JsonSerializer.Serialize(offer));
    Equal(offer, restored, "offer identity remains immutable through roundtrip");
}
Equal(false, new FourthRouteRewardOffer((FourthRouteQuest)1000, FourthTrialPhase.FirstReward).IsValid, "unknown route cannot be shown");
Equal(false, new FourthRouteRewardOffer(FourthRouteQuest.Pride, (FourthTrialPhase)1000).IsValid, "unknown phase cannot be shown");
for (int flags = 0; flags < 4096; flags++)
{
    bool Flag(int bit) => (flags & (1 << bit)) != 0;
    const int required = 1 | 2 | 8 | 16;
    const int forbidden = 4 | 32 | 64 | 128 | 256 | 512;
    bool expectedPresentation = (flags & required) == required && (flags & forbidden) == 0
        && (!Flag(10) || Flag(11));
    Equal(expectedPresentation, FourthRouteRewardOffer.CanPresent(Flag(0), Flag(1), Flag(2), Flag(3), Flag(4),
        Flag(5), Flag(6), Flag(7), Flag(8), Flag(9), Flag(10), Flag(11)),
        "idle gates; victory boundary bypasses executor only, never other safety checks");
}
Console.WriteLine($"PASS DS27 production reward presentation: {checks - beforeReward} assertions; {checks} total.");

int beforeNecklace = checks;
int[] clearDeltas = [0, 0, 0, 0, 0, -1, -1, -1, -1, -1, -1];
int[] murkyDeltas = [1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0];
foreach (int corruption in Enumerable.Range(-5, 11))
{
    bool murky = corruption is 3 or 4 or 5;
    Equal(murky, HeartNecklaceRules.IsMurky(corruption), "necklace variation inclusive boundary");
    for (int desire = 0; desire <= 10; desire++)
        Equal((murky ? murkyDeltas : clearDeltas)[desire], HeartNecklaceRules.TurnDelta(corruption, desire),
            $"necklace {corruption}/{desire}: literal table covers five on both branches");
}
Console.WriteLine($"PASS DS27 production necklace rules: {checks - beforeNecklace} assertions; {checks} total.");

int beforeMagicRelics = checks;
foreach (int corruption in Enumerable.Range(-5, 11))
{
    bool variation = corruption is -5 or -4 or -3 or -2;
    Equal(variation, MagicSupportRelicRules.PrayerVariation(corruption), "prayer inclusive minus two");
    foreach (bool ownForm in new[] { false, true })
    foreach (var (current, added, fresh) in new (decimal, decimal, bool)[]
             { (1, 1, true), (9, 9, true), (2, 1, false), (18, 9, false), (0, 0, false), (0, -1, false), (9, 0, false) })
        Equal(variation && ownForm && fresh,
            MagicSupportRelicRules.GrantsOnForm(corruption, ownForm, current, added),
            "prayer actual fresh entry only, all three initial form amounts");
}
int[] counterSequence = [1, 2, 3, 4, 0];
for (int initial = 0; initial < 5; initial++)
{
    int counter = initial;
    for (int turn = 0; turn < 30; turn++)
    {
        counter = MagicSupportRelicRules.NextBarrierTurn(counter);
        Equal(counterSequence[(initial + turn) % 5], counter, "barrier every fifth across combat partitions");
    }
}
Equal(1, MagicSupportRelicRules.NextBarrierTurn(-1), "barrier invalid negative state recovers safely");
Equal(0, MagicSupportRelicRules.NextBarrierTurn(int.MaxValue), "barrier invalid huge state cannot overflow");
Console.WriteLine($"PASS DS27 production magic support relics: {checks - beforeMagicRelics} assertions; {checks} total.");

int beforeReactiveRelics = checks;
foreach (int corruption in Enumerable.Range(-5, 11))
    Equal(corruption is -5 or -4 or -3, ReactiveMagicRelicRules.MirrorVariation(corruption), "mirror inclusive minus three");
for (int flags = 0; flags < 16; flags++)
foreach (decimal change in new decimal[] { -3, -1, 0, 1, 3 })
{
    bool Flag(int bit) => (flags & (1 << bit)) != 0;
    Equal(flags == 7 && change != 0,
        ReactiveMagicRelicRules.ShouldReflect(Flag(0), Flag(1), change, Flag(2), Flag(3)),
        "mirror excludes foreign, non-enemy, unchanged, helpful and recursive events");
}
int[] stardustSequence = [1, 2, 3, 4, 5, 6, 0];
for (int initial = 0; initial < 7; initial++)
{
    int progress = initial;
    for (int card = 0; card < 35; card++)
    {
        progress = ReactiveMagicRelicRules.NextStardust(progress);
        Equal(stardustSequence[(initial + card) % 7], progress, "stardust every seventh from each saved state");
    }
}
Equal(1, ReactiveMagicRelicRules.NextStardust(int.MinValue), "negative progress recovers safely");
Equal(0, ReactiveMagicRelicRules.NextStardust(int.MaxValue), "huge progress cannot overflow");
Console.WriteLine($"PASS DS27 production reactive magic relics: {checks - beforeReactiveRelics} assertions; {checks} total.");

int beforeSeals = checks;
foreach (CorruptionBand band in Enum.GetValues<CorruptionBand>())
foreach (RouteCardKind route in Enum.GetValues<RouteCardKind>())
{
    bool sealedCard = (band, route) is (CorruptionBand.Holy, RouteCardKind.Corrupt)
        or (CorruptionBand.Corrupt, RouteCardKind.Holy);
    Equal(sealedCard, SealRules.IsSealed(band, route), "unchanged sealing truth table");
    for (int flags = 0; flags < 4; flags++)
    {
        string? key = flags == 3 && sealedCard
            ? route == RouteCardKind.Holy ? "MAIDENSUCCUBUS_SEALED_CARD.holy" : "MAIDENSUCCUBUS_SEALED_CARD.corrupt"
            : null;
        Equal(key, SealRules.DescriptionKey((flags & 1) != 0, (flags & 2) != 0, band, route),
            "seal presentation requires correct owner and real permanent instance");
    }
}
Equal(false, SealRules.IsSealed((CorruptionBand)99, RouteCardKind.Holy), "invalid band not sealed");
Equal(false, SealRules.IsSealed(CorruptionBand.Holy, (RouteCardKind)99), "invalid route not sealed");
for (int baseline = 1; baseline <= 20; baseline++)
{
    var tint = new OwnedVisualOverride<int>();
    Equal(baseline, tint.Restore(baseline), "untouched tint unchanged");
    int painted = tint.Apply(baseline, value => value * 2);
    Equal(baseline * 2, painted, "relative tint preserves baseline");
    Equal(painted, tint.Apply(painted, value => value * 2), "refresh does not multiply tint repeatedly");
    Equal(baseline, tint.Restore(painted), "recycled holder restores original not white");
    Equal(baseline, tint.Restore(baseline), "repeated restore inert");
    tint.Apply(baseline, value => value * 2);
    Equal(-baseline, tint.Restore(-baseline), "external color change wins");
    painted = tint.Apply(-baseline, value => value * 3);
    Equal(-baseline * 3, painted, "reassigned external baseline used");
    Equal(-baseline, tint.Restore(painted), "new external baseline restored");
}
Console.WriteLine($"PASS DS27 production seal presentation: {checks - beforeSeals} assertions; {checks} total.");

int beforeSignedLayers = checks;
// Independent examples: type/visibility is engine-owned and covered separately
// in the native card contract. This host executes only production aggregation.
foreach (var (amounts, layers) in new (int[], int)[]
{
    ([], 0), ([0], 0), ([1], 1), ([-1], 1), ([2, 3], 5),
    ([-2, -3], 5), ([-2, 3], 5), ([2, -3], 5),
    ([0, -2, 3, 0], 5), ([-10, 4, -1], 15),
    ([2, 2, 2], 6), ([-2, -2, -2], 6), ([int.MaxValue], int.MaxValue),
})
    Equal(layers, PowerLayerMath.Count(amounts), "signed layer magnitude " + string.Join(",", amounts));
Console.WriteLine($"PASS DS27 production signed power layers: {checks - beforeSignedLayers} assertions; {checks} total.");
