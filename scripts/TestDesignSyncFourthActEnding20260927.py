"""ACT4-001 wiring checks; C# suite separately executes production boundary rules."""
import unittest
from TestDesignSyncNeutral20260927 import read


class FourthActEndingContracts(unittest.TestCase):
    def test_design_requires_empty_registration_and_normal_ending(self):
        design = read("DesignDoc.md")
        self.assertIn("第四层暂且空注册", design)
        self.assertIn("在与建筑师对话后正常结束游戏流程，不进入未完成的第四层", design)
        self.assertIn("购买碎片和献祭只负责开启下一项路线试炼", design)

    def test_boss_no_longer_awakens_or_appends(self):
        lifecycle = read("src/Acts/FourthRouteLifecycle.cs")
        victory = lifecycle.split("public override async Task AfterCombatVictory", 1)[1].split(
            "public override async Task AfterCardChangedPiles", 1)[0]
        self.assertNotIn("AdvanceStage", victory)
        self.assertNotIn("FourthActRunAdapter", victory)
        self.assertIn("AddProgress", victory)
        self.assertNotIn("FourthRouteThirdBossDefeated", read("src/Acts/FourthRouteProgress.cs").split(
            "public static async Task AdvanceStage", 1)[1].split("public static bool HasFourthActQualification", 1)[0])

    def test_creation_and_load_only_normalize(self):
        patch = read("src/Patches/FourthActPatch.cs")
        creation = patch.split("public static class FourthActCreationPatch", 1)[1].split(
            "public static class FourthActEndingPatch", 1)[0]
        for part in ("RunState.CreateForNewRun", "RunState.FromSerializable", "Safe.Run",
                     "NormalizePendingActs(__result)"):
            self.assertIn(part, creation)
        self.assertNotIn("EnsureDebugPresent", creation)
        normalize = patch.split("public static bool NormalizePendingActs", 1)[1].split(
            "internal static bool EnsureDebugPresent", 1)[0]
        self.assertIn("p.Character is MaidenSuccubusCharacter", normalize)
        self.assertIn("WithoutPendingPlaceholder", normalize)
        self.assertNotIn("acts.Add", normalize)

    def test_native_transition_is_not_replaced(self):
        patch = read("src/Patches/FourthActPatch.cs")
        ending = patch.split("public static class FourthActEndingPatch", 1)[1].split(
            "public static class FourthActRoutePatch", 1)[0]
        self.assertIn("nameof(RunManager.EnterNextAct)", patch)
        for part in ("public static void Prefix", "Safe.Run", "NormalizePendingActs(state)",
                     "RecordThirdActEnding(state)"):
            self.assertIn(part, ending)
        for part in ("EnterAct(", "WinRun(", "AdvanceStage(", "return false"):
            self.assertNotIn(part, ending)
        observer = ending.split("private static async Task ObserveEnding", 1)[1]
        self.assertLess(observer.index("await original;"), observer.index("Safe.Run"))
        self.assertIn("ReferenceEquals(manager.DebugOnlyGetState(), state)", observer)
        self.assertIn("EventRoom { LocalMutableEvent: TheArchitect }", observer)

    def test_qualification_not_entry_switch(self):
        code = read("src/Acts/FourthRouteProgress.cs")
        self.assertIn("FourthActEntryRules.NormalEntryEnabled && HasFourthActQualification(runState)", code)
        self.assertIn("Enum.IsDefined(quest)", code)
        self.assertIn("state.FourthRouteRelicStage >= 4", code)
        self.assertIn("public static bool NormalEntryEnabled => false", read("src/Acts/FourthActEntryRules.cs"))

    def test_debug_is_single_player_per_call(self):
        code = read("src/Patches/FourthActPatch.cs").split("internal static bool EnsureDebugPresent", 1)[1]
        for part in ("#if DEBUG", "runState.Players.Count != 1", "#else", "return false;"):
            self.assertIn(part, code)
        command = read("src/ConsoleCommands/M6ConsoleCmds.cs")
        self.assertNotIn("FourthActRunAdapter.Enabled =", command)
        self.assertIn("if (!FourthActRunAdapter.EnsureDebugPresent(runState))", command)

    def test_checkpoint_is_saved_and_inspectable_not_relic_upgrade(self):
        code = read("src/Acts/FourthRouteProgress.cs").split("public static void RecordThirdActEnding", 1)[1].split(
            "public static int TargetFor", 1)[0]
        self.assertIn("ShouldRecordEnding", code)
        for field in ("FourthRouteEndingChecked", "FourthRouteEndingEligible"):
            self.assertIn(field, read("src/Data/M5Progress.cs"))
            self.assertIn(field, code)
            self.assertIn(field, read("src/ConsoleCommands/M6ConsoleCmds.cs"))
        self.assertNotIn("AdvanceStage", code)
        self.assertNotIn("RelicCmd", code)

    def test_pure_suite_compiles_production_rules(self):
        project = read("tests/DesignSyncContracts/DesignSyncContracts.csproj")
        self.assertIn("../../src/Acts/FourthActEntryRules.cs", project)
        self.assertIn("../../src/Acts/FourthRouteQuest.cs", project)
        tests = read("tests/DesignSyncContracts/Program.cs")
        for part in ("fourteen route oracle entries", "other mod/order/identity preserved",
                     "migration idempotent", "entered room never retargeted", "source collection unmodified"):
            self.assertIn(part, tests)


if __name__ == "__main__":
    unittest.main(verbosity=2)
