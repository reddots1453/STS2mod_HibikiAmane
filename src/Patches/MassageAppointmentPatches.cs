using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Events;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(RunManager), "RollRoomTypeFor")]
public static class MassageAppointmentRoomRollPatch
{
    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    public static bool Prefix(RunManager __instance, MapPointType pointType, ref RoomType __result)
    {
        bool due = false;
        Safe.Run(() =>
        {
            if (__instance.DebugOnlyGetState() is RunState run)
                due = MassageAppointmentService.IsDue(run, pointType);
        }, "MassageAppointment.Roll");
        if (due) __result = RoomType.Event;
        return !due;
    }
}

[HarmonyPatch(typeof(RunManager), "CreateRoom")]
public static class MassageAppointmentCreateRoomPatch
{
    [HarmonyPrefix]
    public static void Prefix(RunManager __instance, RoomType roomType,
        MapPointType mapPointType, ref AbstractModel? model)
    {
        AbstractModel? selected = model;
        Safe.Run(() =>
        {
            if (roomType == RoomType.Event && selected == null
                && __instance.DebugOnlyGetState() is RunState run)
                selected = MassageAppointmentService.EventFor(run, mapPointType);
        }, "MassageAppointment.SelectEvent");
        model = selected;
    }

    [HarmonyPostfix]
    public static void Postfix(RunManager __instance, MapPointType mapPointType,
        AbstractRoom __result) => Safe.Run(() =>
    {
        if (__instance.DebugOnlyGetState() is RunState run)
            MassageAppointmentService.RoomCreated(run, mapPointType, __result);
    }, "MassageAppointment.CountRoom");
}
