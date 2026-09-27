using System.Text.Json;
using MaidenSuccubus.Enchantments;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

int assertions = 0;
void Check(bool condition, string name)
{
    assertions++;
    if (!condition) throw new Exception(name);
}

var card = new SerializableCard
{
    Id = new ModelId("CARD", "LIGHT_WINGS"), CurrentUpgradeLevel = 1, FloorAddedToDeck = 17,
    Enchantment = new() { Id = new ModelId("ENCHANTMENT", "SHARP"), Amount = 3 }
};
var properties = new SavedProperties
{
    ints = [new("SavedCount", 27)], bools = [new("Enabled", true), new("Disabled", false)],
    strings = [new("Text", "附魔：华彩。\n引号\"反斜杠\\")],
    intArrays = [new("Counts", [1, 0, -4, 256])],
    modelIds = [new("Source", new ModelId("RELIC", "WRATH"))],
    cards = [new("SourceCard", card)], cardArrays = [new("SourceCards", [card, card])]
};
SerializableEnchantment[] original =
[
    new() { Id = new ModelId("ENCHANTMENT", "SHARP"), Amount = 2, Props = properties },
    new() { Id = new ModelId("ENCHANTMENT", "GLAM"), Amount = 1 },
    new() { Id = new ModelId("ENCHANTMENT", "GLAM"), Amount = 1 }
];
string json = LayeredEnchantmentSerialization.Serialize(original);
var loaded = LayeredEnchantmentSerialization.Deserialize(json);
Check(loaded.Length == 3, "all children survive");
for (int i = 0; i < original.Length; i++)
{
    Check(loaded[i].Id == original[i].Id, "ordered IDs round-trip");
    Check(loaded[i].Amount == original[i].Amount, "amount round-trip");
    Check(!ReferenceEquals(loaded[i], original[i]), "fresh child DTO");
}
Check(!ReferenceEquals(loaded[1], loaded[2]), "same-name layers remain distinct");
Check(loaded[1].Props == null, "null child props retained");
var actual = loaded[0].Props!;
Check(actual.ints![0].name == "SavedCount" && actual.ints[0].value == 27, "integer fields retained");
Check(actual.bools![0].value && !actual.bools[1].value, "both boolean values retained");
Check(actual.strings![0].value == properties.strings[0].value, "Unicode and escapes retained");
Check(actual.intArrays![0].value.SequenceEqual(properties.intArrays[0].value), "array fields retained");
Check(actual.modelIds![0].value == properties.modelIds[0].value, "nested ModelId retained");
var actualCard = actual.cards![0].value;
Check(actualCard.Id == card.Id, "nested card ID");
Check(actualCard.CurrentUpgradeLevel == 1, "nested card upgrade");
Check(actualCard.FloorAddedToDeck == 17, "nested card floor");
Check(actualCard.Enchantment!.Amount == 3, "nested card enchantment");
Check(actual.cardArrays![0].value.Length == 2, "nested card array length");
Check(actual.cardArrays[0].value.All(c => c.Id == card.Id), "nested card array values");
Check(!ReferenceEquals(actual.cardArrays[0].value[0], actual.cardArrays[0].value[1]), "nested copies independent");
Check(LayeredEnchantmentSerialization.Serialize(loaded) == json, "deterministic payload after load");
actual.intArrays[0].value[0] = 99;
Check(properties.intArrays[0].value[0] == 1, "loaded arrays do not alias original");
loaded[1].Amount = 7;
Check(loaded[2].Amount == 1 && original[1].Amount == 1, "same-name mutation isolated");
Check(LayeredEnchantmentSerialization.Deserialize("[]").Length == 0, "empty container supported");
foreach (string invalid in new[] { "null", "[null]", "[{}]", "{}", "not JSON" })
{
    bool rejected = false;
    try { LayeredEnchantmentSerialization.Deserialize(invalid); }
    catch (JsonException) { rejected = true; }
    Check(rejected, "reject invalid payload: " + invalid);
}
Console.WriteLine($"PASS: {assertions} production codec assertions with installed game DTOs; no engine/gameplay execution.");
