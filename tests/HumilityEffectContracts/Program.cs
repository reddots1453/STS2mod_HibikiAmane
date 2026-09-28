using System.Text.Json.Nodes;
using MaidenSuccubus.Core.Cards;
using static MaidenSuccubus.Core.Cards.HumilityValueKind;

int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++;
}
void Reject(Action action, string message)
{
    bool rejected = false;
    try { action(); }
    catch (Exception error) when (error is FormatException or ArgumentException or InvalidOperationException or OverflowException)
    { rejected = true; }
    Check(rejected, message);
}
static HumilityValue N(decimal number) => HumilityValue.Number(number);
static HumilityEffect D(HumilityValue amount, HumilityValue? repeats = null,
    HumilityTarget target = HumilityTarget.Selected) => new(HumilityEffectKind.Damage, target, amount, repeats ?? N(1));
static HumilityEffect B(HumilityValue amount, HumilityValue? repeats = null) =>
    new(HumilityEffectKind.Block, HumilityTarget.Self, amount, repeats ?? N(1));
static decimal Missing(string name) => throw new InvalidOperationException("Unexpected lookup: " + name);

// Value and repetition are separate even when the original card shares one variable.
var shared = HumilityValue.Named("Shared");
var original = new HumilityEffectProgram([D(shared, shared), B(N(5), N(2))]);
var doubled = original.DoubleAmounts();
var quadrupled = doubled.DoubleAmounts();
foreach (var (program, multiplier) in new[] { (original, 1), (doubled, 2), (quadrupled, 4) })
{
    var sink = new Sink();
    await program.Execute(default, _ => 3, sink);
    Check(sink.Calls.Count == 5, "multipliers do not change three hits/two blocks");
    Check(sink.Groups.SequenceEqual(new[] { 3, 2 }), "one native multi-hit attack then repeated block; no per-hit attack hooks");
    Check(sink.Calls.Take(3).All(call => call == (HumilityEffectKind.Damage, 3m * multiplier, HumilityTarget.Selected)), "damage multiplied");
    Check(sink.Calls.Skip(3).All(call => call == (HumilityEffectKind.Block, 5m * multiplier, HumilityTarget.Self)), "block multiplied");
}
Check(original.AmountMultiplier == 1 && doubled.AmountMultiplier == 2, "repeat application leaves old instance intact");

// All X sources are ledger VALUE, so a free/replayed card need not spend resources again.
foreach (var source in new[] { EnergyX, StarX, SecondaryX })
foreach (int x in new[] { 0, 1, 3, 8 })
foreach (int upgradeBonus in new[] { 0, 1 })
{
    var values = new HumilityXValues(source == EnergyX ? x : 0, source == StarX ? x : 0, source == SecondaryX ? x : 0);
    var program = new HumilityEffectProgram([D(N(6), HumilityValue.Binary(Add, HumilityValue.X(source), N(upgradeBonus)), HumilityTarget.AllEnemies)]).DoubleAmounts();
    var sink = new Sink();
    await program.Execute(values, Missing, sink);
    Check(sink.Calls.Count == x + upgradeBonus, "X + upgrade bonus hit count preserved");
    Check(sink.Calls.All(call => call.Amount == 12 && call.Target == HumilityTarget.AllEnemies), "all-enemy target preserved for each hit");
    var replay = new Sink();
    await program.Execute(values, Missing, replay);
    Check(sink.Calls.SequenceEqual(replay.Calls), "same ledger replay produces identical program");
    var restored = HumilityEffectProgram.Load(JsonNode.Parse(program.Save().ToJsonString()));
    var restoredSink = new Sink();
    await restored.Execute(values, Missing, restoredSink);
    Check(sink.Calls.SequenceEqual(restoredSink.Calls), "serialized formula/multiplier/target preserved");
}
var doubleX = new HumilityEffectProgram([D(HumilityValue.Binary(Multiply, N(6), HumilityValue.X(SecondaryX)), HumilityValue.X(EnergyX))]).DoubleAmounts();
var doubleXSink = new Sink();
await doubleX.Execute(new(3, 0, 4), Missing, doubleXSink);
Check(doubleXSink.Calls.Count == 3 && doubleXSink.Calls.All(call => call.Amount == 48), "6Y becomes 12Y, X repetitions unchanged");

// Named values resolve at their own operation, not before preceding operations.
decimal currentBlock = 0;
var orderedSink = new Sink { OnCall = call => { if (call.Kind == HumilityEffectKind.Block) currentBlock += call.Amount; } };
await new HumilityEffectProgram([B(N(5)), D(HumilityValue.Named("CurrentBlock"))]).DoubleAmounts()
    .Execute(default, _ => currentBlock, orderedSink);
Check(orderedSink.Calls.Select(call => call.Amount).SequenceEqual(new[] { 10m, 20m }), "second formula sees preceding block");
var fractionalSink = new Sink();
await new HumilityEffectProgram([D(N(1.25m), N(2.9m))]).DoubleAmounts().Execute(default, Missing, fractionalSink);
Check(fractionalSink.Calls.Count == 2 && fractionalSink.Calls.All(call => call.Amount == 2.5m), "only hit counts truncate; native amount rounding retained");
var zeroSink = new Sink();
await new HumilityEffectProgram([D(HumilityValue.Named("NeverEvaluated"), N(0)), D(N(5), N(-1))])
    .Execute(default, Missing, zeroSink);
Check(zeroSink.Calls.Count == 0, "nonpositive repetition emits nothing and does not evaluate amount");

foreach (var target in Enum.GetValues<HumilityTarget>())
{
    var sink = new Sink();
    await new HumilityEffectProgram([D(N(0), N(1), target)]).Execute(default, Missing, sink);
    Check(sink.Calls.Single() == (HumilityEffectKind.Damage, 0m, target), "zero amount remains a native effect, target preserved");
}
Check(!new HumilityEffectProgram([]).HasDamageOrBlock, "empty rewrite is not a pure damage/block card");
Check(original.HasDamageOrBlock, "nonempty program exposes structural purity input");
var input = new List<HumilityEffect> { D(N(3)) };
var copied = new HumilityEffectProgram(input);
input.Clear();
Check(copied.Effects.Count == 1, "caller cannot mutate stored effect list");
var clamped = HumilityValue.Binary(Max, N(0), HumilityValue.Binary(Min, N(4), N(-2)));
Check(clamped.Evaluate(default, Missing) == 0, "explicit min/max arithmetic retained");

// No background work after combat termination and no rollback of resolved operations.
var stopped = new Sink();
stopped.OnCall = _ => stopped.CanContinue = false;
await quadrupled.Execute(default, _ => 3, stopped);
Check(stopped.Groups.SequenceEqual(new[] { 3 }), "combat exit stops next operation; native adapter owns in-flight multi-hit stopping");
var cancelled = new CancellationTokenSource();
var cancellationSink = new Sink { OnCall = _ => cancelled.Cancel() };
bool observedCancellation = false;
try { await quadrupled.Execute(default, _ => 3, cancellationSink, cancelled.Token); }
catch (OperationCanceledException) { observedCancellation = true; }
Check(observedCancellation && cancellationSink.Groups.SequenceEqual(new[] { 3 }), "cancellation propagates; resolved native attack not rolled back");
var faultSink = new Sink { OnCall = _ => throw new InvalidOperationException("native failure") };
bool propagated = false;
try { await doubled.Execute(default, _ => 3, faultSink); }
catch (InvalidOperationException) { propagated = true; }
Check(propagated && faultSink.Calls.Count == 1, "native failure never runs later operations");

var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var delayedSink = new Sink { Delay = waiting.Task };
Task execution = doubled.Execute(default, _ => 3, delayedSink);
Check(!execution.IsCompleted && delayedSink.Groups.SequenceEqual(new[] { 3 }), "program awaits native attack before starting block");
waiting.SetResult();
await execution;
Check(delayedSink.Groups.SequenceEqual(new[] { 3, 2 }), "block follows completed attack in source order");

Reject(() => HumilityEffectProgram.Load(null), "missing state rejected");
Reject(() => HumilityEffectProgram.Load(new JsonObject { ["version"] = 2, ["effects"] = new JsonArray() }), "future schema rejected");
foreach (string field in new[] { "kind", "target", "amount", "repeats" })
{
    JsonObject state = doubled.Save();
    ((JsonObject)state["effects"]![0]!).Remove(field);
    Reject(() => HumilityEffectProgram.Load(state), "missing operation field rejected: " + field);
}
foreach (string invalid in new[] { "Exhaust", "Draw", "If", "999", "0", "damage" })
{
    JsonObject state = doubled.Save();
    state["effects"]![0]!["kind"] = invalid;
    Reject(() => HumilityEffectProgram.Load(state), "unknown and noncanonical effect kind rejected: " + invalid);
}
Reject(() => new HumilityEffectProgram([], 0), "zero multiplier rejected");
Reject(() => new HumilityEffectProgram([], -1), "negative multiplier rejected");
Reject(() => new HumilityEffectProgram([], decimal.MaxValue).DoubleAmounts(), "multiplier overflow rejected without mutation");
Reject(() => HumilityValue.X(Constant), "invalid X source rejected");
Reject(() => HumilityValue.Binary(Named, N(1), N(2)), "invalid binary operator rejected");
Reject(() => HumilityValue.Named(" "), "empty variable rejected");
JsonNode deep = N(1).Save();
for (int i = 0; i < 40; i++) deep = new JsonObject { ["kind"] = "Add", ["left"] = deep, ["right"] = N(1).Save() };
Reject(() => HumilityValue.Load(deep), "corrupt/deep expression rejected");
// Exercise the actual production card definitions, not a test-only reconstruction.
var profiles = HumilityProfileDefinitions.All;
Check(profiles.Count == 80, "reviewed groups have 72 Maiden and 8 native profiles");
Check(!profiles.ContainsKey("maiden:Surf") && !profiles.ContainsKey("foreign:PommelStrike"),
    "unsupported formulas and foreign names cannot silently become single-hit damage");
foreach (var pair in profiles)
{
    var loaded = HumilityEffectProgram.Load(pair.Value.Save());
    Check(loaded.Save().ToJsonString() == pair.Value.Save().ToJsonString(), "every actual definition roundtrips: " + pair.Key);
    Check(pair.Value.AmountMultiplier == 1, "shared base definition starts undoubled: " + pair.Key);
    var sink = new Sink();
    await pair.Value.DoubleAmounts().Execute(new(3, 0, 4), name => name switch
    { "Damage" => 7, "Block" => 5, "Hits" => 2, "$enemies" => 3, _ => throw new Exception("Unexpected variable " + name) }, sink);
    Check(pair.Value.AmountMultiplier == 1, "execution cannot mutate shared catalog: " + pair.Key);
}
async Task Profile(string key, HumilityXValues x, Func<string, decimal> vars,
    decimal amount, int repeats, HumilityEffectKind kind, HumilityTarget target)
{
    var sink = new Sink();
    await profiles[key].DoubleAmounts().Execute(x, vars, sink);
    Check(sink.Calls.Count == repeats && sink.Calls.All(call => call == (kind, amount, target)), "actual card program: " + key);
    Check(sink.Groups.SequenceEqual(repeats > 0 ? new[] { repeats } : Array.Empty<int>()), "native command boundary: " + key);
}
foreach (bool upgraded in new[] { false, true })
{
    await Profile("vanilla:PommelStrike", default, _ => upgraded ? 10 : 9, upgraded ? 20 : 18, 1,
        HumilityEffectKind.Damage, HumilityTarget.Selected);
    await Profile("maiden:DoubleDefense", default, _ => upgraded ? 6 : 4, upgraded ? 12 : 8, 2,
        HumilityEffectKind.Block, HumilityTarget.Self);
    await Profile("maiden:ExplosiveImpact", default, _ => upgraded ? 5 : 3, upgraded ? 10 : 6, 2,
        HumilityEffectKind.Damage, HumilityTarget.RandomEnemy);
    await Profile("maiden:UltimateFlare", default, _ => upgraded ? 52 : 40, upgraded ? 104 : 80, 1,
        HumilityEffectKind.Damage, HumilityTarget.AllEnemies);
    foreach (int hits in new[] { 2, 3, 8 })
        await Profile("maiden:Takemikazuchi", default, name => name == "Hits" ? hits : upgraded ? 8 : 6,
            upgraded ? 16 : 12, hits, HumilityEffectKind.Damage, HumilityTarget.Selected);
    foreach (int energy in new[] { 0, 1, 4 })
    {
        await Profile("vanilla:Whirlwind", new(energy, 0, 0), _ => upgraded ? 8 : 5,
            upgraded ? 16 : 10, energy, HumilityEffectKind.Damage, HumilityTarget.AllEnemies);
        foreach (int secondary in new[] { 0, 1, 7 })
            await Profile("maiden:AllHopeLost", new(energy, 0, secondary), name => name == "Hits" ? upgraded ? 1 : 0 : 6,
                secondary * 12, energy + (upgraded ? 1 : 0), HumilityEffectKind.Damage, HumilityTarget.Selected);
    }
}
foreach (string name in new[] { "Transform", "IceShield", "AcceleratedMotion", "HealingArt", "Fusion",
    "MagiciansSecret", "Procrastinate", "Bath", "CurseInfection", "ForgeCharge", "BeyondReasonForge", "CalmingMist", "DreamMist" })
{
    var sink = new Sink();
    var program = profiles["maiden:" + name].DoubleAmounts();
    await program.Execute(default, Missing, sink);
    Check(!program.HasDamageOrBlock && sink.Groups.Count == 0, "explicit empty card is not a pure-effect card: " + name);
}
foreach (bool upgraded in new[] { false, true })
{
    foreach (int enemies in new[] { 0, 1, 2, 5 })
        await Profile("maiden:DragonflyTouch", default, name => name == "$enemies" ? enemies : upgraded ? 10 : 7,
            upgraded ? 20 : 14, enemies, HumilityEffectKind.Block, HumilityTarget.Self);
    await Profile("maiden:ExternalPowerSkeleton", default, _ => upgraded ? 8 : 6,
        upgraded ? 16 : 12, 1, HumilityEffectKind.Damage, HumilityTarget.AllEnemies);
    await Profile("maiden:AutoReactionArmor", default, _ => upgraded ? 7 : 5,
        upgraded ? 14 : 10, 1, HumilityEffectKind.Block, HumilityTarget.Self);
    await Profile("maiden:ForgeNimble", default, _ => upgraded ? 8 : 5,
        upgraded ? 16 : 10, 1, HumilityEffectKind.Block, HumilityTarget.Self);
}
foreach (string name in new[] { "TacticalAnalyzer", "Tranquilizer", "Stigma", "Rest", "MultipleReproduction",
    "SunDance", "DivineEcho", "OriginalSinBrand", "Chant", "Gospel", "SneakSnack", "HolyFlame",
    "ExorcismPerfume", "PurificationOrb", "GuardianScripture", "NimbleScripture", "PunishmentScripture",
    "WisdomScripture", "VitalityScripture", "BlissScripture" })
{
    var sink = new Sink();
    await profiles["maiden:" + name].DoubleAmounts().Execute(default, Missing, sink);
    Check(sink.Calls.Count == 0 && !profiles["maiden:" + name].HasDamageOrBlock,
        "removing power/selection effects does not synthesize damage or block: " + name);
}
Console.WriteLine($"PASS {checks} humility program/profile assertions (production source linked; no game integration claim).");

internal sealed class Sink : IHumilityEffectSink
{
    public bool CanContinue { get; set; } = true;
    internal List<(HumilityEffectKind Kind, decimal Amount, HumilityTarget Target)> Calls { get; } = [];
    internal List<int> Groups { get; } = [];
    internal Task Delay { get; set; } = Task.CompletedTask;
    internal Action<(HumilityEffectKind Kind, decimal Amount, HumilityTarget Target)>? OnCall { get; set; }
    public Task Damage(decimal baseAmount, HumilityTarget target, int hits) => Emit(HumilityEffectKind.Damage, baseAmount, target, hits);
    public Task Block(decimal baseAmount, HumilityTarget target, int repetitions) => Emit(HumilityEffectKind.Block, baseAmount, target, repetitions);
    private Task Emit(HumilityEffectKind kind, decimal amount, HumilityTarget target, int repetitions)
    {
        Groups.Add(repetitions);
        var call = (kind, amount, target);
        for (int index = 0; index < repetitions; index++)
        {
            Calls.Add(call);
            OnCall?.Invoke(call);
        }
        return Delay;
    }
}
