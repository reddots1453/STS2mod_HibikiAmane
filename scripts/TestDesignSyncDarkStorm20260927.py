"""DS27 DarkStorm wiring and precise text; actual native execution lives in the game suite."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


class DarkStormContract(unittest.TestCase):
    def storm(self):
        return read("src/Cards/MvpStrengthCards.cs").split("public sealed class DarkStorm", 1)[1].split("[RegisterCard", 1)[0]

    def test_fixed_meta_and_values(self):
        code = self.storm()
        self.assertIn("base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)", code)
        self.assertIn("new DamageVar(8, ValueProp.Move)", code)
        self.assertIn("new PowerVar<VulnerablePower>(2)", code)
        self.assertNotIn("UpgradeValueBy", code)

    def test_upgrade_attaches_actual_glam(self):
        upgrade = self.storm().split("protected override void OnUpgrade()", 1)[1]
        for term in ["ControlQuery.SuppressPresentation()", "if (Enchantment != null) return",
                     "ModelDb.Enchantment<Glam>().ToMutable()", "EnchantInternal(glam, 1)", "glam.ModifyCard()"]:
            self.assertIn(term, upgrade)
        self.assertNotIn("await", upgrade)
        self.assertNotIn("UpgradeValueBy", upgrade)

    def test_no_pickup_reapplication_and_legacy_property_retained(self):
        code = self.storm()
        self.assertNotIn("AfterCardChangedPiles", code)
        self.assertNotIn("EnchantAndPreview", code)
        self.assertIn("[SavedProperty]\n    public bool EnchantedOnPickup", code)
        self.assertNotIn("EnchantedOnPickup =", code)

    def test_upgrade_does_not_depend_on_live_run_or_pollute_deck(self):
        upgrade = self.storm().split("protected override void OnUpgrade()", 1)[1]
        for forbidden in ["Owner", "DeckVersion", "RunState", "CardCmd.Enchant", "EnchantmentVfxCmd", "TaskHelper"]:
            self.assertNotIn(forbidden, re.sub(r"//[^\n]*", "", upgrade))

    def test_description_matches_current_design(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        text = loc["MAIDEN_SUCCUBUS_CARD_DARK_STORM.description"]
        self.assertNotIn("拾起时", text)
        self.assertIn("升级时，为这张牌[gold]附魔[/gold]：[purple]华彩[/purple]。", text)
        concrete = text.replace("{Damage:diff()}", "8").replace("{VulnerablePower:diff()}", "2")
        concrete = re.sub(r"\[/?(?:gold|purple)\]", "", concrete).replace("\n", "")
        self.assertIn(concrete, read("DesignDoc.md"))
        self.assertIn("HoverTipFactory.FromEnchantment<Glam>()", self.storm())

    def test_catalog_no_longer_tests_old_upgrade(self):
        catalog = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        self.assertIn("DarkStormProbe();", catalog)
        self.assertIn("CustomVariants<DarkStorm>(DesignSyncDarkStormContract.Run, 26)", catalog)
        self.assertNotIn("DamageAllTargetPower<DarkStorm>(8, 10", catalog)

    def test_real_replay_damage_and_lifetime_oracles(self):
        tests = read("src/Debugging/CardEffects/DesignSyncDarkStormContract.cs")
        for term in ["await ctx.Play(card)", "upgraded ? 20 : 8", "upgraded ? 4 : 2",
                     "second play does not replay", "card.GetEnchantedReplayCount()", "card.CreateClone()",
                     "CardModel.FromSerializable(card.ToSerializable())", "card.Enchantment, clone.Enchantment"]:
            self.assertIn(term, tests)

    def test_real_upgrade_and_persistence_oracles(self):
        tests = read("src/Debugging/CardEffects/DesignSyncDarkStormContract.cs")
        for term in ["preview.UpgradeInternal()", "combat.DeckVersion = deck", "CardCmd.Upgrade(combat",
                     "!deck.IsUpgraded && deck.Enchantment == null", "CardCmd.Upgrade(deck", "LoadCard(deck.ToSerializable()",
                     "ApplyVanilla<Sharp>", "legacy.EnchantedOnPickup = true", "legacyLoaded.UpgradeInternal()"]:
            self.assertIn(term, tests)


if __name__ == "__main__":
    unittest.main()
