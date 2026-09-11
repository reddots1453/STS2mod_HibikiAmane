#if DEBUG
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;

namespace MaidenSuccubus.Debugging.ControlIntents;

/// <summary>
/// Runs the destructive suite only after the player manually enters a
/// disposable combat. Shift+F10 extends the established F10 test boundary
/// without reserving another global function key.
/// </summary>
internal static class ControlIntentTestHotkey
{
    private static bool _registered;
    private static bool _chordPressed;
    private static bool _running;

    internal static async void Register()
    {
        if (_registered)
            return;

        try
        {
            while (Engine.GetMainLoop() is not SceneTree sceneTree
                || sceneTree.Root == null)
            {
                await Task.Delay(200);
            }

            ((SceneTree)Engine.GetMainLoop()).ProcessFrame += OnProcessFrame;
            _registered = true;
            MaidenSuccubusMod.Logger.Info(
                "[ControlIntentTest] Ready. Enter a disposable "
                + "MaidenSuccubus combat and press Shift+F10.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error(
                "[ControlIntentTest] Failed to register Shift+F10 trigger: " + ex);
        }
    }

    private static void OnProcessFrame()
    {
        try
        {
            bool chordNow = Input.IsKeyPressed(Key.F10)
                && Input.IsKeyPressed(Key.Shift);
            if (chordNow && !_chordPressed)
                RunFromActiveCombat();
            _chordPressed = chordNow;
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error(
                "[ControlIntentTest] Shift+F10 trigger failed: " + ex);
        }
    }

    private static async void RunFromActiveCombat()
    {
        if (_running)
        {
            MaidenSuccubusMod.Logger.Info(
                "[ControlIntentTest] Suite is already running.");
            return;
        }

        CombatState? combat = CombatManager.Instance.DebugOnlyGetState();
        if (!CombatManager.Instance.IsInProgress || combat == null)
        {
            MaidenSuccubusMod.Logger.Info(
                "[ControlIntentTest] Not in combat. Enter a disposable combat, "
                + "then press Shift+F10.");
            return;
        }
        if (combat.Players.Count != 1)
        {
            MaidenSuccubusMod.Logger.Info(
                "[ControlIntentTest] A single-player disposable combat is required.");
            return;
        }

        Player player = combat.Players[0];
        if (player.Character.GetType().Name != "MaidenSuccubusCharacter")
        {
            MaidenSuccubusMod.Logger.Info(
                "[ControlIntentTest] Use MaidenSuccubus before pressing Shift+F10.");
            return;
        }

        _running = true;
        MaidenSuccubusMod.Logger.Info(
            "[ControlIntentTest] Shift+F10 accepted; starting 9 scenarios.");
        try
        {
            string summary = await ControlIntentTestRunner.Run(player);
            MaidenSuccubusMod.Logger.Info("[ControlIntentTest] " + summary);
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error(
                "[ControlIntentTest] Suite aborted before report completion: " + ex);
        }
        finally
        {
            _running = false;
        }
    }
}
#endif
