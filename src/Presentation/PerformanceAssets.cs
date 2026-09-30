namespace MaidenSuccubus.Presentation;

public enum PerformanceAudioCue
{
    TransformationStart,
    TransformationComplete,
    EroticAttackTouch,
    EroticAttackTentacle,
    RestraintChain,
    RestraintHold,
    InvasionStart,
    InvasionFinish,
    ClothesTear,
    Climax,
}

public enum PerformanceLoopCue
{
    Breath,
}

internal static class PerformanceAssets
{
    private const string Audio = "res://MaidenSuccubus/audio/";
    private const string Cutscenes = "res://MaidenSuccubus/images/cutscenes/";

    public static string AudioPath(PerformanceAudioCue cue) => cue switch
    {
        PerformanceAudioCue.TransformationStart => Audio + "magic/transformation_start.ogg",
        PerformanceAudioCue.TransformationComplete => Audio + "magic/transformation_complete.ogg",
        PerformanceAudioCue.EroticAttackTouch => Audio + "erotic_intents/erotic_attack_touch.ogg",
        PerformanceAudioCue.EroticAttackTentacle => Audio + "erotic_intents/erotic_attack_tentacle.ogg",
        PerformanceAudioCue.RestraintChain => Audio + "erotic_intents/restraint_chain.ogg",
        PerformanceAudioCue.RestraintHold => Audio + "erotic_intents/restraint_hold.ogg",
        PerformanceAudioCue.InvasionStart => Audio + "erotic_intents/invasion_start.ogg",
        PerformanceAudioCue.InvasionFinish => Audio + "erotic_intents/invasion_finish.ogg",
        PerformanceAudioCue.ClothesTear => Audio + "erotic_intents/clothes_tear.ogg",
        PerformanceAudioCue.Climax => Audio + "rest_site/climax.ogg",
        _ => throw new ArgumentOutOfRangeException(nameof(cue), cue, null),
    };

    public static string AudioPath(PerformanceLoopCue cue) => cue switch
    {
        PerformanceLoopCue.Breath => Audio + "erotic_intents/breath.ogg",
        _ => throw new ArgumentOutOfRangeException(nameof(cue), cue, null),
    };

    public static bool IsAdult(PerformanceAudioCue cue) => cue is
        PerformanceAudioCue.EroticAttackTouch
        or PerformanceAudioCue.EroticAttackTentacle
        or PerformanceAudioCue.RestraintChain
        or PerformanceAudioCue.RestraintHold
        or PerformanceAudioCue.InvasionStart
        or PerformanceAudioCue.InvasionFinish
        or PerformanceAudioCue.ClothesTear
        or PerformanceAudioCue.Climax;

    public static bool IsAdult(PerformanceLoopCue cue) => cue is
        PerformanceLoopCue.Breath;

    public static string Control(ControlVisualKind kind) => kind switch
    {
        ControlVisualKind.Tentacle => Cutscenes + "control/restraint_tentacle_corridor.png",
        ControlVisualKind.Device => Cutscenes + "control/restraint_suit.png",
        _ => Cutscenes + "control/restraint_humanoid.png",
    };

    public static string Invasion(InvasionVisualKind kind) =>
        Cutscenes + "invasion/invasion_" + (kind switch
        {
            InvasionVisualKind.Tentacle => "tentacle.png",
            InvasionVisualKind.Alien => "alien.png",
            InvasionVisualKind.HumanoidGroup => "humanoid_group.png",
            InvasionVisualKind.Parasite => "parasite.png",
            InvasionVisualKind.Slime => "slime.png",
            InvasionVisualKind.MagicDevice => "magic_device.png",
            _ => "weak_monster.png",
        });

    public static readonly string[] MasturbationSequence =
    [
        Cutscenes + "rest_site_masturbation/masturbation_start.png",
        Cutscenes + "rest_site_masturbation/masturbation_peak.png",
        Cutscenes + "rest_site_masturbation/masturbation_afterglow.png",
    ];
}
