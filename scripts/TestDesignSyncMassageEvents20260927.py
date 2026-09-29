"""Static wiring checks for the three scheduled MassageShop events.

Real room selection and option commands are covered by ms_test_events confirm;
these checks do not claim that game scenario has been executed.
"""

import json
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
DOC = (ROOT / "DesignDoc.md").read_text(encoding="utf-8")
EVENTS = (ROOT / "src/Events/MassageShopEvents.cs").read_text(encoding="utf-8")
SERVICE = (ROOT / "src/Events/MassageAppointmentService.cs").read_text(encoding="utf-8")
PATCHES = (ROOT / "src/Patches/MassageAppointmentPatches.cs").read_text(encoding="utf-8")
STATE = (ROOT / "src/Data/CorruptionState.cs").read_text(encoding="utf-8")
GREED = (ROOT / "src/Patches/GreedShopPatches.cs").read_text(encoding="utf-8")
RUNNER = (ROOT / "src/ConsoleCommands/DesignEventTestConsoleCmd.cs").read_text(encoding="utf-8")
CONTRACT = (ROOT / "src/ConsoleCommands/DesignMassageEventContract.cs").read_text(encoding="utf-8")
LOC = json.loads((ROOT / "MaidenSuccubus/localization/zhs/events.json").read_text(encoding="utf-8"))


class MassageEventContract(unittest.TestCase):
    def test_all_three_events_are_ready_and_registered(self):
        for number, suffix in [(4, "First"), (5, "Second"), (6, "Third")]:
            self.assertIn(f"EVENT-NEW-00{number} · READY", DOC)
            self.assertIn(f"public sealed class MassageShop{suffix}", EVENTS)
        self.assertEqual(EVENTS.count("[RegisterSharedEvent]"), 3)
        self.assertIn("public override bool IsAllowed(IRunState runState) => false;", EVENTS)

    def test_mandatory_pages_and_option_text_exist(self):
        required = {
            "FIRST": ("GENERAL", "SPECIAL", "REFUSE"),
            "SECOND": ("SPECIAL", "REFUSE", "REFUSE_LOCKED"),
            "THIRD": ("SPECIAL", "REFUSE"),
        }
        for stage, options in required.items():
            prefix = f"MAIDEN_SUCCUBUS_EVENT_MASSAGE_SHOP_{stage}"
            self.assertIn(prefix + ".title", LOC)
            self.assertIn(prefix + ".pages.INITIAL.description", LOC)
            for option in options:
                key = prefix + f".pages.INITIAL.options.{option}"
                self.assertIn(key + ".title", LOC)
                self.assertIn(key + ".description", LOC)
            self.assertIn(prefix + ".pages.SPECIAL.description", LOC)
        self.assertIn("MAIDEN_SUCCUBUS_EVENT_MASSAGE_SHOP_THIRD.pages.CANNOT_LEAVE.description", LOC)

    def test_appointment_is_persisted_and_has_priority_over_greed(self):
        for field in ("MassageAppointmentAct", "ActTwoUnknownRoomsVisited", "ActThreeUnknownRoomsVisited"):
            self.assertIn("public int " + field, STATE)
        self.assertIn("actTwoUnknownRooms >= 1", SERVICE)
        self.assertIn("actThreeUnknownRooms >= 1", SERVICE)
        self.assertIn("state.MassageAppointmentAct = 0", SERVICE)
        self.assertIn("[HarmonyPrefix, HarmonyPriority(Priority.First)]", PATCHES)
        self.assertIn("__result = RoomType.Event", PATCHES)
        self.assertIn("ref AbstractModel? model", PATCHES)
        self.assertIn("[HarmonyPrefix, HarmonyPriority(Priority.Last)]", GREED)
        self.assertIn("reserved: !__runOriginal", GREED)

    def test_fulfilled_fixed_event_is_recorded_like_native_event(self):
        self.assertIn("EventModel? expected = EventFor(run, pointType);", SERVICE)
        self.assertIn("eventRoom.ModelId == expected.Id", SERVICE)
        self.assertIn("if (fulfilled) run.AddVisitedEvent(expected!);", SERVICE)
        self.assertIn("if (fulfilled) state.MassageAppointmentAct = 0;", SERVICE)

    def test_all_three_package_effects_have_game_scenarios(self):
        self.assertIn("DesignMassageEventContract.Run(player, Check)", RUNNER)
        for effect in ("general package pays 100", "first special package pays 50",
                       "second visit refusal lowers corruption", "second special replaces mark",
                       "third visit refusal changes page", "final page replaces mark",
                       "minor mark is missing"):
            self.assertIn(effect, CONTRACT)

    def test_isolated_run_checks_cross_act_appointment_consumption(self):
        for token in (
            "CheckAppointmentLifecycle(player, check)",
            "RunState.CreateForTest([maiden])",
            "Act 2 second unknown selects booked event",
            "successful Act 2 appointment is consumed and recorded",
            "Act 3 second unknown selects booked event",
            "successful Act 3 appointment is consumed and recorded",
            "other character cannot advance or receive appointment",
        ):
            self.assertIn(token, CONTRACT)


if __name__ == "__main__":
    unittest.main()
