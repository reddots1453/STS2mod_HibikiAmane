namespace MaidenSuccubus.Data;

/// <summary>
/// Run-persistent desire data. Desire intentionally lives on the run rather
/// than CombatState so it survives room and combat transitions.
/// </summary>
public sealed class DesireState
{
    public bool HasGrantedFirstMaxCorruption { get; set; }

    /// <summary>
    /// Maximum desire reached outside combat; consumed on the next combat's
    /// first player turn.
    /// </summary>
    public bool PendingFirstTurnStun { get; set; }
}
