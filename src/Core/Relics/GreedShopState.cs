namespace MaidenSuccubus.Core.Relics;

/// <summary>Saved entitlement, not inventory lifetime. Selecting a room never consumes it.</summary>
public struct GreedShopState
{
    public bool Pending;
    public bool Active;
    public string? Location;

    public readonly bool CanSelect(int stage, string location, bool unknown, bool reserved) =>
        stage is 3 or 4 && unknown && !reserved && !string.IsNullOrEmpty(location)
        && (Pending || Active && Location == location);

    public bool Commit(int stage, string location, bool createdMerchant)
    {
        if (!createdMerchant || !CanSelect(stage, location, true, false)) return false;
        Pending = false;
        Active = true;
        Location = location;
        return true;
    }

    public readonly bool IsFree(int stage, string location, bool sameRoom, bool ownPlayer) =>
        stage is 3 or 4 && ownPlayer && sameRoom && Active
        && !string.IsNullOrEmpty(Location) && Location == location;

    public void Leave()
    {
        Active = false;
        Location = null;
        // Visiting other rooms before the appointed unknown must not spend Pending.
    }
}
