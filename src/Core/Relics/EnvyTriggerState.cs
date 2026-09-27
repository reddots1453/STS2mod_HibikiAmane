namespace MaidenSuccubus.Core.Relics;

internal struct EnvyTriggerState
{
    public bool Used;

    public bool TryUse(int stage, bool ownApplication, bool harmfulChange)
    {
        if (stage is < 1 or > 4 || Used || !ownApplication || !harmfulChange) return false;
        Used = true;
        return true;
    }

    public void StartTurn(int stage, bool ownTurn)
    {
        if (stage is 3 or 4 && ownTurn) Used = false;
    }

    public static int CardsToDraw(int stage) => stage is >= 2 and <= 4 ? 1 : 0;
}
