using MaidenSuccubus.Acts;
using MaidenSuccubus.Core.Routes;

int checks = 0;
void Check(bool value, string name)
{
    if (!value) throw new InvalidOperationException(name);
    checks++;
}
foreach (var phase in Enum.GetValues<FourthTrialPhase>())
foreach (bool maiden in new[] { false, true })
foreach (bool selected in new[] { false, true })
foreach (int cards in new[] { 0, 2 })
{
    bool active = phase is FourthTrialPhase.First or FourthTrialPhase.Second or FourthTrialPhase.Third;
    bool awakened = phase == FourthTrialPhase.Complete && cards > 0;
    Check(GenerosityOfferingRules.CanOffer(maiden, selected, phase, 4, cards) == (maiden && selected && (active || awakened)),
        "independent visibility truth table");
}
Check(!GenerosityOfferingRules.CanOffer(true, true, FourthTrialPhase.Complete, 2, 2), "non-awakened relic does not remove cards");
foreach (int parent in new[] { 0, 1, 20, 999_999 })
foreach (int child in new[] { 0, 1 })
{
    int encoded = GenerosityOfferingRules.Encode(parent, child);
    Check(encoded == 1_000_000 + parent * 2 + child, "stable wire representation");
    Check(GenerosityOfferingRules.TryDecode(encoded, parent + 1, out int p, out int c) && p == parent && c == child,
        "roundtrip child index");
    Check(!GenerosityOfferingRules.TryDecode(encoded, parent, out _, out _), "out-of-range group rejected");
}
foreach (int invalid in new[] { int.MinValue, -1, 0, 999_999, 3_000_000, int.MaxValue })
    Check(!GenerosityOfferingRules.TryDecode(invalid, int.MaxValue, out _, out _), "foreign or invalid code not interpreted as own child");
foreach (var invalid in new[] { (-1, 0), (1_000_000, 0), (0, -1), (0, 2) })
{
    bool rejected = false;
    try { GenerosityOfferingRules.Encode(invalid.Item1, invalid.Item2); }
    catch (ArgumentOutOfRangeException) { rejected = true; }
    Check(rejected, "invalid child encoding rejected");
}
Console.WriteLine($"PASS {checks} production generosity rule/index assertions.");
