"""Source contracts for actual scripture-generation tests; not engine execution."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


class ScriptureGenerationContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.runtime = read("src/Debugging/CardEffects/DesignSyncScriptureGenerationContract.cs")
        cls.punishment = read("src/Cards/MvpHolyCardsBatch2.cs").split("public sealed class HolyPunishment", 1)[1].split("[RegisterCard", 1)[0]
        cls.consecration = read("src/Powers/MvpHolyUtilityPowers.cs").split("public sealed class ConsecrationPower", 1)[1].split("[RegisterPower", 1)[0]

    def test_punishment_uses_native_draw_after_valid_transform_and_payment(self):
        code = self.punishment
        self.assertIn("card.Owner != Owner || card.Pile != draw || !card.IsTransformable", code)
        self.assertLess(code.index("card.Pile != draw"), code.index("await ScriptureCmd.TransformToRandomScripture"))
        self.assertLess(code.index("PayOverdraft("), code.index("draw.MoveToTopInternal(transformed)"))
        self.assertLess(code.index("draw.MoveToTopInternal(transformed)"), code.index("await CardPileCmd.Draw(context, 1, Owner)"))
        self.assertNotIn("CardPileCmd.Add(transformed, PileType.Hand)", code)
        self.assertIn("魔力解放：并抽取它。", read("DesignDoc.md"))

    def test_consecration_rechecks_power_and_selected_hand_card(self):
        code = self.consecration
        for token in ("IsMutable && Amount > 0 && Owner.IsAlive && Owner.Powers.Contains(this)",
                      "if (!IsActive || player.Creature != Owner) return;", "if (IsActive && card != null && card.Owner == player",
                      "card.Pile?.Type == PileType.Hand && card.IsTransformable"):
            self.assertIn(token, code)
        self.assertLess(code.index("card.Pile?.Type == PileType.Hand"), code.index("await ScriptureCmd.TransformToRandomScripture"))

    def test_gospel_keeps_native_transform_upgrade_contract(self):
        code = read("src/Cards/HolyCardsExpanded.cs").split("public sealed class Gospel", 1)[1]
        for token in ("card.Owner != Owner || card.Pile?.Type != PileType.Hand || !card.IsTransformable",
                      "ScriptureCmd.TransformCombatCard<GuardianScripture>(card)",
                      "ScriptureCmd.TransformCombatCard<PunishmentScripture>(card)", "CardCmd.Upgrade(transformed)"):
            self.assertIn(token, code)
        native = read("src/Commands/ScriptureCmd.cs")
        self.assertIn("CardCmd.TransformTo<TScripture>(", native)
        self.assertIn("圣言的卡牌变化、升级、附魔和其他未特别说明的行为沿用原版卡牌变化规则。", read("DesignDoc.md"))

    def test_chant_all_three_choices_and_rng_are_observed(self):
        for token in ("selectedIndex = 0; selectedIndex < 3", "new Rng(", "available.RemoveAt(index)",
                      "options.Select(c => c.GetType()).SequenceEqual(expected)", "options.Select(c => c.Id).Distinct().Count()",
                      "chosen instance reaches hand", "unselected offers not in any pile", "chant does not add to permanent deck",
                      "source.EnergyCost.GetWithModifiers(CostModifiers.All)"):
            self.assertIn(token, self.runtime)

    def test_gospel_mixed_hand_and_deck_isolation(self):
        for token in ("clone.DeckVersion = deck", "ctx.Combat.CloneCard(deck)", "ctx.Add<Wound>", "ctx.Add<Regret>",
                      "both skills become guardians", "both attacks become punishment", "c.IsUpgraded == upgraded",
                      "power status curse are same untouched instances", "permanent original untouched",
                      "native explicit transform does not inherit old enchantments", "gospel exhausts only itself",
                      "finally { await CardPileCmd.RemoveFromDeck(deck); }"):
            self.assertIn(token, self.runtime)

    def test_real_punishment_branches_and_draw_history(self):
        for token in ('"plain", "accept", "decline", "amplified", "empty", "moved", "blocked"',
                      "ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 2)", "ctx.ApplyPower<NoDrawPower>(ctx.Self, 1)",
                      "exact armor payment", "exact amplification consumption", "ctx.AssertDamage(",
                      "History.Entries.Skip(historyBefore).OfType<CardDrawnEntry>()", "draws[0].Card == result",
                      "NoDraw prevents retrieval and leaves selected scripture on top"):
            self.assertIn(token, self.runtime)

    def test_consecration_foreign_empty_and_stale_callbacks(self):
        for token in ("power.AfterPlayerTurnStart(choice, foreign)", "empty hand and foreign turn consume no RNG",
                      "new[] { false, true }", "await PowerCmd.Remove(power)",
                      "await CardPileCmd.Add(moving, PileType.Discard", "stale selection never transforms card",
                      "stale selection does not consume RNG", "removed power callback is inert"):
            self.assertIn(token, self.runtime)

    def test_four_registered_real_scenarios_and_unchanged_fulltext(self):
        catalog = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        for card, method, minimum in (("Chant", "Chant", 20), ("Consecration", "Consecration", 10),
                                      ("Gospel", "Gospel", 10), ("HolyPunishment", "Punishment", 30)):
            self.assertIn(f"CustomVariants<{card}>(DesignSyncScriptureGenerationContract.{method}, {minimum})", catalog)
            self.assertIn(f"new(typeof({card}),", read("src/Debugging/CardEffects/DesignSyncHolyTextContract.cs"))
        runner = read("src/Debugging/CardEffects/CardEffectTestRunner.cs")
        self.assertIn('"ds27-scripture-generation"', runner)
        self.assertIn("if (batch.Length != 4)", runner)
        self.assertIn("await CardCmd.AutoPlay(", self.runtime)
        self.assertIn("using (CardSelectCmd.UseSelector(selector))", self.runtime)


if __name__ == "__main__":
    unittest.main()
