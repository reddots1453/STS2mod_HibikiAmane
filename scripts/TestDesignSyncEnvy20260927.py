"""Envy wiring/text checks, alongside production rule and opt-in native model tests."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


def relic():
    return read("src/Relics/FourthRouteRelics.cs").split("public sealed class EnvyRouteRelic", 1)[1].split("[RegisterRelic", 1)[0]


class EnvyContracts(unittest.TestCase):
    def test_exact_stage_text(self):
        design = read("DesignDoc.md").split("#### 嫉妒\n", 1)[1].split("####", 1)[0]
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        prefix = "MAIDEN_SUCCUBUS_RELIC_ENVY_ROUTE_RELIC.descriptionStage"
        for stage, label in ((1, "残缺"), (2, "完整"), (4, "觉醒")):
            expected = re.search(rf"^{label}：(.*)$", design, re.M).group(1)
            self.assertEqual(expected, loc[prefix + str(stage)].replace("{energyPrefix:maidenEnergyIcons(1)}", "1费"))
        self.assertEqual(loc[prefix + "3"], loc[prefix + "4"])

    def test_native_change_type_not_static_type_or_positive_only(self):
        code = relic()
        self.assertIn("amount != 0 && power.GetTypeForAmount(amount) == PowerType.Debuff", code)
        self.assertNotIn("amount <= 0", code)
        self.assertNotIn("power.Type !=", code)
        self.assertNotIn("power.Owner.Side", code)

    def test_caster_and_combat_match(self):
        for text in ("applier == Owner.Creature", "Owner.Creature.CombatState != null",
                     "power.Owner.CombatState == Owner.Creature.CombatState"):
            self.assertIn(text, relic())

    def test_first_reserved_before_commands(self):
        code = relic()
        self.assertLess(code.index("_trigger.TryUse"), code.index("await PlayerCmd.GainEnergy"))
        self.assertIn("if (draw > 0) await CardPileCmd.Draw(context, draw, Owner)", code)
        self.assertIn("Used = true;", read("src/Core/Relics/EnvyTriggerState.cs"))

    def test_own_start_not_side_end_resets(self):
        code = relic()
        self.assertIn("BeforeSideTurnStart", code)
        self.assertIn("participants.Contains(Owner.Creature)", code)
        self.assertNotIn("AfterSideTurnEnd", code)
        self.assertIn("stage is 3 or 4 && ownTurn", read("src/Core/Relics/EnvyTriggerState.cs"))
        self.assertIn("BeforeCombatStart() { _trigger = default;", code)
        self.assertIn("AfterCombatEnd(CombatRoom room) { _trigger = default;", code)

    def test_saved_state_and_production_rules(self):
        self.assertIn("[SavedProperty]\n    public bool UsedThisWindow", relic())
        self.assertIn("../../src/Core/Relics/EnvyTriggerState.cs", read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        tests = read("tests/DesignSyncContracts/Program.cs")
        for text in ("EnvyTriggerState", "nonqualifying change cannot consume first use",
                     "only awakened refreshes on own next turn", "clone reset cannot change original receipt"):
            self.assertIn(text, tests)

    def test_actual_model_test_coverage(self):
        code = read("src/ConsoleCommands/DesignEnvyTestConsoleCmd.cs")
        for text in ("PowerCmd.Apply<WeakPower>", "PowerCmd.Apply<StrengthPower>", "PowerCmd.Apply<ArtifactPower>",
                     "artifact-blocked application gives no reward", "PowerCmd.ModifyAmount(choice, weak, -1",
                     "negative strength delta counts regardless of remaining buff total",
                     "own self-target harmful application is not excluded by side",
                     "SavedProperties.From(relic)!.Fill(restored)"):
            self.assertIn(text, code)

    def test_destructive_debug_entry_is_guarded(self):
        code = read("src/ConsoleCommands/DesignEnvyTestConsoleCmd.cs")
        for text in ("#if DEBUG", 'args[0] != "confirm"', "issuingPlayer.RunState.Players.Count != 1",
                     "!CombatManager.Instance.IsInProgress", "_running", "TestMode.IsOn = previousTestMode"):
            self.assertIn(text, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
