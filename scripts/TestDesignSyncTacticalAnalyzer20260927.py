"""Tactical Analyzer source/text wiring checks; runtime assertions need the game."""
from pathlib import Path
import json
import unittest

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


class TacticalAnalyzerContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = read("src/Cards/MvpHolyCardsBatch2.cs").split("public sealed class TacticalAnalyzer", 1)[1]
        cls.runtime = read("src/Debugging/CardEffects/DesignSyncTacticalAnalyzerContract.cs")

    def test_native_eligibility_before_and_after_selection(self):
        source = self.source
        for token in ("card.Owner == Owner", "card.Pile?.Type == PileType.Hand",
                      "card.IsUpgradable", "ModelDb.Enchantment<Steady>().CanEnchant(card)",
                      "CanUpgradeAndEnchant, this", "if (card != null && CanUpgradeAndEnchant(card))"):
            self.assertIn(token, source)
        self.assertLess(source.index("await CardPileCmd.Draw"), source.index("await CardSelectCmd.FromHand"))
        self.assertLess(source.index("if (card != null && CanUpgradeAndEnchant(card))"), source.index("CardCmd.Upgrade(card)"))
        self.assertLess(source.index("CardCmd.Upgrade(card)"), source.index("CombatEnchantmentCmd.ApplyVanilla<Steady>(card, 1)"))
        self.assertNotIn("card.Enchantment == null", source)
        self.assertNotIn("ClearEnchantment", source)

    def test_design_and_exact_text_unchanged(self):
        design = read("DesignDoc.md")
        self.assertIn("所有临时附魔的逻辑（叠加、复制、作用类型等）完全与普通附魔一致。", design)
        self.assertIn("抽1/2张牌。升级一张手牌并附魔：稳定。", design)
        self.assertIn("被附魔后允许叠加其他附魔，允许同名附魔叠层", design)
        cards = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        self.assertEqual(cards["MAIDEN_SUCCUBUS_CARD_TACTICAL_ANALYZER.description"],
                         "抽{Cards:diff()}张牌。\n升级一张[gold]手牌[/gold]并[gold]附魔[/gold]：[purple]稳定[/purple]。")
        self.assertIn('new CardsVar(1)', self.source)
        self.assertIn('DynamicVars.Cards.UpgradeValueBy(1)', self.source)
        full = read("src/Debugging/CardEffects/DesignSyncHolyTextContract.cs")
        self.assertIn('new(typeof(TacticalAnalyzer), "抽1张牌。\\n升级一张手牌并附魔：稳定。", "抽2张牌。\\n升级一张手牌并附魔：稳定。")', full)

    def test_actual_selector_observes_offered_set(self):
        for token in (": ICardSelector", "options.ToHashSet().SetEquals(fillers.Append(alternative))",
                      "using (CardSelectCmd.UseSelector(selector))", "await CardCmd.AutoPlay(",
                      'ctx.AssertEqual("one actual selector call", 1, selector.Calls)',
                      "chosen.Enchantment is Steady { Amount: 1 }", "enchanted.Enchantment is Sharp { Amount: 3 }",
                      "!steady.IsUpgraded", "!alternative.IsUpgraded"):
            self.assertIn(token, self.runtime)

    def test_no_candidate_and_permanent_deck_isolation(self):
        for token in ("clone.DeckVersion = deck", "ctx.Combat.CloneCard(deck)",
                      "!deck.IsUpgraded && deck.Enchantment == null", "finally { await CardPileCmd.RemoveFromDeck(deck); }",
                      "Empty eligibility must not open a selector", "empty eligibility does not cancel draw",
                      "ineligible.All(c => c.Pile?.Type == PileType.Hand)", "empty eligibility resolves source normally"):
            self.assertIn(token, self.runtime)

    def test_light_wings_is_not_excluded(self):
        for token in ("ctx.Add<LightWings>(PileType.Hand)", "ApplyVanilla<Sharp>(wings, 2)",
                      "selectedCards: [wings]", "wings.IsUpgraded", "layers.Layers.Count",
                      "layers.Layers.OfType<Sharp>().Single().Amount == 2",
                      "layers.Layers.OfType<Steady>().Single().Amount == 1"):
            self.assertIn(token, self.runtime)
        patch = read("src/Patches/LayeredEnchantmentPatches.cs")
        self.assertIn("nameof(EnchantmentModel.CanEnchant)", patch)
        self.assertIn("LayeredEnchantments.Supports(card)", patch)

    def test_revalidate_async_selection_result(self):
        for token in ('new[] { "enchanted", "upgraded", "moved" }', "await Task.Yield()",
                      'CombatEnchantmentCmd.ApplyVanilla<Sharp>(target, 4)', "CardCmd.Upgrade(target)",
                      "await CardPileCmd.Add(target, PileType.Discard", "return [target]",
                      "stale target not additionally upgraded", "stale target never gets stable enchantment",
                      "departed target not moved back", "stale choice does not strand source"):
            self.assertIn(token, self.runtime)

    def test_existing_catalogue_runs_new_contract_for_both_variants(self):
        catalog = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        self.assertIn("TacticalAnalyzerProbe();", catalog)
        self.assertIn("CustomVariants<TacticalAnalyzer>(DesignSyncTacticalAnalyzerContract.Run, 25)", catalog)
        self.assertTrue(self.runtime.startswith("#if DEBUG"))
        command = read("src/ConsoleCommands/CardEffectTestConsoleCmd.cs")
        self.assertIn("confirm", command)


if __name__ == "__main__":
    unittest.main()
