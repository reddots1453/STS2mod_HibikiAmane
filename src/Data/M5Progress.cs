using STS2RitsuLib.RunData;

namespace MaidenSuccubus.Data;

public sealed class M5ProgressState
{
    public HashSet<int> BlessingOfferedActs { get; set; } = [];
    public bool StartProfileApplied { get; set; }
    public string StartProfileId { get; set; } = "";
    public string FourthRouteQuestId { get; set; } = "";
    public string FourthRouteAlignment { get; set; } = "";
    public int FourthRouteQuestProgress { get; set; }
    public bool FourthRouteQuestCompleted { get; set; }
    public bool FourthRouteRewardPending { get; set; }
    public bool FourthRouteRewardClaimed { get; set; }
    public bool FourthRouteRewardCorruptionApplied { get; set; }
    public int FourthRouteRelicStage { get; set; }
    public bool FourthRouteFragmentPending { get; set; }
    public bool FourthRouteFragmentOffered { get; set; }
    public bool FourthRouteFragmentPurchased { get; set; }
    public bool FourthRouteSacrificeCompleted { get; set; }
    public bool FourthRouteThirdBossDefeated { get; set; }
    public bool FourthRouteEndingChecked { get; set; }
    public bool FourthRouteEndingEligible { get; set; }
    public MaidenSuccubus.Acts.FourthRouteTrialState? FourthRouteTrial { get; set; }
    public MaidenSuccubus.Acts.FourthRouteOpeningState? FourthRouteOpening { get; set; }
}

public static class M5Progress
{
    public static RunSavedData<M5ProgressState> Handle = null!;
}
