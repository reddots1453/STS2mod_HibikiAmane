"""Four-card exact text and native draw/pickup regression wiring."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model


class EnchantmentInputContracts(unittest.TestCase):
    def test_exact_design_scope(self):
        design = re.sub(r"\s|\*", "", read("DesignDoc.md"))
        for expected in (
            "魔法之剑攻击牌普通2费造成12/18点伤害。拾起时，为这张牌附魔：充能：2。",
            "疾风之剑攻击牌普通1费造成11/14点伤害。拾起时，为这张牌附魔：迅捷：2。",
            "闪耀之剑攻击牌普通1费造成4/6点伤害2次。拾起时，为这张牌附魔：活力：3。",
            "魔力索引能力牌罕见1/0费每当你抽到1张附魔牌时，抽1张牌。",
        ):
            self.assertIn(expected, design)

    def test_exact_card_templates_and_rich_text(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        expected = {
            "MAGIC_SWORD": "造成{Damage:diff()}点伤害。\n拾起时，为这张牌[gold]附魔[/gold]：[purple]充能：2[/purple]。",
            "GALE_SWORD": "造成{Damage:diff()}点伤害。\n拾起时，为这张牌[gold]附魔[/gold]：[purple]迅捷：2[/purple]。",
            "SHINING_SWORD": "造成{Damage:diff()}点伤害{Repeat}次。\n拾起时，为这张牌[gold]附魔[/gold]：[purple]活力：3[/purple]。",
            "MAGIC_INDEX": "每当你抽到1张[gold]附魔牌[/gold]时，抽1张牌。",
        }
        for name, text in expected.items():
            self.assertEqual(loc["MAIDEN_SUCCUBUS_CARD_" + name + ".description"], text)

    def test_index_power_canonical_and_stacked_text(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/powers.json"))
        self.assertEqual(loc["MAIDEN_SUCCUBUS_POWER_MAGIC_INDEX_POWER.description"],
                         "每当你抽到1张[gold]附魔牌[/gold]时，抽1张牌。")
        self.assertEqual(loc["MAIDEN_SUCCUBUS_POWER_MAGIC_INDEX_POWER.smartDescription"],
                         "每当你抽到1张[gold]附魔牌[/gold]时，抽{Amount}张牌。")
        self.assertIn("EnergyCost.UpgradeBy(-1)", model("MagicIndex"))

    def test_index_retains_native_recursive_draw_with_instance_guards(self):
        source = read("src/Powers/MvpNeutralUtilityPowers.cs").split("public sealed class MagicIndexPower", 1)[1].split("[RegisterPower]", 1)[0]
        for token in ("AfterCardDrawnEarly", "PowerStackType.Counter", "IsAlive: true",
                      "ReferenceEquals(Owner.GetPower<MagicIndexPower>(), this)", "card.Owner != Owner.Player",
                      "card.Enchantment == null", "ReferenceEquals(card.CombatState, Owner.CombatState)",
                      "await CardPileCmd.Draw(context, Amount, Owner.Player)"):
            self.assertIn(token, source)
        for forbidden in ("Task.Run", "_ =", "_drawing", "HashSet<CardModel>", "fromHandDraw: true"):
            self.assertNotIn(forbidden, source)

    def test_pickup_implementations_keep_real_permanent_enchantments_and_vfx(self):
        for name, enchantment, amount in (("MagicSword", "ChargeEnchantment", 2), ("GaleSword", "Swift", 2), ("ShiningSword", "Vigorous", 3)):
            source = model(name)
            for token in ("[SavedProperty]", "!EnchantedOnPickup", "oldPileType == PileType.None",
                          "card.Pile?.Type == PileType.Deck", "if (Enchantment == null)",
                          f"PickupEnchantmentCmd.EnchantAndPreview<{enchantment}>(this, {amount})"):
                self.assertIn(token, source)

    def test_four_card_suite_is_wired_without_replacing_real_effects_with_text_only(self):
        catalog = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        for name in ("MagicSword", "GaleSword", "ShiningSword"):
            self.assertIn(f"CustomVariants<{name}>((ctx, card, upgraded) => DesignSyncEnchantmentInputContract.Sword(ctx, card, upgraded), 17)", catalog)
        self.assertIn("CustomVariants<MagicIndex>(DesignSyncEnchantmentInputContract.Index, 25)", catalog)
        runner = read("src/Debugging/CardEffects/CardEffectTestRunner.cs")
        for token in ('"ds27-enchantment-input"', "batch.Length != 4", "DesignSyncEnchantmentInputContract.Validate(context, card, scenario.Upgraded)"):
            self.assertIn(token, runner)

    def test_game_cases_cover_chain_restrictions_pickup_and_first_use(self):
        source = read("src/Debugging/CardEffects/DesignSyncEnchantmentInputContract.cs")
        for token in ("PileType.Deck", "GetDescriptionForPile(PileType.Hand)", "ctx.Combat.CloneCard(deck)",
                      "combat.DeckVersion = deck", "play < 2", "EnchantmentStatus.Disabled", "EnchantmentStatus.Normal",
                      "CardPileCmd.AddGeneratedCardToCombat", "stacks == 1 ? 3 : 5", "new[] { 9, 10 }",
                      "ApplyPower<NoDrawPower>", "fromHandDraw: true", "Enchanted(PileType.Discard)",
                      "PowerCmd.Remove(power)", "foreignCard", "noncombat card cannot trigger combat index", "finally"):
            self.assertIn(token, source)


if __name__ == "__main__":
    unittest.main()
