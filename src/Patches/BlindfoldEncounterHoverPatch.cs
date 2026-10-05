using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

// Native tooltip UI; read-only encounter lookup adapted from local Foresight.
[HarmonyPatch]
internal static class BlindfoldEncounterHoverPatch
{
    private const string MapMeta = "maiden_blindfold_map_preview";

    [HarmonyPatch(typeof(NMapPoint), "OnFocus")]
    [HarmonyPostfix]
    private static void AfterMapFocus(NMapPoint __instance) => Safe.Run(() =>
    {
        if (__instance.HasMeta(MapMeta) || __instance.State == MapPointState.Traveled
            || __instance.Point == null) return;
        var run = RunManager.Instance?.DebugOnlyGetState();
        var player = run?.Players.FirstOrDefault(LocalContext.IsMe);
        if (run == null || !BlindfoldPresentation.HasEffect(player)) return;
        RoomType? type = __instance.Point.PointType switch
        {
            MapPointType.Monster => RoomType.Monster,
            MapPointType.Elite => RoomType.Elite,
            _ => null,
        };
        if (type == null) return;
        var set = NHoverTipSet.CreateAndShow(__instance,
            BlindfoldPresentation.EncounterTip(run.Act, type.Value), HoverTip.GetHoverTipAlignment(__instance));
        if (set == null) return;
        __instance.SetMeta(MapMeta, true);
        // Match the original map-history tooltip's deferred alignment.
        Callable.From(() => Safe.Run(() =>
        {
            if (GodotObject.IsInstanceValid(__instance) && GodotObject.IsInstanceValid(set)
                && !set.IsQueuedForDeletion())
                set.SetAlignment(__instance, HoverTip.GetHoverTipAlignment(__instance));
        }, nameof(BlindfoldEncounterHoverPatch))).CallDeferred();
    }, nameof(BlindfoldEncounterHoverPatch));

    [HarmonyPatch(typeof(NMapPoint), "OnUnfocus")]
    [HarmonyPostfix]
    private static void AfterMapUnfocus(NMapPoint __instance) => Safe.Run(() =>
    {
        // The native OnUnfocus already removes its NHoverTipSet.
        if (__instance.HasMeta(MapMeta)) __instance.RemoveMeta(MapMeta);
    }, nameof(BlindfoldEncounterHoverPatch));
}
