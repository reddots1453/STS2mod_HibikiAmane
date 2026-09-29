using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;
using MaidenSuccubus.UI;
using MaidenSuccubus.Bootstrap;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// Opens route selection and pending route rewards only after the map scene is
/// stable. Both flows use the original map-compatible modal container and never
/// run from room-entry or combat-start hooks, which are still inside the scene
/// transition lifecycle.
/// </summary>
[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Open))]
public static class FourthRouteQuestSelectionPatch
{
    private static readonly FieldInfo? ActAnimationTweenField =
        ModCompatibility.FindField(
            typeof(NMapScreen), "_actAnimTween", typeof(Tween));

    [HarmonyPostfix]
    public static void Postfix(NMapScreen __instance) =>
        Safe.Run(
            () => TaskHelper.RunSafely(ShowWhenMapIsStable(__instance)),
            "FourthRoute.QuestSelection.Schedule");

    private static async Task ShowWhenMapIsStable(NMapScreen map)
    {
        if (ActAnimationTweenField == null)
        {
            return;
        }
        RunState? runState = RunManager.Instance.DebugOnlyGetState();
        Player? player = runState?.Players.FirstOrDefault(candidate =>
            FourthRouteLifecycle.IsEligible(candidate) && LocalContext.IsMe(candidate));
        bool needsQuest = runState is not null
            && GoddessTrialMode.Enabled(runState)
            && !FourthRouteProgressService.TryGetQuest(runState, out _);
        bool needsReward = runState is not null
            && GoddessTrialMode.Enabled(runState)
            && FourthRouteProgressService.HasPendingInitialReward(runState);
        bool needsAlignment = runState is not null && GoddessTrialMode.NeedsActChoice(runState)
            && runState.Players.Count == 1;
        if (runState is null || player is null || (!needsQuest && !needsReward && !needsAlignment))
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
            || (!GoddessTrialMode.NeedsActChoice(runState)
                && (FourthRouteProgressService.TryGetQuest(runState, out _)
                    || !GoddessTrialMode.Enabled(runState))
                && !FourthRouteProgressService.HasPendingInitialReward(runState)))
        {
            return;
        }

        bool restoreTravel = map.IsOpen;
        if (restoreTravel) map.SetTravelEnabled(false);
        try
        {
            MaidenSuccubusMod.Logger.Info(
                $"Fourth-route map modal ready: quest={needsQuest}, reward={needsReward}, alignment={needsAlignment}, mapOpen={map.IsOpen}.");
            if (GoddessTrialMode.NeedsActChoice(runState))
            {
                int actIndex = runState.CurrentActIndex;
                while (GoddessTrialMode.NeedsActChoice(runState)
                    && RunManager.Instance.DebugOnlyGetState() == runState
                    && GodotObject.IsInstanceValid(map) && map.IsInsideTree()
                    && ReferenceEquals(NMapScreen.Instance, map))
                {
                    int? delta = await ActAlignmentChoiceScreen.Show(actIndex);
                    if (delta is int change && RunManager.Instance.DebugOnlyGetState() == runState)
                        GoddessTrialMode.ResolveActChoice(runState, actIndex, change);
                    else
                        await WaitOneFrame(); // Closing the modal is not a third option.
                }
                return;
            }
            if (GoddessTrialMode.Enabled(runState) && !FourthRouteProgressService.TryGetQuest(runState, out _))
                await FourthRouteLifecycle.For(player).EnsureFourthRouteQuestSelected();
            if (GoddessTrialMode.Enabled(runState) && FourthRouteProgressService.HasPendingInitialReward(runState))
                await FourthRouteLifecycle.For(player).EnsureFourthRouteRewardClaimed();
        }
        finally
        {
            if (restoreTravel && !GoddessTrialMode.NeedsActChoice(runState)
                && RunManager.Instance.DebugOnlyGetState() == runState
                && NMapScreen.Instance is { IsOpen: true } currentMap)
            {
                currentMap.SetTravelEnabled(true);
                MaidenSuccubusMod.Logger.Info(
                    $"Fourth-route map travel restored: enabled={currentMap.IsTravelEnabled}.");
            }
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
