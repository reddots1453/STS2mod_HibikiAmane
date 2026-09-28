namespace MaidenSuccubus.Acts;

/// <summary>Immutable UI receipt, not an independent reward or a saved duplicate of trial state.</summary>
public readonly record struct FourthRouteRewardOffer(FourthRouteQuest Quest, FourthTrialPhase Phase)
{
    public int Stage => FourthRouteTrialRules.RewardStage(Phase);
    public int Trial => FourthRouteTrialRules.Number(Phase);
    public bool IsValid => Enum.IsDefined(Quest) && FourthRouteTrialRules.Pending(Phase);
    public bool Matches(FourthRouteQuest quest, FourthTrialPhase phase) => IsValid && Quest == quest && Phase == phase;

    public static bool CanPresent(bool localMaiden, bool singlePlayer, bool testMode, bool alive,
        bool currentScene, bool combatActive, bool modalOpen, bool overlayOpen, bool transitioning,
        bool openingPending, bool executorBusy, bool victoryBoundary) =>
        localMaiden && singlePlayer && !testMode && alive && currentScene && !combatActive
        && !modalOpen && !overlayOpen && !transitioning && !openingPending && (!executorBusy || victoryBoundary);
}
