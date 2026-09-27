"""Generosity pickup wiring/text contracts; runtime command is compiled, not run here."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


def relic():
    return read("src/Relics/FourthRouteRelics.cs").split("public sealed class GenerosityRouteRelic", 1)[1].split("[RegisterRelic", 1)[0]


class GenerosityContracts(unittest.TestCase):
    def test_exact_stage_text(self):
        design = read("DesignDoc.md").split("#### 慷慨\n", 1)[1].split("####", 1)[0]
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        prefix = "MAIDEN_SUCCUBUS_RELIC_GENEROSITY_ROUTE_RELIC.descriptionStage"
        for stage, label in ((1, "残缺"), (2, "完整"), (4, "觉醒")):
            self.assertEqual(re.search(rf"^{label}：(.*)$", design, re.M).group(1), loc[prefix + str(stage)])
        self.assertEqual(loc[prefix + "3"], loc[prefix + "4"])

    def test_native_removal_picker_and_stage_count(self):
        for text in ("Stage is 1 or 2", "int count = Stage", "CardSelectorPrefs.RemoveSelectionPrompt, count",
                     "Cancelable = false", "CardSelectCmd.FromDeckForRemoval", "CardPileCmd.RemoveFromDeck(cards)"):
            self.assertIn(text, relic())

    def test_saved_receipt_reserved_before_await(self):
        code = relic()
        self.assertIn("[SavedProperty] public bool PickupEffectGranted", code)
        self.assertIn("if (!HasUponPickupEffect || PickupEffectGranted) return", code)
        self.assertLess(code.index("PickupEffectGranted = true"), code.index("await CardSelectCmd"))

    def test_revalidation_and_bounded_unique_selection(self):
        for text in ("card.Owner == Owner", "card.Pile?.Type == PileType.Deck", "card.IsRemovable", ".Distinct().Take(count)"):
            self.assertIn(text, relic())

    def test_actual_commands_and_eternal_short_deck_cases(self):
        code = read("src/ConsoleCommands/DesignGenerosityTestConsoleCmd.cs")
        for text in ("RelicCmd.Obtain", "new[] { 0, 1, 2, 3, 4 }", "new[] { 0, 1, 2, 4 }", "CreateCard<AscendersBane>",
                     "native pickup removes correct stage count or all available", "eternal card never removed",
                     "selected cards actually removed", "unselected cards retained"):
            self.assertIn(text, code)

    def test_async_selection_and_native_saved_properties(self):
        code = read("src/ConsoleCommands/DesignGenerosityTestConsoleCmd.cs")
        for text in ("SavedProperties.From(relic)!.Fill(restored)", "saved receipt prevents another picker",
                     "SetupForAsyncCardSelection", "reentrant pickup does not open second picker",
                     "stale removed selection ignored while valid selection resolves",
                     "duplicate and oversized selection cannot exceed removal count",
                     "replacement gets new receipt without restoring earlier removals"):
            self.assertIn(text, code)

    def test_no_skip_hook_substituted_for_offering(self):
        self.assertNotIn("OnSkipped", relic())
        self.assertNotIn("AfterRoom", relic())
        self.assertNotIn("TrackSimple", relic())

    def test_debug_command_guarded(self):
        code = read("src/ConsoleCommands/DesignGenerosityTestConsoleCmd.cs")
        for text in ("#if DEBUG", 'args[0] != "confirm"', "issuingPlayer.RunState.Players.Count != 1",
                     "CombatManager.Instance.IsInProgress", "_running", "TestMode.IsOn = previousTestMode"):
            self.assertIn(text, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
