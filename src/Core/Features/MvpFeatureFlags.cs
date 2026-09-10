namespace MaidenSuccubus.Core.Features;

/// <summary>
/// Runtime boundary dictated by DOC-MVP-001.  A feature being implemented in
/// the assembly does not make it part of the shipping MVP.
/// </summary>
public static class MvpFeatureFlags
{
    public static readonly bool EnemyIntentExtensions = true;
    public static readonly bool ControlAndInvasion = true;
    public static readonly bool Portable = true;
    public static readonly bool SpecialInvasionMerchant = true;
    public static readonly bool BossBlessings = false;
    public static readonly bool ExtraStartProfiles = false;
    public static readonly bool LegacyUnconditionalFourthAct = false;
}

public static class MvpPatchPolicy
{
    public static bool ShouldInstall(Type patchType)
    {
        for (Type? current = patchType; current is not null; current = current.DeclaringType)
        {
            bool? enabled = current.Name switch
            {
                "IntentAdapterPatch" or "EscapeCardProjectionPatches"
                    or "EscapeOriginalStateAccessPatches" =>
                    MvpFeatureFlags.ControlAndInvasion,
                "MerchantInvasionCursePatch" =>
                    MvpFeatureFlags.SpecialInvasionMerchant,
                "PortableRetainPatch" => MvpFeatureFlags.Portable,
                "BlindfoldIntentPatch" => false,
                _ => null,
            };
            if (enabled.HasValue)
            {
                return enabled.Value;
            }
        }
        return true;
    }
}
