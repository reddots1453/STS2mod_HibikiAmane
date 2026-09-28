using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Acts;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(NRun), nameof(NRun._Ready))]
public static class FourthRouteRewardWatcherPatch
{
    [HarmonyPostfix]
    public static void Postfix(NRun __instance) => Safe.Run(() =>
    {
        if (!__instance.HasNode("FourthRouteRewardWatcher"))
            __instance.AddChild(new FourthRouteRewardWatcher { Name = "FourthRouteRewardWatcher" });
    }, "FourthRoute.RewardWatcher");
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatVictory))]
public static class FourthRouteVictoryRewardPatch
{
    [HarmonyPostfix]
    public static void Postfix(IRunState __0, CombatRoom __2, ref Task __result)
    {
        Task original = __result, wrapped = original;
        Safe.Run(() => wrapped = AfterVictory(original, __0, __2), "FourthRoute.VictoryRewardAwait");
        __result = wrapped;
    }
    internal static async Task AfterVictory(Task original, IRunState state, CombatRoom room)
    {
        await original; // All trial listeners and native victory hooks must have finished.
        try
        {
            if (TestMode.IsOn || state is not RunState run || run.Players.Count != 1
                || RunManager.Instance.NetService.Type == NetGameType.Replay
                || !ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), run)
                || !ReferenceEquals(run.CurrentRoom, room) || LocalContext.GetMe(run) is not { } player
                || !FourthRouteLifecycle.IsEligible(player) || player.Creature.IsDead) return;
            // Settings or an inspection window may still be open at victory.
            // Do not miss the final reward merely because presentation is temporarily busy.
            while (FourthRouteRewardFlow.Pending(run) != null && !FourthRouteRewardFlow.Ready(player, run, victoryBoundary: true))
            {
                if (NRun.Instance is not { } scene || !GodotObject.IsInstanceValid(scene) || !scene.IsInsideTree()
                    || TestMode.IsOn || player.Creature.IsDead
                    || !ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), run) || !ReferenceEquals(run.CurrentRoom, room)) return;
                await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            // Caller still awaits this task before SaveRun/CombatWon/vanilla ending.
            await FourthRouteRewardFlow.Show(player, victoryBoundary: true);
        }
        catch (Exception ex)
        {
            // Our UI must not stop the native victory/save path. Original hook
            // faults still propagate above without offering a reward.
            MaidenSuccubusMod.Logger.Error("[FourthRouteReward] Victory presentation failed: " + ex);
        }
    }
}
