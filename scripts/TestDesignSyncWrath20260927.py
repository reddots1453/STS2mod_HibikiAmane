"""DS27-05G: wiring/exact-text checks, supplemented by production rules and opt-in gameplay tests."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


def relic():
    return read("src/Relics/FourthRouteRelics.cs").split("public sealed class WrathRouteRelic", 1)[1].split("[RegisterRelic", 1)[0]


def enchantment():
    return read("src/Enchantments/MvpEnchantments.cs").split("public sealed class WrathEnchantment", 1)[1].split("[RegisterEnchantment", 1)[0]


class WrathContracts(unittest.TestCase):
    def test_stage_text_exact_design(self):
        design = read("DesignDoc.md").split("#### 愤怒\n", 1)[1].split("####", 1)[0]
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        prefix = "MAIDEN_SUCCUBUS_RELIC_WRATH_ROUTE_RELIC.descriptionStage"
        for stage, label in ((1, "残缺"), (2, "完整"), (4, "觉醒")):
            expected = re.search(rf"^{label}：(.*)$", design, re.M).group(1)
            self.assertEqual(expected, re.sub(r"\[/?gold\]", "", loc[prefix + str(stage)]))
        self.assertEqual(loc[prefix + "3"], loc[prefix + "4"])
        self.assertIn("HoverTipFactory.FromEnchantment<WrathEnchantment>()", relic())

    def test_first_play_only_and_copy_is_reset_before_native_insertion(self):
        code = enchantment()
        self.assertIn("WrathRules.FirstPlayBonus(_used, props.IsPoweredAttack())", code)
        self.assertIn("_used || cardPlay?.Card != Card || Card.CombatState == null", code)
        self.assertLess(code.index("_used = true"), code.index("Card.CreateClone()"))
        self.assertLess(code.index("ResetGeneratedCopy(copy)"), code.index("AddGeneratedCardToCombat"))
        self.assertIn("PileType.Discard", code)

    def test_only_wrath_copy_state_reset_and_saved_state_remains(self):
        code = enchantment()
        self.assertIn("[SavedProperty]", code)
        self.assertIn("single.UsedThisCombat = false", code)
        self.assertIn("layers.Layers.OfType<WrathEnchantment>()", code)
        self.assertNotIn("Status =", code)
        self.assertNotIn("DeepCloneFields", code)
        self.assertNotIn("AfterCloned", code)

    def test_awakened_damage_lives_once_on_relic_not_each_layer(self):
        code = relic()
        for text in ("ModifyDamageAdditive", "CardPlay? cardPlay", "WrathRules.AwakenedBonus",
                     "cardSource?.Owner == Owner", "cardSource?.Type == CardType.Attack",
                     "LayeredEnchantments.Has<WrathEnchantment>(cardSource)"):
            self.assertIn(text, code)
        self.assertNotIn("WrathRouteRelic", enchantment())
        self.assertNotIn("AwakenedBonus", enchantment())

    def test_pickup_is_saved_and_filtered_permanent_attack(self):
        code = relic()
        for text in ("[SavedProperty] public bool PickupEffectGranted", "WrathRules.EnchantOnPickup(Stage)",
                     "CardSelectCmd.FromDeckGeneric", "Cancelable = false", "CardType.Attack",
                     "LayeredEnchantments.HasOpenSlot(card)", "enchantment.CanEnchant(card)",
                     "selected.Owner == Owner", "selected.Pile?.Type == PileType.Deck"):
            self.assertIn(text, code)
        self.assertLess(code.index("PickupEffectGranted = true"), code.index("FromDeckGeneric"))

    def test_production_rule_suite(self):
        self.assertIn("../../src/Core/Relics/WrathRules.cs", read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        tests = read("tests/DesignSyncContracts/Program.cs")
        for text in ("WrathRules.FirstPlayBonus", "WrathRules.AwakenedBonus", "WrathRules.EnchantOnPickup",
                     "awakened flat bonus not per enchantment layer"):
            self.assertIn(text, tests)

    def test_actual_combat_tests_cover_copies_layers_and_save(self):
        tests = read("src/ConsoleCommands/DesignWrathTestConsoleCmd.cs")
        for text in ("ctx.Play(card, ctx.PrimaryEnemy)", "first play damage", "later original produces no extra copy",
                     "copy can create another fresh copy", "CardModel.FromSerializable", "multi-hit produces only one copy",
                     "two Wrath layers but one awakened bonus", "unrelated spent enchantment not reset",
                     "other enchantment does not qualify", "other player's Wrath attack excluded"):
            self.assertIn(text, tests)

    def test_destructive_entry_is_explicit_debug_and_guarded(self):
        tests = read("src/ConsoleCommands/DesignWrathTestConsoleCmd.cs")
        for text in ("#if DEBUG", 'args[0] != "confirm"', "issuingPlayer.RunState.Players.Count != 1",
                     "!CombatManager.Instance.IsInProgress", "_running", "TestMode.IsOn = previousTestMode"):
            self.assertIn(text, tests)


if __name__ == "__main__":
    unittest.main(verbosity=2)
