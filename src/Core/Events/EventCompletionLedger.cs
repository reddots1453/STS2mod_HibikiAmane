using System.Runtime.CompilerServices;

namespace MaidenSuccubus.Core.Events;

// An option may be recreated on a later page. Deduplicate by event identity AND
// stable option key, not the EventOption object and not the entire run.
internal sealed class EventCompletionLedger<TEvent> where TEvent : class
{
    private readonly ConditionalWeakTable<TEvent, HashSet<string>> _committed = new();

    internal bool TryCommit(TEvent model, string key, bool applicable,
        bool wasFinished, bool isFinished, bool pageChanged)
    {
        if (!applicable || wasFinished || (!isFinished && !pageChanged)) return false;
        return _committed.GetOrCreateValue(model).Add(key);
    }
}
