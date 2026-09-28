using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using HumilityCallExtraction;
using MaidenSuccubus.Core.Cards;

if (args.SequenceEqual(new[] { "--self-test" }))
{
    Extraction Extract(string body) => CallExtractor.Extract("namespace Fixture; class Example { async Task OnPlay(Context context, Play play) { " + body + " } }").Single();
    void Check(bool pass, string name) { if (!pass) throw new Exception(name); Console.WriteLine("PASS " + name); }
    string attack = "await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(play.Target).Execute(context);";
    var guarded = Extract("if (ready) { " + attack + " } await CardPileCmd.Draw(context, 2, Owner);");
    Check(guarded.Program?.Effects.Count == 1 && guarded.Program.DoubleAmounts().Effects[0].Amount.Evaluate(default, _ => 7) * 2 == 14,
        "condition and unrelated draw removed; amount retained and doubled by runtime");
    var surf = Extract("while (cost < 3) { var drawn = await CardPileCmd.Draw(context, 1, Owner); if (drawn == null) break; " +
        "await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).TargetingAllOpponents(CombatState).Execute(context); }");
    Check(surf.Program?.Effects.Single() is { Target: HumilityTarget.AllEnemies } effect && effect.Repeats.Evaluate(default, _ => 99) == 1,
        "outer draw loop removed without draw simulation");
    var x = Extract("int count = ResolveEnergyXValue() + 1; await DamageCmd.Attack(4).WithHitCount(count).FromCard(this, play).TargetingRandomOpponents(CombatState).Execute(context);");
    Check(x.Program?.Effects.Single() is { Target: HumilityTarget.RandomEnemy } xe && xe.Repeats.Evaluate(new(3,0,0), _ => 0) == 4,
        "call-chain count and pure local X arithmetic retained");
    var mixed = Extract("await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play); " + attack);
    Check(mixed.Program?.Effects.Select(e => e.Kind).SequenceEqual(new[] { HumilityEffectKind.Block, HumilityEffectKind.Damage }) == true,
        "source order and direct block retained");
    var calculated = Extract("await CreatureCmd.GainBlock(Owner.Creature, ((CalculatedVar)DynamicVars[\"CalculatedBlock\"]).Calculate(play.Target), props, play);");
    Check(calculated.Program?.Effects[0].Amount.Name == "CalculatedBlock", "native calculated variable binding retained");
    var failed = Extract("decimal damage = await SideEffect(); " + attack.Replace("DynamicVars.Damage.BaseValue", "damage"));
    Check(failed.Program == null && failed.Error != null, "side-effectful numeric dependency rejected as whole card, never run");
    Check(Extract("await HiddenHelper();").Program == null, "unknown helper cannot silently become empty effect");
    Check(Extract("await PowerCmd.Apply(context, target);").Program?.Effects.Count == 0, "other command only yields explicit empty program");
    Check(Extract("Action later = () => { " + attack.Replace("await ", "") + " }; ").Program == null,
        "deferred damage is reported, not silently discarded or run immediately");
    Check(CallExtractor.Extract("class Broken {").Single().Program == null, "malformed source is rejected");
    Check(CallExtractor.Extract("class Example { Task OnPlay(Context context, Play play) => HiddenHelper(); }").Single().Program == null,
        "expression-bodied helper cannot silently become empty effect");
    var sink = new RecordedEffects();
    await x.Program!.DoubleAmounts().Execute(new(3, 0, 0), _ => throw new Exception("unexpected variable lookup"), sink);
    Check(sink.Attacks.SequenceEqual(new[] { (8m, HumilityTarget.RandomEnemy, 4) }),
        "extracted program executes through production runtime with doubled amount and original multi-hit boundary");
    JsonObject CatalogCard(string name, HumilityEffectProgram? program, string? error) => new()
    {
        ["card"] = name, ["status"] = program == null ? "unsupported" : "extracted", ["program"] = program?.Save(), ["error"] = error,
    };
    var records = new JsonArray(CatalogCard("Fixture.Ready", mixed.Program, null), CatalogCard("Fixture.Pending", null, "unsupported formula"),
        CatalogCard("Fixture.Empty", new HumilityEffectProgram([]), null));
    var document = new JsonObject { ["schemaVersion"] = 1, ["cards"] = records };
    var catalog = new HumilityExtractedCatalog(document.ToJsonString());
    Check(catalog.Entries["Fixture.Ready"].Program?.Effects.Count == 2
        && catalog.Entries["Fixture.Pending"].Program == null && catalog.Entries["Fixture.Pending"].Error != null
        && catalog.Entries["Fixture.Empty"].Program?.Effects.Count == 0, "catalog distinguishes complete, unsupported and genuine empty programs");
    records.Add(CatalogCard("Fixture.Ready", mixed.Program, null));
    bool rejected = false;
    try { _ = new HumilityExtractedCatalog(document.ToJsonString()); } catch (FormatException) { rejected = true; }
    Check(rejected, "duplicate generated type identities rejected");
    records.RemoveAt(records.Count - 1);
    records[1]!["status"] = "extracted";
    rejected = false;
    try { _ = new HumilityExtractedCatalog(document.ToJsonString()); } catch (FormatException) { rejected = true; }
    Check(rejected, "inconsistent generated status cannot load as an empty effect");
    const string helperSource = """
        namespace Fixture;
        static class Effects {
          static Task Hit(CardModel card, Context context, Play play, decimal amount, int hits = 1) {
            decimal doubled = amount * 2;
            return DamageCmd.Attack(doubled).FromCard(card, play).WithHitCount(hits).Targeting(play.Target).Execute(context);
          }
        }
        class ThroughHelper {
          Task OnPlay(Context context, Play play) {
            int count = ResolveEnergyXValue();
            Effects.Hit(this, context, play, 3);
            Effects.Hit(this, context, play, hits: count, amount: 4);
            return Task.CompletedTask;
          }
        }
        """;
    var helper = CallExtractor.Extract(helperSource).Single();
    Check(helper.Program?.Effects.Count == 2 && helper.Program.Effects[0].Amount.Evaluate(default, _ => 0) == 6
        && helper.Program.Effects[1].Amount.Evaluate(default, _ => 0) == 8
        && helper.Program.Effects[0].Repeats.Evaluate(default, _ => 0) == 1
        && helper.Program.Effects[1].Repeats.Evaluate(new(3,0,0), _ => 0) == 3,
        "source helper expansion preserves defaults, named arguments, caller X and independent local scopes");
    const string recursive = "class Recursive { Task OnPlay(Context c, Play p) => Loop(); Task Loop() { Loop(); return Task.CompletedTask; } }";
    Check(CallExtractor.Extract(recursive).Single().Error?.Contains("Recursive helper") == true,
        "recursive helpers rejected without execution or infinite expansion");
    const string overloads = "class Ambiguous { Task OnPlay(Context c, Play p) => Hit(1); Task Hit(int a) => Task.CompletedTask; Task Hit(decimal a) => Task.CompletedTask; }";
    Check(CallExtractor.Extract(overloads).Single().Error?.Contains("Ambiguous helper") == true,
        "overload ambiguity is reported rather than selecting a different effect");
    const string instance = "class LocalHelper { Task OnPlay(Context context, Play play) => Hit(context, play); Task Hit(Context context, Play play) { return CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play); } }";
    Check(CallExtractor.Extract(instance).Single().Program?.Effects.Single().Kind == HumilityEffectKind.Block,
        "expression-bodied local helper resolves retained block");
    var visual = Extract(attack.Replace(".Execute(context)", ".WithHitVfxSpawnedAtBase().WithNoAttackerAnim().Execute(context)") + " await VfxCmd.PlayOnCreatureCenter(Owner.Creature, effect);");
    Check(visual.Program?.Effects.Count == 1, "visual modifiers and standalone visual command do not block extraction");
    var foreignSource = Extract(attack.Replace(".FromCard(this, play)", ".FromOsty(Owner.Osty, this, play)"));
    Check(foreignSource.Program == null, "different attacker source is not incorrectly discarded as a visual setting");
    var extraEffects = Extract(attack + " await CombatEnchantmentCmd.Apply<AnyEnchant>(card, 1); await ForgeCmd.Forge(3, Owner, this); await OstyCmd.Summon(context, Owner, 3, this);");
    Check(extraEffects.Program?.Effects.Count == 1, "new enchantment, forging and summoning are removed rather than extra damage");
    Check(CallExtractor.Extract("class Returned { Task OnPlay(Context c, Play p) { return Hidden(); } }").Single().Program == null,
        "unresolved returned helper is not silently treated as empty");
    return 0;
}
if (args.Length == 2 && args[0] == "--verify-assembly")
{
    using var pe = new PEReader(File.OpenRead(args[1]));
    var metadata = pe.GetMetadataReader();
    var resource = metadata.ManifestResources.Select(metadata.GetManifestResource)
        .Single(r => metadata.GetString(r.Name) == "MaidenSuccubus.HumilityExtractedCatalog.json");
    if (!resource.Implementation.IsNil) throw new FormatException("Catalog must be embedded, not linked");
    var section = pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);
    var blob = section.GetReader(checked((int)resource.Offset), section.Length - checked((int)resource.Offset));
    int length = blob.ReadInt32();
    var catalog = new HumilityExtractedCatalog(Encoding.UTF8.GetString(blob.ReadBytes(length)));
    Console.WriteLine($"PASS embedded catalog parsed without loading game assemblies: {catalog.Entries.Count} entries; " +
        $"{catalog.Entries.Values.Count(e => e.Program == null)} explicitly unsupported.");
    return 0;
}
if (args.Length == 0) { Console.Error.WriteLine("Usage: HumilityCallExtraction --self-test | [source.cs ...] [--source-dir DIR ...] [--output FILE]"); return 1; }
var sources = new SortedSet<string>(StringComparer.Ordinal);
string? output = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--source-dir")
    {
        if (++i >= args.Length) throw new ArgumentException("Missing source directory");
        foreach (string path in Directory.EnumerateFiles(args[i], "*.cs", SearchOption.AllDirectories))
            sources.Add(Path.GetFullPath(path));
    }
    else if (args[i] == "--output")
    {
        if (++i >= args.Length || output != null) throw new ArgumentException("Expected one output file");
        output = Path.GetFullPath(args[i]);
    }
    else if (args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException("Unknown option " + args[i]);
    else sources.Add(Path.GetFullPath(args[i]));
}
if (sources.Count == 0) throw new ArgumentException("No source files");
if (output != null && sources.Contains(output)) throw new ArgumentException("Output cannot replace input source");
var cards = new JsonArray();
var hashes = new JsonArray();
bool unsupported = false;
var sourceTexts = sources.ToDictionary(path => path, File.ReadAllText, StringComparer.Ordinal);
var methodIndex = HelperExpansion.Index(sourceTexts.Values);
foreach (string path in sources)
{
    string source = sourceTexts[path];
    string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    hashes.Add(new JsonObject { ["file"] = Path.GetFileName(path), ["sha256"] = hash });
    foreach (var card in CallExtractor.Extract(source, methodIndex))
    {
        unsupported |= card.Program == null;
        cards.Add(new JsonObject { ["source"] = Path.GetFileName(path), ["sourceHash"] = hash, ["card"] = card.Card, ["line"] = card.Line,
            ["status"] = card.Program == null ? "unsupported" : "extracted", ["program"] = card.Program?.Save(), ["error"] = card.Error });
    }
}
string json = new JsonObject { ["schemaVersion"] = 1, ["sources"] = hashes, ["cards"] = cards }.ToJsonString(new() { WriteIndented = true });
_ = new HumilityExtractedCatalog(json); // Detect duplicate identities/corrupt output before replacing any artifact.
if (output != null)
{
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    File.WriteAllText(output, json, new UTF8Encoding(false));
    int supported = cards.Count(c => c!["status"]!.GetValue<string>() == "extracted");
    Console.WriteLine($"Generated humility catalog: {supported} extracted, {cards.Count - supported} unsupported (diagnostics retained). {output}");
    return 0; // A generated diagnostic entry is NOT a supported/empty effect program.
}
Console.WriteLine(json);
return unsupported ? 2 : 0;

internal sealed class RecordedEffects : IHumilityEffectSink
{
    public bool CanContinue => true;
    internal List<(decimal Amount, HumilityTarget Target, int Hits)> Attacks { get; } = [];
    public Task Damage(decimal amount, HumilityTarget target, int hits)
    {
        Attacks.Add((amount, target, hits));
        return Task.CompletedTask;
    }
    public Task Block(decimal amount, HumilityTarget target, int repeats) => Task.CompletedTask;
}
