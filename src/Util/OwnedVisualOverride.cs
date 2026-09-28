namespace MaidenSuccubus.Util;

// Restore only a value we still own. External animation/mod changes win.
internal sealed class OwnedVisualOverride<T>
{
    private bool _applied;
    private T _original = default!;
    private T _value = default!;

    internal bool Matches(T current) => _applied && EqualityComparer<T>.Default.Equals(current, _value);

    internal T Apply(T current, Func<T, T> transform)
    {
        _original = Restore(current);
        _value = transform(_original);
        _applied = true;
        return _value;
    }

    internal T Restore(T current)
    {
        bool restore = Matches(current);
        _applied = false;
        return restore ? _original : current;
    }
}
