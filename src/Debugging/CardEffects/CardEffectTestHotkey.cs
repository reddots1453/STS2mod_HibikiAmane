#if DEBUG
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;

namespace MaidenSuccubus.Debugging.CardEffects;

/// <summary>
/// Starts the destructive exact-effect suite only after the player has entered
/// a disposable MaidenSuccubus combat. This mirrors the proven manual-entry
/// boundary used by sts2_contrib_tests while retaining this suite's exact
/// numerical assertions.
/// </summary>
internal static class CardEffectTestHotkey
{
    private static bool _registered;
    private static bool _f10Pressed;
    private static bool _running;

    internal static async void Register()
    {
        if (_registered)
            return;

        try
        {
            while (Engine.GetMainLoop() is not SceneTree sceneTree || sceneTree.Root == null)
                await Task.Delay(200);

            ((SceneTree)Engine.GetMainLoop()).ProcessFrame += OnProcessFrame;
            _registered = true;
            MaidenSuccubusMod.Logger.Info(
                "[CardEffectTest] Ready. In a disposable MaidenSuccubus combat, "
                + "press F10 for all cards or Shift+F10 for the 59 iteration-two cards.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error(
                "[CardEffectTest] Failed to register F10 trigger: " + ex);
        }
    }

    private static void OnProcessFrame()
    {
        try
        {
            bool f10Now = Input.IsKeyPressed(Key.F10);
            bool shiftNow = Input.IsKeyPressed(Key.Shift);
            if (f10Now && !_f10Pressed)
                RunFromActiveCombat(shiftNow ? "iteration2" : "all");
            _f10Pressed = f10Now;
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error(
                "[CardEffectTest] F10 trigger failed: " + ex);
        }
    }

    private static async void RunFromActiveCombat(string requested)
    {
        if (_running)
        {
            MaidenSuccubusMod.Logger.Info(
                "[CardEffectTest] Exact card-effect suite is already running.");
            return;
        }

        CombatState? combat = CombatManager.Instance.DebugOnlyGetState();
        if (!CombatManager.Instance.IsInProgress || combat == null)
        {
            MaidenSuccubusMod.Logger.Info(
                "[CardEffectTest] Not in combat. Enter a disposable combat, then press F10.");
            return;
        }

        if (combat.Players.Count != 1)
        {
            MaidenSuccubusMod.Logger.Info(
                "[CardEffectTest] A single-player disposable combat is required.");
            return;
        }

        Player player = combat.Players[0];
        if (player.Character.GetType().Name != "MaidenSuccubusCharacter")
        {
            MaidenSuccubusMod.Logger.Info(
                "[CardEffectTest] Use the MaidenSuccubus character before pressing F10.");
            return;
        }

        _running = true;
        MaidenSuccubusMod.Logger.Info(
            requested == "iteration2"
                ? "[CardEffectTest] Shift+F10 accepted; starting 59 iteration-two card checks."
                : "[CardEffectTest] F10 accepted; starting all 215 registered card checks.");
        try
        {
            string summary = await CardEffectTestRunner.Run(player, requested);
            MaidenSuccubusMod.Logger.Info("[CardEffectTest] " + summary);
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error(
                "[CardEffectTest] Suite aborted before report completion: " + ex);
        }
        finally
        {
            _running = false;
        }
    }
}
#endif
