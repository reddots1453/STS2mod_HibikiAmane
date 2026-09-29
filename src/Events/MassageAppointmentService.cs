using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Data;

namespace MaidenSuccubus.Events;

internal static class MassageAppointmentService
{
    internal static void Schedule(RunState run, int act)
    {
        if (act is not (2 or 3)) throw new ArgumentOutOfRangeException(nameof(act));
        Corruption.Handle.Modify(run, state => state.MassageAppointmentAct = act);
    }

    internal static void Cancel(RunState run) =>
        Corruption.Handle.Modify(run, state => state.MassageAppointmentAct = 0);

    internal static bool DueForAct(int currentActIndex, int appointmentAct,
        int actTwoUnknownRooms, int actThreeUnknownRooms) => currentActIndex switch
    {
        1 => appointmentAct == 2 && actTwoUnknownRooms >= 1,
        2 => appointmentAct == 3 && actThreeUnknownRooms >= 1,
        _ => false,
    };

    internal static bool IsDue(RunState run, MapPointType pointType)
    {
        if (pointType != MapPointType.Unknown
            || run.Players.Count == 0
            || run.Players.Any(player => player.Character is not MaidenSuccubusCharacter))
            return false;
        CorruptionState state = Corruption.Handle.Get(run);
        return DueForAct(run.CurrentActIndex, state.MassageAppointmentAct,
            state.ActTwoUnknownRoomsVisited, state.ActThreeUnknownRoomsVisited);
    }

    internal static EventModel? EventFor(RunState run, MapPointType pointType)
    {
        if (!IsDue(run, pointType)) return null;
        return run.CurrentActIndex == 1
            ? ModelDb.Event<MassageShopSecond>()
            : ModelDb.Event<MassageShopThird>();
    }

    internal static void RoomCreated(RunState run, MapPointType pointType, AbstractRoom room)
    {
        if (pointType != MapPointType.Unknown
            || run.CurrentActIndex is not (1 or 2)
            || run.Players.Count == 0
            || run.Players.Any(player => player.Character is not MaidenSuccubusCharacter))
            return;
        EventModel? expected = EventFor(run, pointType);
        bool fulfilled = room is EventRoom eventRoom
            && expected != null && eventRoom.ModelId == expected.Id;
        if (fulfilled) run.AddVisitedEvent(expected!);
        Corruption.Handle.Modify(run, state =>
        {
            if (run.CurrentActIndex == 1) state.ActTwoUnknownRoomsVisited++;
            else state.ActThreeUnknownRoomsVisited++;
            if (fulfilled) state.MassageAppointmentAct = 0;
        });
    }
}
