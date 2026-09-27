using System.Runtime.CompilerServices;

namespace MaidenSuccubus.Util;

/// <summary>Main-thread instance scopes; weak keys never keep departed models alive.</summary>
internal sealed class WeakInstanceScope<T> where T : class
{
    private sealed class State { internal int Depth; }
    private readonly ConditionalWeakTable<T, State> _active = new();

    internal bool Contains(T key) => _active.TryGetValue(key, out var state) && state.Depth > 0;

    internal IDisposable Enter(T key)
    {
        var state = _active.GetOrCreateValue(key);
        state.Depth++;
        return new Scope(state);
    }

    private sealed class Scope(State state) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            state.Depth--;
        }
    }
}
