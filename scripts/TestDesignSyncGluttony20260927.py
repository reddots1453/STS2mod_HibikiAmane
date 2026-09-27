"""DS27-05E wiring and exact design text checks; stage rules execute in C#."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


def gluttony():
    return read("src/Relics/FourthRouteRelics.cs").split("public sealed class GluttonyRouteRelic", 1)[1].split(
        "[RegisterRelic", 1)[0]


class GluttonyContracts(unittest.TestCase):
    def test_exact_stage_text_and_order(self):
        design = read("DesignDoc.md").split("#### 暴食\n", 1)[1].split("####", 1)[0]
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        prefix = "MAIDEN_SUCCUBUS_RELIC_GLUTTONY_ROUTE_RELIC.descriptionStage"
        for stage, label in ((1, "残缺"), (2, "完整"), (4, "觉醒")):
            expected = re.search(rf"^{label}：(.*)$", design, re.M).group(1)
            self.assertEqual(expected, loc[prefix + str(stage)])
        self.assertEqual(loc[prefix + "3"], loc[prefix + "4"])

    def test_native_max_hp_and_slot_commands(self):
        code = gluttony()
        for text in ("GluttonyRules.PickupMaxHp(Stage)", "CreatureCmd.GainMaxHp(Owner.Creature, maxHp)",
                     "PlayerCmd.GainMaxPotionCount(GluttonyRules.PickupSlots(Stage), Owner)"):
            self.assertIn(text, code)
        self.assertNotIn("GainMaxHp(Owner.Creature, 5)", code)
        self.assertNotIn("SetMaxAndCurrentHp", code)

    def test_persistent_receipt_precedes_native_side_effects(self):
        code = gluttony()
        self.assertIn("[SavedProperty] public bool PickupEffectGranted", code)
        self.assertIn("Stage == 0 || PickupEffectGranted", code)
        self.assertLess(code.index("PickupEffectGranted = true"), code.index("await CreatureCmd.GainMaxHp"))
        self.assertIn("HasUponPickupEffect => Stage > 0", code)

    def test_potion_owner_not_target_decides_bonus(self):
        hook = gluttony().split("public override Task AfterPotionUsed", 1)[1]
        self.assertIn("GluttonyRules.MaxHpOnPotionUsed(Stage, potion.Owner == Owner)", hook)
        self.assertIn("CreatureCmd.GainMaxHp(Owner.Creature, amount)", hook)
        self.assertNotIn("target ==", hook)
        self.assertNotIn("IsInProgress", hook)

    def test_empty_slots_and_rng_guard(self):
        code = gluttony()
        self.assertIn("GluttonyRules.EmptySlots(Owner.MaxPotionCount, Owner.Potions.Count())", code)
        self.assertLess(code.index("if (empty == 0) return"), code.index("PotionFactory.CreateRandomPotionsOutOfCombat"))
        self.assertIn("Owner.RunState.Rng.CombatPotionGeneration", code)
        self.assertIn("PotionCmd.TryToProcure(potion.ToMutable(), Owner)", code)
        self.assertNotIn("PotionCmd.Discard", code)

    def test_no_reward_on_acquire_discard_or_remove(self):
        code = gluttony()
        for callback in ("AfterPotionProcured", "AfterPotionDiscarded", "AfterRemoved"):
            self.assertNotIn(callback, code)
        self.assertIn("GluttonyRules.FillOnPickup(Stage)", code)

    def test_executable_suite_links_production_and_capacity_boundaries(self):
        self.assertIn("../../src/Core/Relics/GluttonyRules.cs",
                      read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        tests = read("tests/DesignSyncContracts/Program.cs")
        for text in ("GluttonyRules.PickupMaxHp", "GluttonyRules.PickupSlots", "GluttonyRules.FillOnPickup",
                     "GluttonyRules.MaxHpOnPotionUsed", "GluttonyRules.EmptySlots", "no subtraction overflow"):
            self.assertIn(text, tests)

    def test_engine_command_is_opt_in_and_asserts_actual_native_results(self):
        tests = read("src/ConsoleCommands/DesignGluttonyTestConsoleCmd.cs")
        for text in ("#if DEBUG", 'args[0] != "confirm"', "issuingPlayer.RunState.Players.Count != 1",
                     "CombatManager.Instance.IsInProgress", "RelicCmd.Obtain", "SavedProperties.From",
                     "juice.OnUseWrapper", "other owner's potion targeting us cannot trigger",
                     "full inventory does not advance generation RNG", "TestMode.IsOn = previousTestMode",
                     "[DS27GluttonyTest] FAIL"):
            self.assertIn(text, tests)


if __name__ == "__main__":
    unittest.main(verbosity=2)
