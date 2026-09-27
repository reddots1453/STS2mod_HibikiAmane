"""DS27-05C wiring/localization checks; numeric rules execute in C# contracts."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


def relic(name):
    return read("src/Relics/FourthRouteRelics.cs").split(
        f"public sealed class {name}RouteRelic", 1)[1].split("[RegisterRelic", 1)[0]


class VirtueContracts(unittest.TestCase):
    def test_design_and_localization_exact_stage_effects(self):
        design = read("DesignDoc.md")
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        for name, title in (("BENEVOLENCE", "仁爱"), ("DILIGENCE", "勤勉")):
            section = design.split(f"#### {title}\n", 1)[1].split("####", 1)[0]
            for stage, label in ((1, "残缺"), (2, "完整"), (4, "觉醒")):
                expected = re.search(rf"^{label}：(.*)$", section, re.M).group(1)
                actual = loc[f"MAIDEN_SUCCUBUS_RELIC_{name}_ROUTE_RELIC.descriptionStage{stage}"]
                self.assertEqual(expected, re.sub(r"\[/?gold\]", "", actual))
            prefix = f"MAIDEN_SUCCUBUS_RELIC_{name}_ROUTE_RELIC.descriptionStage"
            self.assertEqual(loc[prefix + "3"], loc[prefix + "4"])

    def test_benevolence_permanent_owner_aware_addition(self):
        code = relic("Benevolence")
        for part in ("BenevolencePickupCount(Stage)", "BenevolenceOnAdded(Stage, card.Owner == Owner",
                     "oldPileType == PileType.Deck", "card.Pile?.Type == PileType.Deck",
                     "Owner.Deck.Cards.Where(card => card.IsUpgradable)", "UpgradeRandom(2)"):
            self.assertIn(part, code)
        self.assertNotIn("CardReward", code)
        self.assertNotIn("AfterCombatVictory", code)

    def test_pickup_receipts_saved_before_side_effects(self):
        for name, receipt, effect in (("Benevolence", "PickupEffectGranted", "UpgradeRandom(Virtue"),
                                      ("Diligence", "PickupRewardsGranted", "await RewardsCmd.OfferCustom")):
            code = relic(name)
            self.assertIn(f"[SavedProperty] public bool {receipt}", code)
            self.assertIn(f"Stage == 0 || {receipt}", code)
            self.assertLess(code.index(f"{receipt} = true"), code.index(effect))

    def test_reward_generation_upgrades_before_enchanting(self):
        code = relic("Diligence")
        for part in ("VirtuePickupRules.Diligence(Stage)", "i < rule.Count",
                     "CardCreationOptions.ForRoom(Owner, RoomType.Monster)",
                     "reward.AfterGenerated +=", "card.Owner != Owner", "options.Count == 0"):
            self.assertIn(part, code)
        body = code.split("internal void PrepareRewardOptions", 1)[1]
        self.assertLess(body.index("CardCmd.Upgrade"), body.index("EnchantmentOptions(card)"))
        self.assertNotIn("AfterCombatVictory", code)
        self.assertNotIn("UpgradeRandom", code)

    def test_vanilla_legal_pool_and_deterministic_amounts(self):
        code = relic("Diligence")
        for part in ("ModelDb.DebugEnchantments", "typeof(EnchantmentModel).Assembly",
                     'Namespace == "MegaCrit.Sts2.Core.Models.Enchantments"', "is not DeprecatedEnchantment",
                     "enchantment.ToMutable()", "enchantment.CanEnchant(card)",
                     "Owner.RunState.Rng.Niche", "VirtuePickupRules.RollEnchantmentAmount"):
            self.assertIn(part, code)
        self.assertNotIn("Random.Shared", code)

    def test_per_instance_dedupe_and_chosen_card_visuals_only(self):
        code = relic("Diligence")
        for part in ("protected override void DeepCloneFields()", "base.DeepCloneFields()",
                     "_prepared = new()", "_pendingReveal = new()", "_prepared.TryGetValue(card",
                     "_pendingReveal.Remove(card)", "EnchantmentVfxCmd.PreviewAfterCardPickup(card)"):
            self.assertIn(part, code)
        before_add_hook = code.split("public override Task AfterCardChangedPiles", 1)[0]
        self.assertNotIn("PreviewAfterCardPickup", before_add_hook)

    def test_executable_contracts_link_production(self):
        self.assertIn("../../src/Core/Relics/VirtuePickupRules.cs",
                      read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        code = read("tests/DesignSyncContracts/Program.cs")
        for part in ("VirtuePickupRules.BenevolencePickupCount", "VirtuePickupRules.BenevolenceOnAdded",
                     "VirtuePickupRules.Diligence", "VirtuePickupRules.RollEnchantmentAmount"):
            self.assertIn(part, code)

    def test_actual_model_tests_are_explicit_destructive_debug_only(self):
        code = read("src/ConsoleCommands/DesignVirtueTestConsoleCmd.cs")
        for part in ("#if DEBUG", 'CmdName => "ms_test_virtues"', 'args[0] != "confirm"',
                     "issuingPlayer.RunState.Players.Count != 1", "CombatManager.Instance.IsInProgress",
                     "await RelicCmd.Obtain", "SavedProperties.From", "await CardPileCmd.Add",
                     "PrepareToSelectCardReward", "TestMode.IsOn = previousTestMode", "[DS27VirtueTest] FAIL"):
            self.assertIn(part, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
