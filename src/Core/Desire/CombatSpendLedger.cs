using System.Runtime.CompilerServices;

namespace MaidenSuccubus.Core.Desire;

/// <summary>Actual committed payments, scoped by combat and player object identity.</summary>
internal sealed class CombatSpendLedger<TCombat, TPlayer>
    where TCombat : class
    where TPlayer : class
{
    private sealed class Counter { internal int Amount; }
    private sealed class Session
    {
        internal bool Closed;
        internal ConditionalWeakTable<TPlayer, Counter> Players = new();
    }
    private readonly ConditionalWeakTable<TCombat, Session> _sessions = new();

    internal int Get(TCombat? combat, TPlayer player) =>
        combat != null && _sessions.TryGetValue(combat, out var session)
        && !session.Closed && session.Players.TryGetValue(player, out var count)
            ? count.Amount : 0;

    internal void Record(TCombat combat, TPlayer player, int amount)
    {
        if (amount <= 0) return;
        var session = _sessions.GetOrCreateValue(combat);
        if (session.Closed) return;
        var count = session.Players.GetOrCreateValue(player);
        // Reserve one representable hit for the card's initial attack.
        count.Amount = (int)Math.Min(int.MaxValue - 1L, (long)count.Amount + amount);
    }

    internal void Close(TCombat combat)
    {
        var session = _sessions.GetOrCreateValue(combat);
        session.Closed = true;
        session.Players = new();
    }

#if DEBUG
    // Only the destructive disposable-combat fixture reuses a combat object.
    internal void ResetForTests(TCombat combat) => _sessions.Remove(combat);
#endif
}
