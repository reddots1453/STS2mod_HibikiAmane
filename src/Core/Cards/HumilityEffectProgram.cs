using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace MaidenSuccubus.Core.Cards;

// Deliberately contains no conditional, keyword, draw, exhaust or power operation.
// Card adapters construct this program instead of executing the original OnPlay.
internal enum HumilityEffectKind { Damage, Block }
internal enum HumilityAttackSource { Card, Osty }
internal enum HumilityTarget { Selected, Self, AllEnemies, RandomEnemy, AllAllies, CurrentCardTarget, LowestHpEnemy, AllPlayers }
internal enum HumilityValueKind { Constant, Named, EnergyX, StarX, SecondaryX, Add, Multiply, Min, Max }

/// <summary>X values come from CardPlay's resource ledger, NOT the remaining balance or amount spent.</summary>
internal readonly record struct HumilityXValues(int Energy, int Stars, int Secondary);

internal sealed record HumilityValue
{
    internal HumilityValueKind Kind { get; }
    internal decimal Constant { get; }
    internal string? Name { get; }
    internal HumilityValue? Left { get; }
    internal HumilityValue? Right { get; }

    private HumilityValue(HumilityValueKind kind, decimal constant = 0, string? name = null,
        HumilityValue? left = null, HumilityValue? right = null)
    {
        Kind = kind;
        Constant = constant;
        Name = name;
        Left = left;
        Right = right;
    }

    internal static HumilityValue Number(decimal value) => new(HumilityValueKind.Constant, value);
    internal static HumilityValue Named(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new(HumilityValueKind.Named, name: name);
    }
    internal static HumilityValue X(HumilityValueKind kind) => kind switch
    {
        HumilityValueKind.EnergyX or HumilityValueKind.StarX or HumilityValueKind.SecondaryX => new(kind),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
    internal static HumilityValue Binary(HumilityValueKind kind, HumilityValue left, HumilityValue right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (kind is not (HumilityValueKind.Add or HumilityValueKind.Multiply or HumilityValueKind.Min or HumilityValueKind.Max))
            throw new ArgumentOutOfRangeException(nameof(kind));
        return new(kind, left: left, right: right);
    }

    // Named expressions are resolved at each operation, allowing preceding block/damage
    // to affect the next formula. Only the three X values are immutable play snapshots.
    internal decimal Evaluate(HumilityXValues x, Func<string, decimal> resolve) => Kind switch
    {
        HumilityValueKind.Constant => Constant,
        HumilityValueKind.Named => resolve(Name!),
        HumilityValueKind.EnergyX => x.Energy,
        HumilityValueKind.StarX => x.Stars,
        HumilityValueKind.SecondaryX => x.Secondary,
        HumilityValueKind.Add => checked(Left!.Evaluate(x, resolve) + Right!.Evaluate(x, resolve)),
        HumilityValueKind.Multiply => checked(Left!.Evaluate(x, resolve) * Right!.Evaluate(x, resolve)),
        HumilityValueKind.Min => Math.Min(Left!.Evaluate(x, resolve), Right!.Evaluate(x, resolve)),
        HumilityValueKind.Max => Math.Max(Left!.Evaluate(x, resolve), Right!.Evaluate(x, resolve)),
        _ => throw new InvalidOperationException("Unknown humility expression."),
    };

    internal JsonObject Save() => Kind switch
    {
        HumilityValueKind.Constant => new() { ["kind"] = Kind.ToString(), ["value"] = Constant },
        HumilityValueKind.Named => new() { ["kind"] = Kind.ToString(), ["name"] = Name },
        HumilityValueKind.EnergyX or HumilityValueKind.StarX or HumilityValueKind.SecondaryX => new() { ["kind"] = Kind.ToString() },
        _ => new() { ["kind"] = Kind.ToString(), ["left"] = Left!.Save(), ["right"] = Right!.Save() },
    };

    internal static HumilityValue Load(JsonNode? node, int depth = 0)
    {
        // A corrupt save must fail closed, not silently become a different effect.
        if (node is not JsonObject obj || depth > 32)
            throw new FormatException("Invalid humility expression tree.");
        HumilityValueKind kind = HumilityEffectProgram.ReadEnum<HumilityValueKind>(obj, "kind");
        return kind switch
        {
            HumilityValueKind.Constant => Number(obj["value"]?.GetValue<decimal>()
                ?? throw new FormatException("Missing humility number.")),
            HumilityValueKind.Named => Named(obj["name"]?.GetValue<string>()
                ?? throw new FormatException("Missing humility value name.")),
            HumilityValueKind.EnergyX or HumilityValueKind.StarX or HumilityValueKind.SecondaryX => X(kind),
            _ => Binary(kind, Load(obj["left"], depth + 1), Load(obj["right"], depth + 1)),
        };
    }
}

internal sealed record HumilityEffect(HumilityEffectKind Kind, HumilityTarget Target,
    HumilityValue Amount, HumilityValue Repeats, HumilityAttackSource Source = HumilityAttackSource.Card);

/// <summary>
/// The game adapter must use native damage/block commands with the original card as source.
/// Damage repetitions are passed together to native WithHitCount, so AfterAttack fires
/// once for the attack, not once for every hit. Random targeting belongs to native RNG.
/// Cancellation only stops future effects; it never rolls back resolved game commands.
/// </summary>
internal interface IHumilityEffectSink
{
    bool CanContinue { get; }
    Task Damage(decimal baseAmount, HumilityTarget target, int hits, HumilityAttackSource source);
    Task Block(decimal baseAmount, HumilityTarget target, int repetitions);
}

internal sealed class HumilityEffectProgram
{
    internal const int SchemaVersion = 2;
    internal ReadOnlyCollection<HumilityEffect> Effects { get; }
    internal decimal AmountMultiplier { get; }
    internal bool HasDamageOrBlock => Effects.Count > 0;

    internal HumilityEffectProgram(IEnumerable<HumilityEffect> effects, decimal amountMultiplier = 1)
    {
        ArgumentNullException.ThrowIfNull(effects);
        if (amountMultiplier <= 0) throw new ArgumentOutOfRangeException(nameof(amountMultiplier));
        HumilityEffect[] snapshot = effects.ToArray();
        foreach (HumilityEffect effect in snapshot)
        {
            ArgumentNullException.ThrowIfNull(effect);
            ArgumentNullException.ThrowIfNull(effect.Amount);
            ArgumentNullException.ThrowIfNull(effect.Repeats);
            if (!Enum.IsDefined(effect.Kind) || !Enum.IsDefined(effect.Target) || !Enum.IsDefined(effect.Source)
                || effect.Kind == HumilityEffectKind.Block && effect.Source != HumilityAttackSource.Card)
                throw new ArgumentException("Invalid humility operation.", nameof(effects));
        }
        Effects = Array.AsReadOnly(snapshot);
        AmountMultiplier = amountMultiplier;
    }

    // Multiplying the amount, rather than dynamic vars in-place, cannot accidentally
    // multiply a shared variable used for both damage and the number of hits.
    internal HumilityEffectProgram DoubleAmounts() => new(Effects, checked(AmountMultiplier * 2));

    internal async Task Execute(HumilityXValues x, Func<string, decimal> resolve,
        IHumilityEffectSink sink, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        ArgumentNullException.ThrowIfNull(sink);
        if (x.Energy < 0 || x.Stars < 0 || x.Secondary < 0)
            throw new ArgumentOutOfRangeException(nameof(x), "X values must be nonnegative play ledger values.");
        foreach (HumilityEffect effect in Effects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sink.CanContinue) return;
            decimal count = decimal.Truncate(effect.Repeats.Evaluate(x, resolve));
            if (count <= 0) continue;
            int repeats = checked((int)count);
            decimal amount = checked(effect.Amount.Evaluate(x, resolve) * AmountMultiplier);
            // Preserve the native multi-hit attack boundary, not one AttackCommand per hit.
            // Do not round baseAmount here: native commands own modifiers and rounding.
            if (effect.Kind == HumilityEffectKind.Damage)
                await sink.Damage(amount, effect.Target, repeats, effect.Source);
            else
                await sink.Block(amount, effect.Target, repeats);
        }
    }

    internal JsonObject Save()
    {
        JsonArray effects = new();
        foreach (HumilityEffect effect in Effects)
            effects.Add(new JsonObject
            {
                ["kind"] = effect.Kind.ToString(), ["target"] = effect.Target.ToString(),
                ["amount"] = effect.Amount.Save(), ["repeats"] = effect.Repeats.Save(),
                ["source"] = effect.Source.ToString(),
            });
        return new JsonObject { ["version"] = SchemaVersion, ["multiplier"] = AmountMultiplier, ["effects"] = effects };
    }

    internal static HumilityEffectProgram Load(JsonNode? state)
    {
        if (state is not JsonObject obj || obj["version"]?.GetValue<int>() is not (1 or SchemaVersion)
            || obj["effects"] is not JsonArray effects)
            throw new FormatException("Unsupported humility program state.");
        decimal multiplier = obj["multiplier"]?.GetValue<decimal>()
            ?? throw new FormatException("Missing humility multiplier.");
        int version = obj["version"]!.GetValue<int>();
        return new HumilityEffectProgram(effects.Select(node =>
        {
            if (node is not JsonObject effect) throw new FormatException("Invalid humility effect.");
            return new HumilityEffect(ReadEnum<HumilityEffectKind>(effect, "kind"),
                ReadEnum<HumilityTarget>(effect, "target"),
                HumilityValue.Load(effect["amount"]), HumilityValue.Load(effect["repeats"]),
                version == 1 && !effect.ContainsKey("source") ? HumilityAttackSource.Card
                    : ReadEnum<HumilityAttackSource>(effect, "source"));
        }), multiplier);
    }

    internal static T ReadEnum<T>(JsonObject obj, string name) where T : struct, Enum
    {
        string? value = obj[name]?.GetValue<string>();
        if (value == null || !Enum.TryParse(value, out T result) || !Enum.IsDefined(result)
            || !string.Equals(value, result.ToString(), StringComparison.Ordinal))
            throw new FormatException($"Invalid humility {name}.");
        return result;
    }
}
