using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// Opens route selection and pending route rewards only after the map scene is
/// stable. Both flows use a dedicated overlay and never run from room-entry or
/// combat-start hooks, which are still inside the scene transition lifecycle.
/// </summary>
[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Open))]
public static class FourthRouteQuestSelectionPatch
{
    private static readonly FieldInfo ActAnimationTweenField =
        AccessTools.Field(typeof(NMapScreen), "_actAnimTween");

    [HarmonyPostfix]
    public static void Postfix(NMapScreen __instance) =>
        Safe.Run(
            () => TaskHelper.RunSafely(ShowWhenMapIsStable(__instance)),
            "FourthRoute.QuestSelection.Schedule");

    private static async Task ShowWhenMapIsStable(NMapScreen map)
    {
        RunState? runState = RunManager.Instance.DebugOnlyGetState();
        TwinSoulChalice? chalice = runState?.Players
            .Select(player => player.GetRelic<TwinSoulChalice>())
            .FirstOrDefault(relic => relic != null);
        bool needsQuest = runState is not null
            && !FourthRouteProgressService.TryGetQuest(runState, out _);
        bool needsReward = runState is not null
            && FourthRouteProgressService.HasPendingInitialReward(runState);
        if (runState is null || chalice is null || (!needsQuest && !needsReward))
        {
            return;
        }

        // Let Open() and the surrounding room transition finish. The first-map
        // tutorial is scheduled only after the act banner animation completes,
        // so wait for that animation and one short grace period before testing
        // the modal container.
        await WaitOneFrame();
        for (int frame = 0; frame < 600
             && ActAnimationTweenField.GetValue(map) is Tween; frame++)
        {
            if (RunManager.Instance.DebugOnlyGetState() != runState)
            {
                return;
            }
            await WaitOneFrame();
        }
        await Task.Delay(150);
        for (int frame = 0; frame < 600; frame++)
        {
            if (RunManager.Instance.DebugOnlyGetState() != runState)
            {
                return;
            }
            if (NModalContainer.Instance?.OpenModal is null)
            {
                break;
            }
            await WaitOneFrame();
        }

        if (RunManager.Instance.DebugOnlyGetState() != runState
            || NModalContainer.Instance?.OpenModal is not null
            || (FourthRouteProgressService.TryGetQuest(runState, out _)
                && !FourthRouteProgressService.HasPendingInitialReward(runState)))
        {
            return;
        }

        bool restoreTravel = map.IsOpen;
        if (restoreTravel) map.SetTravelEnabled(false);
        try
        {
            if (!FourthRouteProgressService.TryGetQuest(runState, out _))
                await chalice.EnsureFourthRouteQuestSelected();
            if (FourthRouteProgressService.HasPendingInitialReward(runState))
                await chalice.EnsureFourthRouteRewardClaimed();
        }
        finally
        {
            if (restoreTravel
                && RunManager.Instance.DebugOnlyGetState() == runState
                && NMapScreen.Instance is { IsOpen: true } currentMap)
                currentMap.SetTravelEnabled(true);
        }
    }

    private static async Task WaitOneFrame()
    {
        if (NGame.Instance is { } game)
        {
            await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        else
        {
            await Task.Yield();
        }
    }
}
