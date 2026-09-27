using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(RunManager), "RollRoomTypeFor")]
public static class GreedRoomSelectionBoundaryPatch
{
    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    public static void Begin(RunManager __instance) => Safe.Run(() =>
    {
        if (__instance.DebugOnlyGetState() is RunState run) GreedShopService.Cancel(run);
    }, "Greed.BeginRoomSelection");
}

[HarmonyPatch(typeof(RunManager), "RollRoomTypeFor")]
public static class GreedUnknownRoomPatch
{
    // A scheduled event must take over the roll first and suppress the original.
    // Never replace a room already reserved by a higher-priority provider.
    [HarmonyPrefix, HarmonyPriority(Priority.Last)]
    public static bool Prefix(RunManager __instance, MapPointType pointType, bool __runOriginal, ref RoomType __result)
    {
        bool selected = false;
        Safe.Run(() =>
        {
            if (__instance.DebugOnlyGetState() is RunState run)
                selected = GreedShopService.Select(run, pointType, reserved: !__runOriginal);
        }, "Greed.SelectUnknown");
        if (selected) __result = RoomType.Shop;
        return !selected;
    }

    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    public static void Postfix(RunManager __instance, RoomType __result) => Safe.Run(() =>
    {
        if (__result != RoomType.Shop && __instance.DebugOnlyGetState() is RunState run)
            GreedShopService.Cancel(run);
    }, "Greed.ValidateSelectedRoom");
}

[HarmonyPatch(typeof(RunManager), "CreateRoom")]
public static class GreedCreatedRoomPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunManager __instance, MapPointType mapPointType, AbstractRoom __result) => Safe.Run(() =>
    {
        if (__instance.DebugOnlyGetState() is RunState run)
            GreedShopService.Created(run, mapPointType, __result);
    }, "Greed.BindCreatedRoom");
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyMerchantPrice))]
public static class GreedFinalPricePatch
{
    // Enforce zero after all ordinary model price modifiers (including fixed prices).
    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    public static void Postfix(Player player, MerchantEntry entry, ref decimal __result)
    {
        bool free = false;
        Safe.Run(() => free = player.Relics.OfType<GreedRouteRelic>().Any(relic => relic.IsFreeShopFor(player)),
            "Greed.FinalMerchantPrice");
        if (free) __result = 0m;
    }
}
