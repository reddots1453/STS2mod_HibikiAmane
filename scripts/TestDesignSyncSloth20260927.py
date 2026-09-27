"""Sloth wiring and exact text; production state transitions run in DesignSyncContracts."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


def relic():
    return read("src/Relics/FourthRouteRelics.cs").split("public sealed class SlothRouteRelic", 1)[1].split("[RegisterRelic", 1)[0]


class SlothContracts(unittest.TestCase):
    def test_exact_stage_text_with_energy_icons(self):
        design = read("DesignDoc.md").split("#### 懒惰\n", 1)[1].split("###", 1)[0]
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        prefix = "MAIDEN_SUCCUBUS_RELIC_SLOTH_ROUTE_RELIC.descriptionStage"
        for stage, label in ((1, "残缺"), (2, "完整"), (4, "觉醒")):
            expected = re.search(rf"^{label}：(.*)$", design, re.M).group(1)
            actual = re.sub(r"\{energyPrefix:maidenEnergyIcons\((\d)\)\}", r"\1点能量", loc[prefix + str(stage)])
            self.assertEqual(expected, re.sub(r"\[/?gold\]", "", actual))
        self.assertEqual(loc[prefix + "3"], loc[prefix + "4"])
        self.assertIn("[gold]格挡[/gold]", loc[prefix + "4"])
        self.assertIn("AdditionalHoverTips => Stage is 3 or 4", relic())
        self.assertIn("HoverTipFactory.Static(StaticHoverTip.Block)", relic())

    def test_payment_hook_not_play_valuation(self):
        code = relic()
        self.assertIn("AfterEnergySpent(CardModel card, int amount)", code)
        self.assertIn("_turn.RecordPayment(amount, Stage > 0 && card.Owner == Owner", code)
        self.assertNotIn("AfterCardPlayed", code)
        self.assertNotIn("EnergyValue", code)

    def test_owner_and_participant_isolation(self):
        code = relic()
        self.assertIn("combatState == Owner.Creature.CombatState", code)
        self.assertEqual(2, code.count("participants.Contains(Owner.Creature)"))
        self.assertIn("_turn.TakeEnergy(Stage, player == Owner", code)
        self.assertNotIn("RoundNumber", code)

    def test_both_combat_boundaries_clear(self):
        code = relic()
        self.assertIn("BeforeCombatStart() { _turn = default;", code)
        self.assertIn("AfterCombatEnd(CombatRoom room) { _turn = default;", code)
        self.assertIn("_turn.StartTurn", code)

    def test_saved_integer_and_value_clone(self):
        code = relic()
        self.assertIn("[SavedProperty]\n    public int EnergySpentThisTurn", code)
        self.assertIn("[SavedProperty]\n    public bool TriggeredForNextTurn", code)
        self.assertIn("[SavedProperty]\n    public bool TurnEndResolved", code)
        self.assertIn("internal struct SlothTurnState", read("src/Core/Relics/SlothTurnState.cs"))
        self.assertNotIn("decimal", code)

    def test_production_state_suite(self):
        self.assertIn("../../src/Core/Relics/SlothTurnState.cs", read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        code = read("tests/DesignSyncContracts/Program.cs")
        for text in ("sloth threshold inclusive two", "duplicate end never gives block twice",
                     "foreign reset preserves reward", "separate payments accumulate above threshold",
                     "battle reset clears pending reward", "large spending cannot wrap into eligibility"):
            self.assertIn(text, code)

    def test_real_payments_replay_save_and_turn_tests(self):
        code = read("src/ConsoleCommands/DesignSlothTestConsoleCmd.cs")
        for text in ("card.SpendResources()", "card.OnPlayWrapper", "CardCmd.Enchant<Glam>",
                     "free auto-play ignores nominal cost", "X costs count actual paid two",
                     "SavedProperties.From(relic)!.Fill(restored)", "same-round next own turn receives bonus",
                     "foreign energy reset preserves pending bonus", "battle end clears all state"):
            self.assertIn(text, code)

    def test_destructive_debug_entry_guards_and_cleanup(self):
        code = read("src/ConsoleCommands/DesignSlothTestConsoleCmd.cs")
        for text in ("#if DEBUG", 'args[0] != "confirm"', "issuingPlayer.RunState.Players.Count != 1",
                     "!CombatManager.Instance.IsInProgress", "_running", "TestMode.IsOn = previousTestMode",
                     "finally { combat.RoundNumber = originalRound; }"):
            self.assertIn(text, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
