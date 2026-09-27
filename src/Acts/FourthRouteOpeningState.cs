namespace MaidenSuccubus.Acts;

/// <summary>Saved opening receipt; unrelated to trial counters or reward claims.</summary>
public sealed class FourthRouteOpeningState
{
    public FourthRouteQuest? Dark { get; set; }
    public FourthRouteQuest? Light { get; set; }
    public FourthRouteQuest? Chosen { get; set; }
    public bool Completed { get; set; }

    public bool HasOffers => Dark is { } dark && Light is { } light
        && FourthActEntryRules.AlignmentOf(dark) == FourthRouteAlignment.Dark
        && FourthActEntryRules.AlignmentOf(light) == FourthRouteAlignment.Light;

    public bool Offer(FourthRouteQuest dark, FourthRouteQuest light)
    {
        if (HasOffers || Chosen != null || Completed
            || FourthActEntryRules.AlignmentOf(dark) != FourthRouteAlignment.Dark
            || FourthActEntryRules.AlignmentOf(light) != FourthRouteAlignment.Light) return false;
        Dark = dark;
        Light = light;
        return true;
    }

    public bool Choose(FourthRouteQuest quest)
    {
        if (!HasOffers || Chosen != null || Completed || (quest != Dark && quest != Light)) return false;
        Chosen = quest;
        return true;
    }

    public bool Finish()
    {
        if (!HasOffers || Completed || Chosen is not { } chosen || (chosen != Dark && chosen != Light)) return false;
        Completed = true;
        return true;
    }

    // Legacy runs with a chosen route have already passed the old opening.
    public static bool NeedsOpening(bool hasQuest, FourthRouteOpeningState? state) =>
        state is null ? !hasQuest : !state.Completed && (!hasQuest || state.Chosen != null);
}
