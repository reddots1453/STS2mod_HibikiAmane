namespace MaidenSuccubus.Core.Relics;

// Value type: mutable model clones must not share another relic's turn ledger.
internal struct SlothTurnState
{
    public int EnergySpent;
    public bool PendingEnergy;
    public bool EndResolved;

    public void StartTurn(bool ownTurn)
    {
        if (!ownTurn) return;
        EnergySpent = 0;
        EndResolved = false;
        // The previous turn's reward is consumed after the native energy reset.
    }

    public void RecordPayment(int amount, bool ownPayment)
    {
        if (!ownPayment || EndResolved || amount <= 0) return;
        EnergySpent = (int)Math.Min(int.MaxValue, (long)Math.Max(0, EnergySpent) + amount);
    }

    public int ResolveEnd(int stage, bool ownTurn)
    {
        if (!ownTurn || EndResolved || stage is < 1 or > 4) return 0;
        EndResolved = true; // Reserve before any asynchronous block command.
        bool qualifies = EnergySpent <= 2;
        PendingEnergy = qualifies && stage is 3 or 4;
        return qualifies ? (stage == 1 ? 6 : 12) : 0;
    }

    public int TakeEnergy(int stage, bool ownTurn)
    {
        if (!ownTurn || !PendingEnergy) return 0;
        PendingEnergy = false; // Reserve before the asynchronous energy command.
        return stage is 3 or 4 ? 3 : 0;
    }
}
