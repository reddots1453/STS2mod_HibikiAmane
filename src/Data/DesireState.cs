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
    /// Full-desire penalties waiting for the next legal player-turn start.
    /// Kept as a count because separate gains can reach ten more than once
    /// before that resolution point.
    /// </summary>
    public int PendingClimaxResolutions { get; set; }
}
