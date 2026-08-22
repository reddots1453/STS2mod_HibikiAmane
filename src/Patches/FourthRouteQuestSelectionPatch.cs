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
/// Opens the mandatory route quest only after the map scene exists. A blocking
/// card choice must never be awaited from room-entry or combat-start hooks: those
/// hooks are part of the scene transition itself and waiting there can leave the
/// run behind the transition backstop indefinitely.
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
        if (runState is null || chalice is null
            || FourthRouteProgressService.TryGetQuest(runState, out _))
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
            || FourthRouteProgressService.TryGetQuest(runState, out _))
        {
            return;
        }

        bool reopenMap = map.IsOpen;
        if (reopenMap)
        {
            map.SetTravelEnabled(false);
            map.Close(animateOut: false);
            await WaitOneFrame();
        }

        try
        {
            await chalice.EnsureFourthRouteQuestSelected();
        }
        finally
        {
            if (reopenMap
                && RunManager.Instance.DebugOnlyGetState() == runState
                && NMapScreen.Instance is { IsOpen: false } currentMap)
            {
                currentMap.Open(isOpenedFromTopBar: false);
                currentMap.SetTravelEnabled(true);
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
