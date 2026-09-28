namespace MaidenSuccubus.Core.Powers;

/// <summary>After native type/visibility filtering, count layers, not signed stat changes.</summary>
internal static class PowerLayerMath
{
    internal static int Count(IEnumerable<int> amounts) => amounts.Sum(amount => Math.Abs(amount));
}
