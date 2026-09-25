using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MaidenSuccubus.Presentation;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(NRun), nameof(NRun.SetCurrentRoom))]
internal static class PerformanceRoomTransitionPatch
{
    [HarmonyPrefix]
    private static void Prefix() => Safe.Run(
        PerformanceDirector.OnSceneTransition,
        "Performance.RoomTransition");
}

[HarmonyPatch(typeof(NSceneContainer), nameof(NSceneContainer.SetCurrentScene))]
internal static class PerformanceSceneTransitionPatch
{
    [HarmonyPrefix]
    private static void Prefix() => Safe.Run(
        PerformanceDirector.OnSceneTransition,
        "Performance.SceneTransition");
}
