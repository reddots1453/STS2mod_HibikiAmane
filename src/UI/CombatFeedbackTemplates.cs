using System.Text.Json;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Corruption;

namespace MaidenSuccubus.UI;

/// <summary>Per-combat presentation configuration; never uses gameplay RNG.</summary>
internal sealed class FeedbackTemplates
{
    private const int SlotsPerBand = 3;
    private readonly Dictionary<(string Key, CorruptionBand Band, ControlType? Type), int> _next = [];

    internal bool Enabled { get; private set; } = true;
    private Dictionary<string, Interaction> Texts { get; } = new(StringComparer.Ordinal);

    internal FeedbackTemplates()
    {
        Texts["damage_received"] = Interaction.FromSingle("受到{enemy}的伤害：{amount}");
        foreach (string key in new[]
        {
            "desire_attack_received", "desire_increased", "desire_reached_8",
            "desire_reached_max", "armor_damage_received", "control_intent_received",
            "escape_incomplete", "control_released", "invasion_intent_received",
        }) Texts[key] = new Interaction();
    }

    internal string? Next(string key, CorruptionBand band, ControlType? bindingType)
    {
        if (!Enabled || !Texts.TryGetValue(key, out Interaction? interaction)) return null;
        string[] slots = interaction.General.For(band);
        ControlType? selectedType = null;
        if (bindingType is { } type && interaction.ByControl.TryGetValue(type, out Routes? routes))
        {
            string[] specific = routes.For(band);
            if (specific.Any(text => !string.IsNullOrWhiteSpace(text)))
            {
                slots = specific;
                selectedType = type;
            }
        }

        var cursorKey = (key, band, selectedType);
        int cursor = _next.GetValueOrDefault(cursorKey);
        for (int offset = 0; offset < slots.Length; offset++)
        {
            int index = (cursor + offset) % slots.Length;
            if (string.IsNullOrWhiteSpace(slots[index])) continue;
            _next[cursorKey] = (index + 1) % slots.Length;
            return slots[index];
        }
        return null;
    }

    internal static FeedbackTemplates Load()
    {
        try
        {
            string? directory = Path.GetDirectoryName(typeof(MaidenSuccubusMod).Assembly.Location);
            if (string.IsNullOrEmpty(directory)) return new();
            string path = Path.Combine(directory, "combat_feedback.json");
            if (!File.Exists(path)) return new();
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = document.RootElement;
            var loaded = new FeedbackTemplates();
            if (TryProperty(root, "enabled", out JsonElement enabled)
                && enabled.ValueKind is JsonValueKind.True or JsonValueKind.False)
                loaded.Enabled = enabled.GetBoolean();
            if (!TryProperty(root, "texts", out JsonElement texts)
                || texts.ValueKind != JsonValueKind.Object)
            {
                if (!loaded.Enabled) return loaded;
                throw new InvalidDataException("Missing texts dictionary");
            }
            // Keep an omitted interaction silent, matching the previous format.
            loaded.Texts.Clear();
            foreach (JsonProperty property in texts.EnumerateObject())
            {
                try
                {
                    loaded.Texts[property.Name] = Interaction.Parse(property.Value);
                }
                catch (Exception ex)
                {
                    // One bad interaction must not erase the remaining authored text.
                    MaidenSuccubusMod.Logger.Warn(
                        $"[CombatTextFeedback] Template {property.Name}: {ex.Message}");
                }
            }
            return loaded;
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Warn("[CombatTextFeedback] Template file: " + ex.Message);
            return new();
        }
    }

    private static bool TryProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
            foreach (JsonProperty property in element.EnumerateObject())
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
        value = default;
        return false;
    }

    private static bool TryControlType(string key, out ControlType type)
    {
        switch (key.ToLowerInvariant())
        {
            case "attack": type = ControlType.Attack; return true;
            case "skill": type = ControlType.Skill; return true;
            case "power": type = ControlType.Power; return true;
            default: type = default; return false;
        }
    }

    private sealed class Interaction
    {
        internal Routes General { get; private set; } = new();
        internal Dictionary<ControlType, Routes> ByControl { get; } = [];

        internal static Interaction FromSingle(string text) => new()
        {
            General = Routes.FromSingle(text),
        };

        internal static Interaction Parse(JsonElement element)
        {
            // Legacy strings remain usable without rewriting the user's file.
            if (element.ValueKind == JsonValueKind.String)
                return FromSingle(element.GetString() ?? "");
            if (element.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
                throw new InvalidDataException("Expected route groups or a legacy text string");
            var interaction = new Interaction { General = Routes.Parse(element) };
            if (TryProperty(element, "by_control", out JsonElement overrides)
                && overrides.ValueKind == JsonValueKind.Object)
                foreach (JsonProperty property in overrides.EnumerateObject())
                    if (TryControlType(property.Name, out ControlType type))
                    {
                        try { interaction.ByControl[type] = Routes.Parse(property.Value); }
                        catch (Exception ex)
                        {
                            MaidenSuccubusMod.Logger.Warn(
                                $"[CombatTextFeedback] Control group {property.Name}: {ex.Message}");
                        }
                    }
            return interaction;
        }
    }

    private sealed class Routes
    {
        private string[] Holy { get; init; } = ["", "", ""];
        private string[] Neutral { get; init; } = ["", "", ""];
        private string[] Corrupt { get; init; } = ["", "", ""];

        internal string[] For(CorruptionBand band) => band switch
        {
            CorruptionBand.Holy => Holy,
            CorruptionBand.Corrupt => Corrupt,
            _ => Neutral,
        };

        internal static Routes FromSingle(string text) => new()
        {
            Holy = [text, "", ""], Neutral = [text, "", ""], Corrupt = [text, "", ""],
        };

        internal static Routes Parse(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return FromSingle(element.GetString() ?? "");
            if (element.ValueKind == JsonValueKind.Array)
                return new() { Holy = ReadSlots(element), Neutral = ReadSlots(element), Corrupt = ReadSlots(element) };
            if (element.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Expected holy/neutral/corrupt groups");
            return new()
            {
                Holy = TryProperty(element, "holy", out JsonElement holy) ? ReadSlots(holy) : ["", "", ""],
                Neutral = TryProperty(element, "neutral", out JsonElement neutral) ? ReadSlots(neutral) : ["", "", ""],
                Corrupt = TryProperty(element, "corrupt", out JsonElement corrupt) ? ReadSlots(corrupt) : ["", "", ""],
            };
        }

        private static string[] ReadSlots(JsonElement element)
        {
            string[] slots = ["", "", ""];
            if (element.ValueKind == JsonValueKind.String)
                slots[0] = element.GetString() ?? "";
            else if (element.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (JsonElement item in element.EnumerateArray())
                {
                    if (index >= SlotsPerBand) break;
                    slots[index++] = item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : "";
                }
            }
            return slots;
        }
    }
}
