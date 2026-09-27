"""DS27-05B wiring tests. Executable phase/count contracts live in the C# suite."""
import json
import unittest
from TestDesignSyncNeutral20260927 import read


class ThreeTrialContracts(unittest.TestCase):
    def test_full_design_contract_including_no_direct_upgrade(self):
        design = read("DesignDoc.md")
        for text in ("第一试炼：进行2场精英战斗。", "第一试炼：持有至少200金币。",
                     "第二试炼：持有至少300金币。", "第三试炼：持有至少500金币。",
                     "第三试炼：以0点欲望的状态结束2场战斗。", "第三试炼：在牌组中再加入5张牌。",
                     "第三试炼：跳过3次卡牌奖励。", "第一试炼：在火堆锻造2次。",
                     "购买碎片和献祭只负责开启下一项路线试炼"):
            self.assertIn(text, design)

    def test_progress_and_rewards_use_saved_production_phase(self):
        code = read("src/Acts/FourthRouteProgress.cs")
        for text in ("FourthRouteTrialRules.Add", "FourthRouteTrialRules.Claim", "FourthRouteTrialRules.SetGold",
                     "FourthRouteTrialRules.RewardStage", "FourthRouteTrialRules.FromLegacy",
                     "if (gate.Busy) return", "finally { gate.Busy = false; }"):
            self.assertIn(text, code)
        self.assertIn("FourthRouteTrialState? FourthRouteTrial", read("src/Data/M5Progress.cs"))
        self.assertNotIn("public static async Task AdvanceStage", code)
        self.assertIn("state.FourthRouteRewardCorruptionApplied = false", code)

    def test_fragment_stays_visible_and_does_not_upgrade(self):
        fragment = read("src/Relics/FourthRouteRelics.cs").split("public sealed class FourthRouteFragmentRelic", 1)[1].split(
            "public sealed class PrideRouteRelic", 1)[0]
        self.assertIn("UnlockSecondTrial(Owner)", fragment)
        self.assertIn("RouteTitleKey", fragment)
        self.assertNotIn("RelicCmd.Remove", fragment)
        service = read("src/Acts/FourthRouteProgress.cs")
        self.assertIn("if (targetStage == 2)", service)
        self.assertIn("await RelicCmd.Remove(fragment)", service)
        self.assertIn("? 100m : cost", read("src/Acts/FourthRouteLifecycle.cs"))
        self.assertIn("state.FourthRouteFragmentOffered", read("src/Patches/FourthRouteMerchantPatch.cs"))

    def test_sacrifice_counts_actual_removal_and_matching_direction(self):
        code = read("src/RestSite/SacrificeRestSiteOption.cs")
        for text in ("confirmed.Count(card => !Owner.Deck.Cards.Contains(card))", "removedCount > 0",
                     "sacrificedCards.All", "FourthTrialPhase.Sacrifice", "PreserveRemainingOptionsOnce",
                     "UnlockThirdTrial(Owner, removedCount, true)"):
            self.assertIn(text, code)
        self.assertLess(code.index("await CardPileCmd.RemoveFromDeck(confirmed)"), code.index("UnlockThirdTrial"))
        self.assertNotIn("AdvanceStage", code)

    def test_dormant_forms_are_effectless_at_hook_boundaries(self):
        code = read("src/Relics/FourthRouteRelics.cs")
        self.assertIn("Math.Clamp(value, 0, 4)", code)
        for name in ("Pride", "Greed", "Lust", "Envy", "Gluttony", "Wrath", "Sloth", "Humility",
                     "Generosity", "Chastity", "Benevolence", "Temperance", "Diligence"):
            section = code.split(f"public sealed class {name}RouteRelic", 1)[1].split("[RegisterRelic", 1)[0]
            self.assertTrue("Stage == 0" in section or "Stage is 0" in section, name)
        self.assertIn("VirtueCombatRules.PatienceAtCombatStart(Stage) ? CreateHoly()", code)
        self.assertIn("PatienceAtCombatStart(int stage) => stage is 1 or 2",
                      read("src/Core/Relics/VirtueCombatRules.cs"))
        self.assertIn("relic.Stage = 0", read("src/Acts/FourthRouteProgress.cs"))

    def test_counts_use_current_trial_and_qualified_sources(self):
        code = read("src/Acts/FourthRouteLifecycle.cs")
        self.assertIn("FourthRouteTrialRules.CountsCombat", code)
        self.assertIn("AfterGoldGained", code)
        self.assertIn("!isMimicked", code)
        patches = read("src/Patches/FourthRouteQuestPatches.cs")
        self.assertIn("card.Pile?.Type != PileType.Deck", patches)
        self.assertIn("Counted.TryGetValue(__instance", patches)
        self.assertNotIn("nameof(RelicReward.OnSkipped)", patches)
        self.assertNotIn('"OnProceedButtonReleased"', patches)

    def test_titles_progress_and_preview_not_revealing_next_trial(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        prefix = "MAIDEN_SUCCUBUS_RELIC_FOURTH_ROUTE_STAGE"
        self.assertEqual("{Stage}的{Relic}", loc[prefix + ".title"])
        for stage, title in ((0, "沉睡"), (1, "残缺"), (2, "完整"), (4, "觉醒")):
            self.assertEqual(title, loc[f"{prefix}_{stage}.title"])
        ui = read("src/UI/FourthRouteSelectionScreen.cs")
        self.assertIn("试炼的奖赏", ui)
        self.assertIn("_rewardStage", ui)
        self.assertNotIn("始源", ui)
        desc = read("src/Patches/TwinSoulChaliceDescriptionPatch.cs")
        self.assertIn("routeRelic.Owner is { } owner", desc)
        self.assertIn("FourthRouteProgressService.ProgressText", desc)

    def test_executable_suite_covers_all_trials_and_persistence(self):
        project = read("tests/DesignSyncContracts/DesignSyncContracts.csproj")
        self.assertIn("../../src/Acts/FourthRouteTrialRules.cs", project)
        tests = read("tests/DesignSyncContracts/FourthRouteTrialContracts.cs")
        for part in ("targets.Count == 14", "trial <= 3", "RoundTrip", "duplicate claim rejected",
                     "opposite sealed direction no unlock", "gold is state not accumulated events",
                     "legacy preserves earned reward", "unlocked trial starts at zero"):
            self.assertIn(part, tests)


if __name__ == "__main__":
    unittest.main(verbosity=2)
