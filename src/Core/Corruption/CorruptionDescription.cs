namespace MaidenSuccubus.Core.Corruption;

/// <summary>
/// Formats the shared three-band wording used by corruption-sensitive content.
/// Content models provide effects; this helper owns threshold terminology.
/// </summary>
public static class CorruptionDescription
{
    public static string ForThresholds(
        string neutral,
        string corrupt,
        string holy) =>
        $"[purple]-3[/purple] 至 [purple]+3[/purple]：{neutral}\n"
        + $"[purple]+4[/purple] 或更高：{corrupt}\n"
        + $"[purple]-4[/purple] 或更低：{holy}";

    public static string ForThresholds(
        int corruption,
        string neutral,
        string corrupt,
        string holy)
    {
        string current = CorruptionQuery.GetBand(corruption) switch
        {
            CorruptionBand.Corrupt => corrupt,
            CorruptionBand.Holy => holy,
            _ => neutral,
        };

        return $"当前效果：{current}\n"
            + ForThresholds(neutral, corrupt, holy);
    }
}
