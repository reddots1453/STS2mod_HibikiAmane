using STS2RitsuLib.RunData;

namespace MaidenSuccubus.Data;

public sealed class M5ProgressState
{
    public HashSet<int> BlessingOfferedActs { get; set; } = [];
    public bool StartProfileApplied { get; set; }
    public string StartProfileId { get; set; } = "";
}

public static class M5Progress
{
    public static RunSavedData<M5ProgressState> Handle = null!;
}
