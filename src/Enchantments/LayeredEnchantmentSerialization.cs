using System.Text.Json;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MaidenSuccubus.Enchantments;

/// <summary>Native save DTOs only: no live models, card owners or Godot nodes.</summary>
internal static class LayeredEnchantmentSerialization
{
    // SavedProperties and its SavedProperty<T> entries expose fields, not properties.
    private static readonly JsonSerializerOptions Options = new() { IncludeFields = true };

    internal static string Serialize(IEnumerable<SerializableEnchantment> layers) =>
        JsonSerializer.Serialize(layers.ToArray(), Options);

    internal static SerializableEnchantment[] Deserialize(string value)
    {
        var layers = JsonSerializer.Deserialize<SerializableEnchantment[]>(value, Options)
            ?? throw new JsonException("Layered enchantments require an array.");
        if (layers.Any(layer => layer == null || layer.Id == null))
            throw new JsonException("A layered enchantment is missing its model ID.");
        return layers;
    }
}
