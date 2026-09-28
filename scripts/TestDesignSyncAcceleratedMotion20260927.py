"""Permanent pickup-copy contracts; actual gameplay requires the debug suite."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model


class AcceleratedMotionContracts(unittest.TestCase):
    def test_exact_design_and_template(self):
        design = re.sub(r"\s", "", read("DesignDoc.md"))
        self.assertIn("加速运动技能牌稀有0费抽2/3张牌。消耗。拾起时，向牌组中加入1张复制。", design)
        self.assertIn("这是类似遗物中的“拾起时”效果，而不作用于战斗内", design)
        loc = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        self.assertEqual(loc["MAIDEN_SUCCUBUS_CARD_ACCELERATED_MOTION.description"],
                         "抽{Cards:diff()}张牌。\n拾起时，向牌组中加入1张复制。")

    def test_metadata_and_draw_values_unchanged(self):
        source = model("AcceleratedMotion")
        for token in ("base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)", "CardKeyword.Exhaust",
                      "new CardsVar(2)", 'new DynamicVar("Copies", 1)', "DynamicVars.Cards.UpgradeValueBy(1)",
                      "CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner)"):
            self.assertIn(token, source)

    def test_only_actual_first_permanent_pickup_can_duplicate(self):
        source = model("AcceleratedMotion")
        for token in ("[SavedProperty]", "card != this", "AddedPickupCopies", "oldPileType != PileType.None",
                      "card.Pile?.Type != PileType.Deck"):
            self.assertIn(token, source)
        self.assertLess(source.index("AddedPickupCopies = true;"), source.index("Owner.RunState.CloneCard(this)"))

    def test_copy_is_native_full_run_clone_not_new_canonical_card(self):
        source = model("AcceleratedMotion")
        self.assertIn("(AcceleratedMotion)Owner.RunState.CloneCard(this)", source)
        for forbidden in ("CreateCard<AcceleratedMotion>", "CreateClone()", "CardCmd.Upgrade(copy)", "CombatState.CloneCard"):
            self.assertNotIn(forbidden, source)
        self.assertLess(source.index("copy.AddedPickupCopies = true;"), source.index("await CardPileCmd.Add(copy, PileType.Deck)"))

    def test_actual_probe_replaces_draw_only_coverage(self):
        source = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        self.assertIn("CustomVariants<AcceleratedMotion>(DesignSyncAcceleratedMotionContract.Run, 41)", source)
        self.assertNotIn("Draw<AcceleratedMotion>(2, 3)", source)

    def test_game_probe_covers_exact_text_draw_and_combat_exclusion(self):
        source = read("src/Debugging/CardEffects/DesignSyncAcceleratedMotionContract.cs")
        for token in ("instance.GetDescriptionForPile(pile)", "CardPileCmd.AddGeneratedCardToCombat(card",
                      "!card.AddedPickupCopies", "await ctx.Play(card)", "upgraded ? 3 : 2",
                      "PileType.Exhaust", "combat play adds no permanent copy"):
            self.assertIn(token, source)

    def test_game_probe_covers_real_pickup_clone_save_and_no_recursion(self):
        source = read("src/Debugging/CardEffects/DesignSyncAcceleratedMotionContract.cs")
        for token in ("CardPileCmd.Add(original, PileType.Deck", "CardCmd.Enchant<Swift>(original, 4)",
                      "copy.Enchantment is Swift { Amount: 4", "!ReferenceEquals(copy.Enchantment, original.Enchantment)",
                      "copy.RemoveKeyword(CardKeyword.Retain)", "original.AfterCardChangedPiles(original, PileType.None",
                      "copy.AfterCardChangedPiles(copy, PileType.None", "LoadCard(saved.ToSerializable()",
                      "resumed.AddedPickupCopies", "beforeLoadAdd + 1", "ctx.Combat.CloneCard(copy)",
                      "finally", "CardPileCmd.RemoveFromDeck(created"):
            self.assertIn(token, source)


if __name__ == "__main__":
    unittest.main()
