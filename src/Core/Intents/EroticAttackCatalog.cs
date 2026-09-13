using System.Reflection;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Commands;

namespace MaidenSuccubus.Core.Intents;

public enum EroticIntentKind
{
    Desire,
    Control,
    Invasion,
}

public sealed class EroticMonsterSpec
{
    public required string MonsterId { get; init; }
    public bool Steadfast { get; set; }
    public EroticIntentKind Preferred { get; set; }
    public DesireIntentSpec? Desire { get; set; }
    public ControlIntentSpec? Control { get; set; }
    public InvasionIntentSpec? Invasion { get; set; }
    public int DesireThreshold { get; set; }
    public int ControlThreshold { get; set; }
    public int InvasionThreshold { get; set; }
    public bool StunAfterEscape { get; set; }
    public string? RecoveryMoveId { get; set; }
}

/// <summary>
/// The two reviewed monster tables are embedded as the authoritative data
/// source. Parsing them here keeps the runtime roster synchronized with the
/// design catalogue and makes omissions fail loudly during registration.
/// </summary>
public static partial class EroticAttackCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, EroticMonsterSpec>>
        Specs = new(Build);

    public static EroticMonsterSpec? Get(MonsterModel monster) =>
        Specs.Value.GetValueOrDefault(monster.Id.Entry);

    public static IReadOnlyDictionary<string, EroticMonsterSpec> All => Specs.Value;

    public static void Validate() => _ = Specs.Value;

    public static MoveState? GetRecoveryMove(MonsterModel monster)
    {
        EroticMonsterSpec? spec = Get(monster);
        if (spec?.RecoveryMoveId == null)
        {
            return null;
        }
        if (monster.MoveStateMachine?.States.GetValueOrDefault(
                spec.RecoveryMoveId) is MoveState move)
        {
            return move;
        }
        throw new InvalidDataException(
            $"Recovery move {spec.RecoveryMoveId} does not exist on "
            + $"{monster.Id.Entry}.");
    }

    private static IReadOnlyDictionary<string, EroticMonsterSpec> Build()
    {
        string assignments = ReadEmbedded("EROTIC_ATTACK_ASSIGNMENTS.md");
        string intents = ReadEmbedded("EROTIC_ATTACK_INTENTS.md");
        var result = new Dictionary<string, EroticMonsterSpec>(
            StringComparer.OrdinalIgnoreCase);

        ParseAssignments(assignments, result);
        ParseThresholds(assignments, result);
        ParseIntentDetails(intents, result);
        ParseRecovery(assignments, result);

        if (result.Count != 102)
        {
            throw new InvalidDataException(
                $"Erotic intent catalogue expected 102 monsters, got {result.Count}.");
        }

        foreach (EroticMonsterSpec spec in result.Values)
        {
            int behaviorCount = (spec.Desire != null ? 1 : 0)
                + (spec.Control != null ? 1 : 0)
                + (spec.Invasion != null ? 1 : 0);
            if (spec.Steadfast == (behaviorCount > 0))
            {
                throw new InvalidDataException(
                    $"Erotic intent catalogue must assign either behaviors or "
                    + $"Steadfast, exclusively: {spec.MonsterId}.");
            }
            if (!spec.Steadfast
                && !HasPreferredBehavior(spec))
            {
                throw new InvalidDataException(
                    $"Preferred erotic intent is unavailable for {spec.MonsterId}.");
            }
            if (spec.Desire is { } desire)
            {
                ValidateCap(spec, EroticIntentKind.Desire, desire.MaxUsesPerCombat);
                ValidateThreshold(spec, EroticIntentKind.Desire, spec.DesireThreshold);
                EroticEffectCmd.Validate(desire.EffectText, EroticIntentKind.Desire);
            }
            if (spec.Control is { } control)
            {
                ValidateCap(spec, EroticIntentKind.Control, control.MaxUsesPerCombat);
                ValidateThreshold(spec, EroticIntentKind.Control, spec.ControlThreshold);
                if (control.BlockRequired <= 0 || control.EscapeRequired <= 0)
                {
                    throw new InvalidDataException(
                        $"Control intent has invalid values: {spec.MonsterId}.");
                }
                if (spec.StunAfterEscape == (spec.RecoveryMoveId != null))
                {
                    throw new InvalidDataException(
                        $"Control intent must have exactly one escape recovery "
                        + $"classification: {spec.MonsterId}.");
                }
                EroticEffectCmd.Validate(control.EffectText, EroticIntentKind.Control);
            }
            else if (spec.StunAfterEscape || spec.RecoveryMoveId != null)
            {
                throw new InvalidDataException(
                    $"Non-control monster has escape recovery data: {spec.MonsterId}.");
            }
            if (spec.Invasion is { } invasion)
            {
                ValidateCap(spec, EroticIntentKind.Invasion, invasion.MaxUsesPerCombat);
                ValidateThreshold(spec, EroticIntentKind.Invasion, spec.InvasionThreshold);
                if (string.IsNullOrWhiteSpace(invasion.CurseName))
                {
                    throw new InvalidDataException(
                        $"Invasion intent has no curse: {spec.MonsterId}.");
                }
                if (!InvasionCmd.IsRegisteredCurseName(invasion.CurseName))
                {
                    throw new InvalidDataException(
                        $"Invasion intent uses an unregistered curse "
                        + $"'{invasion.CurseName}': {spec.MonsterId}.");
                }
                EroticEffectCmd.Validate(invasion.EffectText, EroticIntentKind.Invasion);
            }
        }
        int steadfastCount = result.Values.Count(spec => spec.Steadfast);
        if (steadfastCount != 18)
        {
            throw new InvalidDataException(
                $"Erotic intent catalogue expected 18 Steadfast monsters, got {steadfastCount}.");
        }
        return result;
    }

    private static bool HasPreferredBehavior(EroticMonsterSpec spec) =>
        spec.Preferred switch
        {
            EroticIntentKind.Desire => spec.Desire != null,
            EroticIntentKind.Control => spec.Control != null,
            EroticIntentKind.Invasion => spec.Invasion != null,
            _ => false,
        };

    private static void ValidateCap(
        EroticMonsterSpec spec,
        EroticIntentKind kind,
        int cap)
    {
        if (cap is < 1 or > 4)
        {
            throw new InvalidDataException(
                $"{kind} cap must be 1..4 for {spec.MonsterId}, got {cap}.");
        }
    }

    private static void ValidateThreshold(
        EroticMonsterSpec spec,
        EroticIntentKind kind,
        int threshold)
    {
        if (threshold <= 0 || threshold % 5 != 0)
        {
            throw new InvalidDataException(
                $"{kind} threshold must be a positive multiple of 5 for "
                + $"{spec.MonsterId}, got {threshold}.");
        }
    }

    private static void ParseAssignments(
        string markdown,
        IDictionary<string, EroticMonsterSpec> result)
    {
        foreach (string line in Lines(markdown))
        {
            string[] cells = Cells(line);
            if (cells.Length == 6 && cells[2].Contains('`'))
            {
                string[] ids = IdRegex().Matches(cells[2])
                    .Select(match => match.Groups[1].Value).ToArray();
                if (ids.Length == 0) continue;
                foreach (string id in ids)
                {
                    EroticMonsterSpec spec = GetOrAdd(result, id);
                    int a = ParseCap(cells[3]);
                    int b = ParseCap(cells[4]);
                    int i = ParseCap(cells[5]);
                    spec.Desire = a > 0
                        ? new DesireIntentSpec(0, a) : null;
                    spec.Control = b > 0
                        ? new ControlIntentSpec(0, ControlType.Skill, 0,
                            MaxUsesPerCombat: b) : null;
                    spec.Invasion = i > 0
                        ? new InvasionIntentSpec(0, MaxUsesPerCombat: i) : null;
                }
            }
            else if (cells.Length == 5 && cells[1].Contains('`'))
            {
                string preferred = cells[3].Trim();
                foreach (Match match in IdRegex().Matches(cells[1]))
                {
                    EroticMonsterSpec spec = GetOrAdd(
                        result, match.Groups[1].Value);
                    if (preferred == "S")
                    {
                        spec.Steadfast = true;
                        spec.Desire = null;
                        spec.Control = null;
                        spec.Invasion = null;
                    }
                    else if (TryKind(preferred, out EroticIntentKind kind))
                    {
                        spec.Preferred = kind;
                    }
                }
            }
        }
    }

    private static void ParseIntentDetails(
        string markdown,
        IDictionary<string, EroticMonsterSpec> result)
    {
        foreach (string line in Lines(markdown))
        {
            string[] cells = Cells(line);
            if (cells.Length != 4) continue;
            Match idMatch = SingleIdRegex().Match(cells[0]);
            if (!idMatch.Success) continue;
            EroticMonsterSpec spec = GetOrAdd(result, idMatch.Groups[1].Value);
            if (cells[3].Contains("意志坚定", StringComparison.Ordinal))
            {
                spec.Steadfast = true;
                spec.Desire = null;
                spec.Control = null;
                spec.Invasion = null;
                continue;
            }

            int aCap = spec.Desire?.MaxUsesPerCombat ?? 0;
            int bCap = spec.Control?.MaxUsesPerCombat ?? 0;
            int iCap = spec.Invasion?.MaxUsesPerCombat ?? 0;
            if (aCap > 0 && cells[1] != "—")
            {
                spec.Desire = ParseDesire(cells[1], aCap);
            }
            if (bCap > 0 && cells[2] != "—")
            {
                spec.Control = ParseControl(cells[2], bCap);
            }
            if (iCap > 0 && cells[3] != "—")
            {
                spec.Invasion = ParseInvasion(cells[3], iCap);
            }
        }
    }

    private static void ParseThresholds(
        string markdown,
        IDictionary<string, EroticMonsterSpec> result)
    {
        foreach (string line in Lines(markdown))
        {
            string[] cells = Cells(line);
            if (cells.Length != 5)
            {
                continue;
            }
            Match idMatch = SingleIdRegex().Match(cells[0]);
            if (!idMatch.Success
                || !result.TryGetValue(idMatch.Groups[1].Value, out EroticMonsterSpec? spec))
            {
                continue;
            }

            spec.DesireThreshold = ParseCap(cells[1]);
            spec.ControlThreshold = ParseCap(cells[2]);
            spec.InvasionThreshold = ParseCap(cells[3]);
        }
    }

    private static DesireIntentSpec ParseDesire(string cell, int cap)
    {
        (string name, string effect) = SplitIntentCell(cell);
        int desire = Number(effect, @"欲望增加(\d+)");
        int damage = Number(effect, @"造成(\d+)点伤害");
        int hits = Number(effect, @"伤害(\d+)次", 1);
        return new DesireIntentSpec(
            desire, cap, Damage: damage, Hits: hits,
            DisplayName: name, EffectText: effect);
    }

    private static ControlIntentSpec ParseControl(string cell, int cap)
    {
        (string name, string effect) = SplitIntentCell(cell);
        int block = Number(effect, @"需要(\d+)点格挡");
        int escape = Number(effect, @"挣脱值(\d+)");
        ControlType type = effect.Contains("攻击牌", StringComparison.Ordinal)
            ? ControlType.Attack
            : effect.Contains("能力牌", StringComparison.Ordinal)
                ? ControlType.Power
                : ControlType.Skill;
        return new ControlIntentSpec(
            block, type, escape, MaxUsesPerCombat: cap,
            DisplayName: name, EffectText: effect);
    }

    private static InvasionIntentSpec ParseInvasion(string cell, int cap)
    {
        (string name, string effect) = SplitIntentCell(cell);
        int damage = Number(effect, @"造成(\d+)点伤害");
        Match curse = Regex.Match(effect, "将\\d+张[“\"]([^”\"]+)[”\"]加入牌组");
        return new InvasionIntentSpec(
            damage,
            MaxUsesPerCombat: cap,
            CurseName: curse.Success ? curse.Groups[1].Value : "精液",
            DisplayName: name,
            EffectText: effect);
    }

    private static void ParseRecovery(
        string markdown,
        IDictionary<string, EroticMonsterSpec> result)
    {
        bool weakTable = false;
        bool strongTable = false;
        foreach (string line in Lines(markdown))
        {
            if (line.StartsWith("### 弱怪", StringComparison.Ordinal))
            {
                weakTable = true;
                strongTable = false;
                continue;
            }
            if (line.StartsWith("### 强大怪物", StringComparison.Ordinal))
            {
                weakTable = false;
                strongTable = true;
                continue;
            }
            if (line.StartsWith("## 3.", StringComparison.Ordinal))
            {
                weakTable = false;
                strongTable = false;
            }
            string[] cells = Cells(line);
            if (weakTable && cells.Length == 2)
            {
                foreach (Match id in IdRegex().Matches(cells[1]))
                {
                    GetOrAdd(result, id.Groups[1].Value).StunAfterEscape = true;
                }
            }
            else if (strongTable && cells.Length == 4)
            {
                Match id = SingleIdRegex().Match(cells[1]);
                Match move = SingleMoveIdRegex().Match(cells[2]);
                if (id.Success && move.Success)
                {
                    GetOrAdd(result, id.Groups[1].Value).RecoveryMoveId =
                        move.Groups[1].Value;
                }
            }
        }
    }

    private static EroticMonsterSpec GetOrAdd(
        IDictionary<string, EroticMonsterSpec> result,
        string id)
    {
        if (!result.TryGetValue(id, out EroticMonsterSpec? spec))
        {
            spec = new EroticMonsterSpec { MonsterId = id };
            result[id] = spec;
        }
        return spec;
    }

    private static string ReadEmbedded(string suffix)
    {
        Assembly assembly = typeof(EroticAttackCatalog).Assembly;
        string resource = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(suffix, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidDataException($"Missing embedded {suffix}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static IEnumerable<string> Lines(string value) =>
        value.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');

    private static string[] Cells(string line) =>
        !line.StartsWith('|') ? [] : line.Trim('|').Split('|')
            .Select(cell => cell.Trim()).ToArray();

    private static int ParseCap(string value) =>
        int.TryParse(value, out int parsed) ? parsed : 0;

    private static bool TryKind(string value, out EroticIntentKind kind)
    {
        kind = value switch
        {
            "B" => EroticIntentKind.Control,
            "I" => EroticIntentKind.Invasion,
            _ => EroticIntentKind.Desire,
        };
        return value is "A" or "B" or "I";
    }

    private static (string Name, string Effect) SplitIntentCell(string cell)
    {
        Match match = Regex.Match(cell, @"\*\*(.+?)\*\*[：:](.+)");
        return match.Success
            ? (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim())
            : ("色情攻击", cell);
    }

    private static int Number(string text, string pattern, int fallback = 0)
    {
        Match match = Regex.Match(text, pattern);
        return match.Success && int.TryParse(match.Groups[1].Value, out int value)
            ? value : fallback;
    }

    [GeneratedRegex(@"`([A-Z0-9_]+)`")]
    private static partial Regex IdRegex();

    [GeneratedRegex(@"^`([A-Z0-9_]+)`$")]
    private static partial Regex SingleIdRegex();

    [GeneratedRegex(@"^`([A-Za-z0-9_]+)`$")]
    private static partial Regex SingleMoveIdRegex();
}
