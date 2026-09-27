namespace MaidenSuccubus.Acts;

/// <summary>ACT4-001: an awakened route qualifies, but unfinished content stays closed.</summary>
internal static class FourthActEntryRules
{
    public static bool NormalEntryEnabled => false;

    public static FourthRouteAlignment? AlignmentOf(FourthRouteQuest quest) => quest switch
    {
        FourthRouteQuest.Pride or FourthRouteQuest.Greed or FourthRouteQuest.Lust
            or FourthRouteQuest.Envy or FourthRouteQuest.Gluttony or FourthRouteQuest.Wrath
            or FourthRouteQuest.Sloth => FourthRouteAlignment.Dark,
        FourthRouteQuest.Humility or FourthRouteQuest.Generosity or FourthRouteQuest.Chastity
            or FourthRouteQuest.Benevolence or FourthRouteQuest.Temperance or FourthRouteQuest.Patience
            or FourthRouteQuest.Diligence => FourthRouteAlignment.Light,
        _ => null
    };

    public static bool Qualifies(bool isMaidenRun, bool isAwakened,
        FourthRouteAlignment? alignment, int corruption) =>
        isMaidenRun && isAwakened && alignment switch
        {
            FourthRouteAlignment.Dark => corruption > -3,
            FourthRouteAlignment.Light => corruption < 3,
            _ => false
        };

    public static bool ShouldRecordEnding(bool isMaidenRun, int currentActIndex, bool alreadyChecked) =>
        isMaidenRun && currentActIndex == 2 && !alreadyChecked;

    // Preserve identity/order and every entered act. In particular, an old debug
    // save already inside act four must not be moved into a different room/map.
    public static IReadOnlyList<T> WithoutPendingPlaceholder<T>(IReadOnlyList<T> acts,
        int currentActIndex, Func<T, bool> isPlaceholder)
    {
        if (currentActIndex < 0 || currentActIndex >= acts.Count) return acts;
        var retained = acts.Where((act, index) => index <= currentActIndex || !isPlaceholder(act)).ToArray();
        return retained.Length == acts.Count ? acts : retained;
    }
}
