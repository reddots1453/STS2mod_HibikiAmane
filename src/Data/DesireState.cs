namespace MaidenSuccubus.Data;

/// <summary>
/// Run-persistent desire data. Desire intentionally lives on the run rather
/// than CombatState so it survives room and combat transitions.
/// </summary>
public sealed class DesireState
{
    public bool HasGrantedFirstMaxCorruption { get; set; }

    /// <summary>
    /// Legacy save field kept for runs created before first-iteration rules.
    /// </summary>
    public bool PendingFirstTurnStun { get; set; }

    /// <summary>
    /// A full-desire penalty waiting for the next legal player-turn start.
    /// A full value queues only one resolution, regardless of the current cap.
    /// </summary>
    public bool PendingClimaxResolution { get; set; }

    // Zero in older saves means the original ten-desire threshold.
    public int PendingClimaxThreshold { get; set; }

    /// <summary>
    /// Legacy save field from the count-based implementation. A positive
    /// value is migrated to the single pending flag when the save is read.
    /// </summary>
    public int PendingClimaxResolutions { get; set; }
}
