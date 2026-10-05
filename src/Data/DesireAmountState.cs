namespace MaidenSuccubus.Data;

/// <summary>
/// Per-player run bridge for desire while no PlayerCombatState exists.
/// RitsuLib remains responsible for the live combat resource; this saved
/// value keeps non-combat UI and event choices on the same cross-combat value.
/// </summary>
public sealed class DesireAmountState
{
    public int Amount { get; set; }

    /// <summary>
    /// Distinguishes an intentional zero from a pre-migration save that only
    /// contains RitsuLib's secondary-resource snapshot.
    /// </summary>
    public bool HasValue { get; set; }

    // Saved per player; rechecks while still at maximum must not replay audio.
    public bool MaximumAudioTriggered { get; set; }
}
