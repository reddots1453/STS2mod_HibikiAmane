#if DEBUG
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;

namespace MaidenSuccubus.Debugging.CardEffects;

/// <summary>
/// Prevents the Rest probe from advancing the live disposable combat while
/// still proving that the card requested an end turn. Active only inside an
/// explicit test scope in Debug builds.
/// </summary>
[HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.EndTurn))]
internal static class CardEffectTestEndTurnInterception
{
    private static int _scopeDepth;
    internal static int SuppressedCalls { get; private set; }

    internal static IDisposable Begin()
    {
        _scopeDepth++;
        SuppressedCalls = 0;
        return new Scope();
    }

    private static bool Prefix()
    {
        if (_scopeDepth <= 0)
            return true;
        SuppressedCalls++;
        return false;
    }

    private sealed class Scope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _scopeDepth = Math.Max(0, _scopeDepth - 1);
        }
    }
}
#endif
