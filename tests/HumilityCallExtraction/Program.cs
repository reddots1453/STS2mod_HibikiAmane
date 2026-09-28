using System.Text.Json.Nodes;
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
    return 0;
}
if (args.Length == 0) { Console.Error.WriteLine("Usage: HumilityCallExtraction --self-test | source.cs [...]"); return 1; }
var cards = new JsonArray();
bool unsupported = false;
foreach (string path in args)
foreach (var card in CallExtractor.Extract(File.ReadAllText(path)))
{
    unsupported |= card.Program == null;
    cards.Add(new JsonObject { ["source"] = Path.GetFullPath(path), ["card"] = card.Card, ["line"] = card.Line,
        ["status"] = card.Program == null ? "unsupported" : "extracted", ["program"] = card.Program?.Save(), ["error"] = card.Error });
}
Console.WriteLine(new JsonObject { ["schemaVersion"] = 1, ["cards"] = cards }.ToJsonString(new() { WriteIndented = true }));
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
