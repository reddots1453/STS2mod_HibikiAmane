using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace MaidenSuccubus.Core.Cards;

internal sealed record HumilityExtractionEntry(HumilityEffectProgram? Program, string? Error);

/// <summary>Generated data, never a hand-maintained type whitelist.</summary>
internal sealed class HumilityExtractedCatalog
{
    internal IReadOnlyDictionary<string, HumilityExtractionEntry> Entries { get; }

    internal HumilityExtractedCatalog(string json)
    {
        if (JsonNode.Parse(json) is not JsonObject root || root["schemaVersion"]?.GetValue<int>() != 1
            || root["cards"] is not JsonArray cards)
            throw new FormatException("Invalid extracted humility catalog.");
        var entries = new Dictionary<string, HumilityExtractionEntry>(StringComparer.Ordinal);
        foreach (JsonNode? node in cards)
        {
            if (node is not JsonObject card || card["card"]?.GetValue<string>() is not { Length: > 0 } name)
                throw new FormatException("Missing extracted card identity.");
            string? status = card["status"]?.GetValue<string>();
            string? error = card["error"]?.GetValue<string>();
            HumilityEffectProgram? program;
            if (status == "extracted" && string.IsNullOrEmpty(error)) program = HumilityEffectProgram.Load(card["program"]);
            else if (status == "unsupported" && card["program"] == null && !string.IsNullOrWhiteSpace(error)) program = null;
            else throw new FormatException("Inconsistent extracted card status: " + name);
            if (!entries.TryAdd(name, new(program, error))) throw new FormatException("Duplicate extracted card identity: " + name);
        }
        Entries = new ReadOnlyDictionary<string, HumilityExtractionEntry>(entries);
    }
}
