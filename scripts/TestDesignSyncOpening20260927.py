"""Opening narration fidelity and lifecycle wiring. Actual UI is an explicit hand-test gate."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read

NAMES = "Pride Greed Lust Envy Gluttony Wrath Sloth Humility Generosity Chastity Benevolence Temperance Patience Diligence".split()
PREFIX = "MAIDEN_SUCCUBUS_ROUTE_OPENING."


class OpeningContracts(unittest.TestCase):
    def test_all_supplied_narratives_exact_including_punctuation(self):
        source = read("docs/GODDESS_TRIAL_NARRATIVE.md")
        loc = json.loads(read("MaidenSuccubus/localization/zhs/events.json"))
        common = re.search(r"### 公共开场叙事\s+(.*?)\s+### 双栏", source, re.S).group(1)
        self.assertEqual(common, loc[PREFIX + "common"])
        entries = re.findall(r"### ([^\n]+)\s+\*\*选项风味\*\*\s+(.*?)\s+\*\*选择后全屏叙事\*\*\s+(.*?)\s+\*\*按钮：\*\*", source, re.S)
        self.assertEqual(len(entries), 14)
        for name, (_, flavor, story) in zip(NAMES, entries):
            self.assertEqual(flavor.splitlines()[0], loc[PREFIX + name + ".flavor"])
            self.assertEqual(story, loc[PREFIX + name + ".story"])

    def test_current_rules_and_initial_reward_not_narrative_numbers(self):
        ui = read("src/UI/FourthRouteOpeningScreen.cs")
        for value in ("QuestText(quest, 1)", "CreateRelicPreview(quest, 0)", "CreateRelicPreview(quest, 1)",
                      "reward.DynamicDescription.GetFormattedText()"):
            self.assertIn(value, ui)
        self.assertNotIn("取得这件遗物的碎片", ui)

    def test_actual_native_await_extended_not_async_postfix(self):
        code = read("src/Patches/FourthRouteOpeningPatch.cs")
        for value in ("AsyncStateMachineAttribute", 'GetMethod("MoveNext"', "sites.Length != 1", "OpCodes.Ldfld",
                      "await originalWait", "await FourthRouteOpeningScreen.Show", "Safe.Run("):
            self.assertIn(value, code)
        self.assertNotIn("HarmonyPostfix", code)
        self.assertNotIn(".Invoke(", code)

    def test_local_single_player_first_ancient_gates(self):
        code = read("src/Patches/FourthRouteOpeningPatch.cs")
        for value in ("TestMode.IsOn", "is not Neow", "is not false", "!LocalContext.IsMe(player)",
                      "!FourthRouteLifecycle.IsEligible(player)", "run.Players.Count != 1", "run.CurrentActIndex != 0",
                      "!FourthRouteOpeningService.NeedsOpening(run)"):
            self.assertIn(value, code)

    def test_scene_lifetime_modal_input_and_font(self):
        code = read("src/UI/FourthRouteOpeningScreen.cs")
        for value in ("IScreenContext", "MouseFilterEnum.Stop", "container.OpenModal != null", "AddBlockingScreen(this)",
                      "RemoveBlockingScreen(this)", "_ExitTree()", "_completion.TrySetResult(false)",
                      "if (!_isCurrent()) Close(false)", "AutoSizeEnabled = false", "MinFontSize = 24, MaxFontSize = 24",
                      "ScrollContainer", 'IsActionPressed("ui_cancel")'):
            self.assertIn(value, code)

    def test_saved_offer_receipt_and_no_reroll_after_choice(self):
        code = read("src/Acts/FourthRouteOpeningService.cs")
        self.assertIn("opening.HasOffers || opening.Chosen != null || opening.Completed", code)
        self.assertIn("opening.Offer(", code)
        self.assertIn("gate.Busy", code)
        self.assertLess(code.index("data.FourthRouteQuestId = quest.ToString()"), code.index("await FourthRouteProgressService.EnsureDormantRelic"))
        self.assertIn("FourthRouteOpeningState? FourthRouteOpening", read("src/Data/M5Progress.cs"))
        self.assertIn("state.FourthRouteOpening = null", read("src/ConsoleCommands/M6ConsoleCmds.cs"))

    def test_choice_locked_and_story_continue_only(self):
        code = read("src/UI/FourthRouteOpeningScreen.cs")
        story = code.split("private async Task Confirm", 1)[1].split("private Button ActionButton", 1)[0]
        self.assertIn("_busy = true", story)
        self.assertIn("button.Disabled = true", story)
        self.assertEqual(story.count("ActionButton("), 1)
        self.assertIn('TextFor("continue")', story)
        self.assertIn("FourthRouteOpeningService.Finish(_run)", story)

    def test_executable_production_rules_and_guarded_actual_models(self):
        self.assertIn("../../src/Acts/FourthRouteOpeningState.cs", read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        code = read("src/ConsoleCommands/DesignRouteOpeningTestConsoleCmd.cs")
        for value in ("#if DEBUG", 'args[0] != "confirm"', "Harmony.GetPatchInfo", "PatchProcessor.GetOriginalInstructions",
                      "await FourthRouteOpeningService.Confirm", "Enum.GetValues<FourthRouteQuest>()",
                      "Player.CreateForNewRun<Ironclad>", "exactly one dormant relic obtained", "TestMode.IsOn = previous"):
            self.assertIn(value, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
