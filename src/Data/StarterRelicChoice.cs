using STS2RitsuLib.RunData;

namespace MaidenSuccubus.Data;

public enum StarterRelicKind { Omnipotent, Hero }

public sealed class StarterRelicChoiceState
{
    public StarterRelicKind Kind { get; set; } = StarterRelicKind.Omnipotent;
    public bool Applied { get; set; }
}

public static class StarterRelicChoice
{
    public static PlayerRunSavedData<StarterRelicChoiceState> Handle = null!;
    public static StarterRelicKind Normalize(StarterRelicKind kind) =>
        kind == StarterRelicKind.Hero ? kind : StarterRelicKind.Omnipotent;
    public static StarterRelicKind Next(StarterRelicKind kind) =>
        Normalize(kind) == StarterRelicKind.Hero ? StarterRelicKind.Omnipotent : StarterRelicKind.Hero;
}
