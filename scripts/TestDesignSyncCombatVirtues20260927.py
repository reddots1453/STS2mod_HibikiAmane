"""DS27-05D source/UI contracts, complementary to executed production stage rules."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


def relic(name):
    return read("src/Relics/FourthRouteRelics.cs").split(
        f"public sealed class {name}RouteRelic", 1)[1].split("[RegisterRelic", 1)[0]


class CombatVirtueContracts(unittest.TestCase):
    def test_stage_text_matches_design_punctuation(self):
        design = read("DesignDoc.md")
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        for name, title in (("TEMPERANCE", "节制"), ("PATIENCE", "耐心")):
            section = design.split(f"#### {title}\n", 1)[1].split("####", 1)[0]
            prefix = f"MAIDEN_SUCCUBUS_RELIC_{name}_ROUTE_RELIC.descriptionStage"
            for stage, label in ((1, "残缺"), (2, "完整"), (4, "觉醒")):
                expected = re.search(rf"^{label}：(.*)$", section, re.M).group(1)
                self.assertEqual(expected, re.sub(r"\[/?gold\]", "", loc[prefix + str(stage)]))
            self.assertEqual(loc[prefix + "3"], loc[prefix + "4"])

    def test_temperance_optional_selection_and_clear_prompt(self):
        code = relic("Temperance")
        for part in ("PileType.Draw.GetPile(Owner)", "CardSelectCmd.FromCombatPile", "SelectionPrefs(Stage)",
                     "0, VirtueCombatRules.TemperanceMaximum(stage)", "pile.Cards.Count == 0"):
            self.assertIn(part, code)
        loc = json.loads(read("MaidenSuccubus/localization/zhs/card_selection.json"))
        prompt = loc["MAIDEN_SUCCUBUS_TEMPERANCE_ADD_EXHAUST"]
        self.assertIn("至多{MaxCount}", prompt)
        self.assertIn("附加消耗", prompt)
        self.assertIn("可以不选", prompt)
        self.assertNotIn("ExhaustSelectionPrompt", code)

    def test_no_exhaust_now_or_extra_enchantment(self):
        code = relic("Temperance")
        self.assertIn("card.AddKeyword(CardKeyword.Exhaust)", code)
        for part in ("CardPileCmd.Exhaust", "CardCmd.Enchant", "CombatEnchantmentCmd", "Swift"):
            self.assertNotIn(part, code)
        for part in ("card.Owner != Owner || card.Pile != pile", "Owner.Creature.CombatState != combat"):
            self.assertIn(part, code)

    def test_patience_lifecycle_separates_first_turn_triggers(self):
        code = relic("Patience")
        self.assertIn("VirtueCombatRules.PatienceAtCombatStart(Stage)", code)
        self.assertIn("VirtueCombatRules.PatienceAtTurnStart(Stage, player == Owner)", code)
        self.assertIn("Owner.PlayerCombatState == null", code)
        self.assertIn("if (pool.Count == 0) return", code)

    def test_discount_uses_native_relative_until_played_only(self):
        code = relic("Patience")
        self.assertIn("card.EnergyCost.AddUntilPlayed(-1)", code)
        self.assertIn("card.InvokeEnergyCostChanged()", code)
        for part in ("CardCmd.Upgrade", "SetThisTurn", "AddThisTurn", "SecondaryResource", "SetUntilPlayed"):
            self.assertNotIn(part, code)
        self.assertLess(code.index("PrepareGeneratedCard(card, Stage)"), code.index("AddGeneratedCardToCombat"))

    def test_generation_keeps_legal_pool_rng_and_native_insertion(self):
        code = relic("Patience")
        for part in ("ModelDb.CardPool<MSHolyCardPool>()", "GetUnlockedCards", "CardMultiplayerConstraint",
                     "card.CanBeGeneratedInCombat", "Rng.CombatCardGeneration.NextInt(pool.Count)",
                     "combat.CreateCard(canonical, Owner)", "AddGeneratedCardToCombat(card, PileType.Hand, Owner)"):
            self.assertIn(part, code)
        self.assertNotIn("RunState.CreateCard", code)

    def test_production_rules_execute_in_contract_suite(self):
        self.assertIn("../../src/Core/Relics/VirtueCombatRules.cs",
                      read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        code = read("tests/DesignSyncContracts/Program.cs")
        for part in ("VirtueCombatRules.TemperanceMaximum", "VirtueCombatRules.PatienceAtCombatStart",
                     "VirtueCombatRules.PatienceAtTurnStart", "VirtueCombatRules.PatienceDiscount",
                     "first turn no double generation", "four-turn cumulative generation"):
            self.assertIn(part, code)

    def test_engine_probe_not_automatic_and_restores_test_mode(self):
        code = read("src/ConsoleCommands/DesignCombatVirtueTestConsoleCmd.cs")
        for part in ("#if DEBUG", 'args[0] != "confirm"', "!CombatManager.Instance.IsInProgress",
                     "issuingPlayer.RunState.Players.Count != 1", "_running", "CardSelectCmd.UseSelector",
                     "SetupForAsyncCardSelection", "EndOfTurnCleanup", "AfterCardPlayedCleanup",
                     "permanent deck untouched", "TestMode.IsOn = priorTestMode", "[DS27CombatVirtueTest] FAIL"):
            self.assertIn(part, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
