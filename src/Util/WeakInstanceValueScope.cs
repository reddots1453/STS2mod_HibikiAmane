using System.Runtime.CompilerServices;

namespace MaidenSuccubus.Util;

// Main-thread scopes. Nested events restore the previous snapshot, including
// exceptional exits; neither the table nor a scope retains the key instance.
internal sealed class WeakInstanceValueScope<TKey, TValue> where TKey : class
{
    private readonly ConditionalWeakTable<TKey, LinkedList<TValue>> _values = new();

    internal IDisposable Enter(TKey key, TValue value)
    {
        var values = _values.GetOrCreateValue(key);
        return new Scope(values, values.AddLast(value));
    }

    internal bool TryGet(TKey key, out TValue value)
    {
        if (_values.TryGetValue(key, out var values) && values.Last is { } last)
        {
            value = last.Value;
            return true;
        }
        value = default!;
        return false;
    }

    private sealed class Scope(LinkedList<TValue> values, LinkedListNode<TValue> node) : IDisposable
    {
        public void Dispose()
        {
            if (node.List != null) values.Remove(node);
        }
    }
}
