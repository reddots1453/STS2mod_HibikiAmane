namespace MaidenSuccubus.UI;

/// <summary>
/// Lifecycle bridge for run-level UI that must react when combat takes over
/// the screen. It replaces permanent visibility polling timers.
/// </summary>
public static class RunUiRefreshEvents
{
    public static event Action<bool>? CombatVisibilityChanged;

    internal static void PublishCombatVisibility(bool inCombat)
    {
        foreach (Action<bool> handler in
            CombatVisibilityChanged?.GetInvocationList().Cast<Action<bool>>()
            ?? [])
        {
            try
            {
                handler(inCombat);
            }
            catch (Exception ex)
            {
                MaidenSuccubusMod.Logger.Warn(
                    $"Run UI refresh listener failed: {ex.Message}");
            }
        }
    }
}
