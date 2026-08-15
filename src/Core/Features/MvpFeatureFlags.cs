namespace MaidenSuccubus.Core.Features;

/// <summary>
/// Runtime boundary dictated by DOC-MVP-001.  A feature being implemented in
/// the assembly does not make it part of the shipping MVP.
/// </summary>
public static class MvpFeatureFlags
{
    public static readonly bool EnemyIntentExtensions = false;
    public static readonly bool ControlAndInvasion = false;
    public static readonly bool Portable = false;
    public static readonly bool SpecialInvasionMerchant = false;
    public static readonly bool BossBlessings = false;
    public static readonly bool ExtraStartProfiles = false;
    public static readonly bool LegacyUnconditionalFourthAct = false;
}

public static class MvpPatchPolicy
{
    private static readonly HashSet<string> DeferredPatchTypes =
    [
        "BossBlessingPatch",
        "BlindfoldIntentPatch",
        "FourthActPatch",
        "FourthActCreationPatch",
        "FourthActRoutePatch",
        "IntentAdapterPatch",
        "MerchantInvasionCursePatch",
        "PortableRetainPatch",
        "EscapeCardVisualPatch",
        "EscapeCardProjectionPatches",
        "InfectionHandSnapshotPatch",
    ];

    public static bool ShouldInstall(Type patchType)
    {
        for (Type? current = patchType; current is not null; current = current.DeclaringType)
        {
            if (DeferredPatchTypes.Contains(current.Name))
            {
                return false;
            }
        }
        return true;
    }
}
