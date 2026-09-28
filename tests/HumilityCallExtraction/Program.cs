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
    Check(foreignSource.Program?.Effects.Single().Source == HumilityAttackSource.Osty,
        "Osty attack source retained instead of becoming player damage");
    var restoredSource = HumilityEffectProgram.Load(foreignSource.Program!.DoubleAmounts().Save());
    var sourceSink = new RecordedEffects();
    await restoredSource.Execute(default, _ => 7, sourceSink);
    Check(sourceSink.Sources.SequenceEqual(new[] { HumilityAttackSource.Osty }) && sourceSink.Attacks.Single().Amount == 14,
        "attack source survives doubling, serialization and production execution");
    var legacy = mixed.Program!.Save();
    legacy["version"] = 1;
    foreach (JsonNode? op in (JsonArray)legacy["effects"]!) ((JsonObject)op!).Remove("source");
    Check(HumilityEffectProgram.Load(legacy).Effects.All(e => e.Source == HumilityAttackSource.Card),
        "version one programs retain original card source");
    var invalidSource = restoredSource.Save();
    invalidSource["effects"]![0]!["source"] = "Unknown";
    rejected = false;
    try { _ = HumilityEffectProgram.Load(invalidSource); } catch (FormatException) { rejected = true; }
    Check(rejected, "unknown saved attack source rejected");
    var invalidBlockSource = mixed.Program.Save();
    invalidBlockSource["effects"]![0]!["source"] = "Osty";
    rejected = false;
    try { _ = HumilityEffectProgram.Load(invalidBlockSource); } catch (ArgumentException) { rejected = true; }
    Check(rejected, "block cannot carry an attack-only source");
    Check(Extract(attack.Replace(".FromCard(this, play)", ".FromOsty(otherPet, this, play)")).Program == null,
        "unknown pet source cannot silently become owner's Osty");
    var secondary = Extract("decimal damage = DynamicVars.Damage.BaseValue * play.SecondaryResources().Value(DesireResource.Id); "
        + attack.Replace("DynamicVars.Damage.BaseValue", "damage").Replace(".Execute(context)", ".WithHitCount(ResolveEnergyXValue()).Execute(context)"));
    var secondarySink = new RecordedEffects();
    await secondary.Program!.DoubleAmounts().Execute(new(2, 0, 3), _ => 6, secondarySink);
    Check(secondarySink.Attacks.SequenceEqual(new[] { (36m, HumilityTarget.Selected, 2) }),
        "secondary X amount and energy X hits come from separate play ledger values");
    var upgradeCount = Extract("int hits = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0); "
        + attack.Replace(".Execute(context)", ".WithHitCount(hits).Execute(context)"));
    Check(upgradeCount.Program?.Effects.Single().Repeats.Evaluate(new(2,0,0), _ => 0) == 2
        && upgradeCount.Program.Effects.Single().Repeats.Evaluate(new(2,0,0), _ => 1) == 3,
        "numeric upgrade branch remains live without restoring outer trigger conditions");
    Check(Extract(attack.Replace("DynamicVars.Damage.BaseValue", "other.SecondaryResources().Value(DesireResource.Id)")).Program == null
        && Extract(attack.Replace("DynamicVars.Damage.BaseValue", "play.SecondaryResources().Value(UnknownResource.Id)")).Program == null,
        "foreign ledger or unrecognized resource is not assumed to be secondary X");
    var extraEffects = Extract(attack + " await CombatEnchantmentCmd.Apply<AnyEnchant>(card, 1); await ForgeCmd.Forge(3, Owner, this); await OstyCmd.Summon(context, Owner, 3, this);");
    Check(extraEffects.Program?.Effects.Count == 1, "new enchantment, forging and summoning are removed rather than extra damage");
    Check(CallExtractor.Extract("class Returned { Task OnPlay(Context c, Play p) { return Hidden(); } }").Single().Program == null,
        "unresolved returned helper is not silently treated as empty");
    string split = "var command = DamageCmd.Attack(4).FromCard(this, play); "
        + "await CreatureCmd.GainBlock(Owner.Creature, 3, play); command.Targeting(play.Target); await command.Execute(context);";
    var splitProgram = Extract(split).Program;
    Check(splitProgram?.Effects.Select(e => e.Kind).SequenceEqual(new[] { HumilityEffectKind.Block, HumilityEffectKind.Damage }) == true,
        "split builder is ordered at Execute, not construction before block");
    var splitSink = new RecordedEffects();
    await splitProgram!.DoubleAmounts().Execute(default, _ => 0, splitSink);
    Check(splitSink.Order.SequenceEqual(new[] { "Block:6", "Damage:8" }),
        "production execution preserves actual block-attack order for split chain");
    var branched = Extract("var command = DamageCmd.Attack(4).FromCard(this, play); "
        + "if (externalAllTargets) command = command.TargetingAllOpponents(CombatState); else command = command.Targeting(play.Target); "
        + "await command.Execute(context);");
    Check(branched.Program?.Effects.Single().Target == HumilityTarget.CurrentCardTarget,
        "exclusive single/all target branches use card's live target type");
    Check(Extract(split.Replace("command.Targeting(play.Target);", "command.Targeting(play.Target); command.TargetingAllOpponents(CombatState);")).Program == null,
        "sequential conflicting targets are not misread as dynamic branch");
    Check(Extract(split.Replace("await command.Execute(context);", "command = OtherAttack(); await command.Execute(context);")).Program == null
        && Extract(split + "await command.Execute(context);").Program == null,
        "unknown builder reassignment and repeated execution do not silently merge");
    Check(Extract(split.Replace("command.Targeting(play.Target);", "if (flag) command.WithHitCount(2); else command.WithHitCount(3); command.Targeting(play.Target);")).Program == null,
        "conflicting branch hit counts require numeric resolution rather than last branch wins");
    var beforeVisual = Extract(attack.Replace(".Execute(context)", ".BeforeDamage(async () => { var fx = NBeamVfx.Create(Owner.Creature); NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(fx); await Cmd.Wait(0.2f); }).OnlyPlayAnimOnce().Execute(context)"));
    Check(beforeVisual.Program?.Effects.Count == 1, "recognized visual-only BeforeDamage removed with animation flags");
    Check(Extract(attack.Replace(".Execute(context)", ".BeforeDamage(() => HiddenEffect()).Execute(context)")).Program == null,
        "unknown BeforeDamage behavior is not guessed to be visual");
    Check(Extract(attack.Replace(".Execute(context)", ".BeforeDamage(() => { Owner.Creature.CurrentHp = 0; return Task.CompletedTask; }).Execute(context)")).Program == null,
        "callback state mutation cannot pass the visual-only classifier without method calls");
    var otherStatements = Extract(attack + " CardModel card = CreateCard(); card.SetToFreeThisTurn(); card.EnergyCost.SetThisCombat(0); "
        + "List<CardModel> generated = new List<CardModel>(); generated.Add(card); generated.Clear();");
    Check(otherStatements.Program?.Effects.Count == 1, "typed card cost and list maintenance are deleted as other effects");
    Check(Extract("Unknown generated = GetUnknown(); generated.Add(1);").Program == null,
        "unknown Add method is not assumed to be list maintenance");
    var boundaries = Extract(attack + " await CondemnationCmd.Apply(context, play.Target, 2); "
        + "await TransformationCmd.GainArmor(context, Owner.Creature, 2, this); GeneratedCardCostCmd.SetFreeThisTurn(this); "
        + "Data.Desire.Modify(Owner, 2); await TemperancePileCmd.Play(context, Owner, 1);");
    Check(boundaries.Program?.Effects.Count == 1, "independent status, resource, armor and autoplay effects are deleted at command boundary");
    Check(Extract("CardModel? selected = (await CardSelectCmd.FromHand(context, Owner)).FirstOrDefault(); "
        + "selected.FinalizeUpgradeInternal();").Program?.Effects.Count == 0,
        "nullable selected-card configuration is removed without executing selection");
    Check(Extract("var power = await PowerCmd.Apply<ExamplePower>(context, Owner); power.Schedule(); "
        + "(await PowerCmd.Apply<ExamplePower>(context, Owner)).SetDamage(5);").Program?.Effects.Count == 0,
        "power-returned configuration is removed as part of the power effect");
    const string boundarySource = """
        namespace Fixture;
        class Example { Task OnPlay(Context context, Play play) { return PowerCmd.Apply(context, play); } }
        static class PowerCmd {
          static Task Apply(Context context, Play play) {
            return DamageCmd.Attack(99).FromCard(this, play).Targeting(play.Target).Execute(context);
          }
        }
        """;
    Check(CallExtractor.Extract(boundarySource).Single().Program?.Effects.Count == 0,
        "deleted power internals are not inlined back into a card's retained damage");
    const string overloadSource = """
        namespace Fixture;
        class Example { Task OnPlay(Context context, Play play) {
          Effects.Run(base.Owner, DynamicVars.Hits.IntValue, base.CombatState, context, play);
          return Task.CompletedTask;
        } }
        static class Effects {
          static Task Run(Player owner, ICombatState state, Player creator, Context context, Play play) => Hidden();
          static Task Run(Player owner, int count, ICombatState state, Context context, Play play) {
            return DamageCmd.Attack(4).FromCard(this, play).WithHitCount(count).Targeting(play.Target).Execute(context);
          }
        }
        """;
    Check(CallExtractor.Extract(overloadSource).Single().Program?.Effects.Single().Repeats.Name == "Hits",
        "API argument shapes disambiguate same-arity overloads without card identities");
    const string returnedCollection = """
        namespace Fixture;
        class Example { Task OnPlay(Context context, Play play) => Effects.Generate(base.Owner, base.CombatState); }
        static class Effects {
          static async Task<CardModel> Generate(Player owner, ICombatState state) {
            return (await Generate(owner, 1, state)).FirstOrDefault();
          }
          static Task<IEnumerable<CardModel>> Generate(Player owner, int count, ICombatState state) {
            CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, owner);
            return Array.Empty<CardModel>();
          }
        }
        """;
    Check(CallExtractor.Extract(returnedCollection).Single().Program?.Effects.Count == 0,
        "arity-resolved overload delegation and returned collection selection do not restore card generation");
    const string mutationHelper = """
        namespace Fixture;
        class Example {
          Task OnPlay(Context context, Play play) {
            foreach (Example other in cards) other.Grow(2);
            return Task.CompletedTask;
          }
          void Grow(decimal amount) { DynamicVars.Damage.BaseValue += amount; Growth += amount; }
        }
        """;
    Check(CallExtractor.Extract(mutationHelper).Single().Program?.Effects.Count == 0,
        "source-proven field-only helper on another card is deleted without impersonating its receiver");
    Check(Extract("var power = GetUnknownPower(); power.Schedule();").Program == null,
        "unknown receiver configuration remains unsupported");
    Check(Extract(attack).OnlyDamageAndBlock && mixed.OnlyDamageAndBlock && x.OnlyDamageAndBlock,
        "pure direct, mixed and X attacks carry positive original-effect evidence");
    Check(!guarded.OnlyDamageAndBlock && !boundaries.OnlyDamageAndBlock && !surf.OnlyDamageAndBlock,
        "successful slicing does not imply original purity after deleting draw, conditions or other effects");
    Check(!Extract(attack + " Growth++;").OnlyDamageAndBlock,
        "non-command state growth is still an additional original effect");
    var withHook = CallExtractor.Extract("class Example { Task OnPlay(Context context, Play play) { " + attack
        + " } public override Task AfterCardPlayed(Context context, Play play) => CardPileCmd.Draw(context, 1, Owner); }").Single();
    Check(!withHook.OnlyDamageAndBlock, "intrinsic after-play trigger prevents original pure classification");
    Check(visual.OnlyDamageAndBlock && beforeVisual.OnlyDamageAndBlock,
        "known visual-only presentation does not disqualify pure damage");
    string mixedMutation = mutationHelper.Replace("foreach (Example other in cards)", attack + " foreach (Example other in cards)");
    Check(!CallExtractor.Extract(mixedMutation).Single().OnlyDamageAndBlock,
        "removing a field-only helper does not erase evidence of an original additional effect");
    var pureRecord = CatalogCard("Fixture.Pure", mixed.Program, null);
    pureRecord["onlyDamageAndBlock"] = true;
    var pureDocument = new JsonObject { ["schemaVersion"] = 2, ["cards"] = new JsonArray(pureRecord) };
    Check(new HumilityExtractedCatalog(pureDocument.ToJsonString()).Entries["Fixture.Pure"].OnlyDamageAndBlock
        && !catalog.Entries["Fixture.Ready"].OnlyDamageAndBlock,
        "catalog round trip preserves classification; legacy catalog never invents purity");
    pureRecord.Remove("onlyDamageAndBlock");
    rejected = false;
    try { _ = new HumilityExtractedCatalog(pureDocument.ToJsonString()); } catch (FormatException) { rejected = true; }
    Check(rejected, "new catalog requires explicit original-effect classification");
    pureRecord["onlyDamageAndBlock"] = true;
    pureRecord["program"] = new HumilityEffectProgram([]).Save();
    rejected = false;
    try { _ = new HumilityExtractedCatalog(pureDocument.ToJsonString()); } catch (FormatException) { rejected = true; }
    Check(rejected, "empty effect cannot qualify for awakening");
    const string inherited = """
        namespace Fixture;
        abstract class BaseCard {
            protected Task OnPlay(Context context, Play play) => Apply(context, play);
            protected abstract Task Apply(Context context, Play play);
        }
        abstract class Generic<T> : BaseCard {
            protected override Task Apply(Context context, Play play) => ScriptureCmd.Apply<T>(context);
        }
        class EmptyDerived : Generic<Power> { }
        class DamageDerived : BaseCard {
            protected override Task Apply(Context context, Play play) =>
                DamageCmd.Attack(5).FromCard(this, play).Targeting(play.Target).Execute(context);
        }
        class FurtherDerived : DamageDerived {
            protected override Task Apply(Context context, Play play) {
                base.Apply(context, play);
                return CreatureCmd.GainBlock(Owner.Creature, 3, play);
            }
        }
        """;
    var inheritedCards = CallExtractor.Extract(inherited).ToDictionary(c => c.Card);
    Check(inheritedCards.Count == 3 && inheritedCards["Fixture.EmptyDerived"].Program?.Effects.Count == 0,
        "concrete inherited cards indexed; generic and abstract templates are not cards");
    Check(inheritedCards["Fixture.DamageDerived"].Program?.Effects.Single().Amount.Evaluate(default, _ => 0) == 5,
        "inherited OnPlay dispatches abstract helper to concrete override");
    Check(inheritedCards["Fixture.FurtherDerived"].Program?.Effects.Select(e => e.Kind)
        .SequenceEqual(new[] { HumilityEffectKind.Damage, HumilityEffectKind.Block }) == true,
        "most derived virtual helper selected, explicit base call remains lexical");
    var inheritedHook = CallExtractor.Extract(inherited.Replace("class DamageDerived : BaseCard {",
        "class DamageDerived : BaseCard { public override Task AfterCardDrawn() => Task.CompletedTask;"));
    Check(inheritedHook.Single(c => c.Card == "Fixture.FurtherDerived").Program != null
        && !inheritedHook.Single(c => c.Card == "Fixture.FurtherDerived").OnlyDamageAndBlock,
        "inherited original hooks prevent false pure-effect classification");
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
var hierarchy = new SourceHierarchy(methodIndex.Select(m => m.SyntaxTree).Distinct()
    .SelectMany(t => t.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()));
foreach (string path in sources)
{
    string source = sourceTexts[path];
    string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    hashes.Add(new JsonObject { ["file"] = Path.GetFileName(path), ["sha256"] = hash });
    foreach (var card in CallExtractor.Extract(source, methodIndex, hierarchy))
    {
        unsupported |= card.Program == null;
        cards.Add(new JsonObject { ["source"] = Path.GetFileName(path), ["sourceHash"] = hash, ["card"] = card.Card, ["line"] = card.Line,
            ["status"] = card.Program == null ? "unsupported" : "extracted", ["program"] = card.Program?.Save(), ["error"] = card.Error,
            ["onlyDamageAndBlock"] = card.OnlyDamageAndBlock });
    }
}
string json = new JsonObject { ["schemaVersion"] = 2, ["sources"] = hashes, ["cards"] = cards }.ToJsonString(new() { WriteIndented = true });
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
    internal List<HumilityAttackSource> Sources { get; } = [];
    internal List<string> Order { get; } = [];
    public Task Damage(decimal amount, HumilityTarget target, int hits, HumilityAttackSource source)
    {
        Attacks.Add((amount, target, hits));
        Sources.Add(source);
        Order.Add("Damage:" + amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Task.CompletedTask;
    }
    public Task Block(decimal amount, HumilityTarget target, int repeats)
    {
        Order.Add("Block:" + amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Task.CompletedTask;
    }
}
