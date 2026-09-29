#if DEBUG
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;
using MaidenSuccubus.Events;
using MaidenSuccubus.Patches;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

internal static class DesignMassageEventContract
{
    internal static async Task Run(Player player, Action<bool, string> check)
    {
        RunState run = (RunState)player.RunState;
        var roll = AccessTools.Method(typeof(RunManager), "RollRoomTypeFor");
        var create = AccessTools.Method(typeof(RunManager), "CreateRoom");
        check(Harmony.GetPatchInfo(roll)?.Prefixes.Any(p =>
            p.PatchMethod.DeclaringType == typeof(MassageAppointmentRoomRollPatch)) == true,
            "real appointment room-roll prefix installed");
        check(Harmony.GetPatchInfo(create)?.Prefixes.Any(p =>
            p.PatchMethod.DeclaringType == typeof(MassageAppointmentCreateRoomPatch)) == true,
            "real appointment event-model prefix installed");
        check(Harmony.GetPatchInfo(create)?.Postfixes.Any(p =>
            p.PatchMethod.DeclaringType == typeof(MassageAppointmentCreateRoomPatch)) == true,
            "real unknown-room counter postfix installed");
        MassageAppointmentService.Cancel(run);
        check(!MassageAppointmentService.DueForAct(1, 2, 0, 0), "appointment waits for second Act 2 unknown");
        check(MassageAppointmentService.DueForAct(1, 2, 1, 0), "Act 2 second unknown is due");
        check(!MassageAppointmentService.DueForAct(1, 3, 1, 0), "wrong-act appointment is not due");
        check(MassageAppointmentService.DueForAct(2, 3, 0, 1), "Act 3 second unknown is due");
        check(!MassageAppointmentService.DueForAct(0, 2, 1, 0), "Act 1 cannot consume appointment");
        CheckAppointmentLifecycle(player, check);

        await PlayerCmd.GainGold(300, player);
        MassageShopFirst general = await Start<MassageShopFirst>(player);
        decimal gold = player.Gold;
        int relics = player.Relics.OfType<Refreshed>().Count();
        await general.CurrentOptions[0].Chosen();
        check(player.Gold == gold - 100 && player.Relics.OfType<Refreshed>().Count() == relics + 1,
            "general package pays 100 and grants Refreshed");
        check(Corruption.Handle.Get(run).MassageAppointmentAct == 0,
            "general package makes no appointment");

        MassageShopFirst first = await Start<MassageShopFirst>(player);
        gold = player.Gold;
        int minor = player.Deck.Cards.OfType<LewdMarkMinorCurse>().Count();
        await first.CurrentOptions[1].Chosen();
        check(player.Gold == gold - 50
            && player.Deck.Cards.OfType<LewdMarkMinorCurse>().Count() == minor + 1
            && Corruption.Handle.Get(run).MassageAppointmentAct == 2,
            "first special package pays 50, adds minor mark and books Act 2");

        await Desire.Set(player, 5);
        MassageShopSecond locked = await Start<MassageShopSecond>(player);
        check(locked.CurrentOptions[1].IsLocked, "second visit refusal locked at desire 5");
        await Desire.Set(player, 4);
        CorruptionCmd.Set(run, 0);
        MassageShopSecond refuse = await Start<MassageShopSecond>(player);
        check(!refuse.CurrentOptions[1].IsLocked, "second visit refusal unlocked below 5");
        await refuse.CurrentOptions[1].Chosen();
        check(CorruptionQuery.Get(run) == -1 && Corruption.Handle.Get(run).MassageAppointmentAct == 0,
            "second visit refusal lowers corruption and ends chain");

        MassageShopSecond second = await Start<MassageShopSecond>(player);
        int spread = player.Deck.Cards.OfType<LewdMarkSpreadCurse>().Count();
        await second.CurrentOptions[0].Chosen();
        check(player.Deck.Cards.OfType<LewdMarkMinorCurse>().Count() == minor
            && player.Deck.Cards.OfType<LewdMarkSpreadCurse>().Count() == spread + 1
            && CorruptionQuery.Get(run) == 0
            && Corruption.Handle.Get(run).MassageAppointmentAct == 3,
            "second special replaces mark, adds corruption and books Act 3");

        MassageShopThird third = await Start<MassageShopThird>(player);
        int complete = player.Deck.Cards.OfType<LewdMarkCompleteCurse>().Count();
        await third.CurrentOptions[1].Chosen();
        check(third.CurrentOptions.Count == 1
            && CorruptionQuery.Get(run) == 0
            && player.Deck.Cards.OfType<LewdMarkCompleteCurse>().Count() == complete,
            "third visit refusal changes page without settling reward");
        await third.CurrentOptions[0].Chosen();
        check(player.Deck.Cards.OfType<LewdMarkSpreadCurse>().Count() == spread
            && player.Deck.Cards.OfType<LewdMarkCompleteCurse>().Count() == complete + 1
            && CorruptionQuery.Get(run) == 1
            && Corruption.Handle.Get(run).MassageAppointmentAct == 0,
            "final page replaces mark and ends appointment chain");

        CardModel? minorToRemove = player.Deck.Cards.OfType<LewdMarkMinorCurse>().FirstOrDefault();
        if (minorToRemove != null) await CardPileCmd.RemoveFromDeck(minorToRemove, showPreview: false);
        MassageShopSecond missingMinor = await Start<MassageShopSecond>(player);
        spread = player.Deck.Cards.OfType<LewdMarkSpreadCurse>().Count();
        await missingMinor.CurrentOptions[0].Chosen();
        check(player.Deck.Cards.OfType<LewdMarkSpreadCurse>().Count() == spread + 1,
            "second special still grants spread mark when minor mark is missing");
        MassageAppointmentService.Cancel(run);
    }

    private static async Task<T> Start<T>(Player player) where T : EventModel
    {
        T model = (T)ModelDb.Event<T>().ToMutable();
        await model.BeginEvent(player, null, isPreFinished: false);
        return model;
    }

    private static void CheckAppointmentLifecycle(Player source,
        Action<bool, string> check)
    {
        Player maiden = Player.CreateForNewRun<MaidenSuccubusCharacter>(
            source.UnlockState, source.NetId + 1000);
        RunState isolated = RunState.CreateForTest([maiden]);
        isolated.CurrentActIndex = 1;
        MassageAppointmentService.Schedule(isolated, 2);
        check(!MassageAppointmentService.IsDue(isolated, MapPointType.Unknown),
            "Act 2 first unknown is not replaced");
        MassageAppointmentService.RoomCreated(isolated, MapPointType.Unknown,
            new MapRoom());
        check(Corruption.Handle.Get(isolated).ActTwoUnknownRoomsVisited == 1
              && MassageAppointmentService.EventFor(isolated, MapPointType.Unknown)
                  is MassageShopSecond,
            "Act 2 second unknown selects booked event");
        EventModel second = ModelDb.Event<MassageShopSecond>();
        MassageAppointmentService.RoomCreated(isolated, MapPointType.Unknown,
            new EventRoom(second));
        check(Corruption.Handle.Get(isolated).MassageAppointmentAct == 0
              && isolated.VisitedEventIds.Contains(second.Id),
            "successful Act 2 appointment is consumed and recorded");

        isolated.CurrentActIndex = 2;
        MassageAppointmentService.Schedule(isolated, 3);
        MassageAppointmentService.RoomCreated(isolated, MapPointType.Unknown,
            new MapRoom());
        check(Corruption.Handle.Get(isolated).ActThreeUnknownRoomsVisited == 1
              && MassageAppointmentService.EventFor(isolated, MapPointType.Unknown)
                  is MassageShopThird,
            "Act 3 second unknown selects booked event");
        EventModel third = ModelDb.Event<MassageShopThird>();
        MassageAppointmentService.RoomCreated(isolated, MapPointType.Unknown,
            new EventRoom(third));
        check(Corruption.Handle.Get(isolated).MassageAppointmentAct == 0
              && isolated.VisitedEventIds.Contains(third.Id),
            "successful Act 3 appointment is consumed and recorded");

        Player ironclad = Player.CreateForNewRun<Ironclad>(
            source.UnlockState, source.NetId + 1001);
        RunState foreign = RunState.CreateForTest([ironclad]);
        foreign.CurrentActIndex = 1;
        MassageAppointmentService.Schedule(foreign, 2);
        MassageAppointmentService.RoomCreated(foreign, MapPointType.Unknown,
            new MapRoom());
        check(Corruption.Handle.Get(foreign).ActTwoUnknownRoomsVisited == 0
              && MassageAppointmentService.EventFor(foreign, MapPointType.Unknown) == null,
            "other character cannot advance or receive appointment");
    }
}
#endif
