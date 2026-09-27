"""DS27-02K wiring/text regression; native scenarios still need the game."""
import json
import unittest
from TestDesignSyncNeutral20260927 import read


class WindGodCloakContracts(unittest.TestCase):
    def power(self):
        return read("src/Powers/Iteration2NeutralPowers.cs").split("public sealed class WindGodCloakPower", 1)[1]

    def test_actual_payment_not_effect_value_or_displayed_cost(self):
        code = self.power()
        self.assertIn("cardPlay.Resources.EnergySpent != 0", code)
        self.assertNotIn("EnergyValue", code)
        self.assertNotIn("EnergyCost", code)

    def test_first_is_reserved_before_effect_and_not_after_nested_children(self):
        before = self.power().split("BeforeCardPlayed", 1)[1].split("public override async Task AfterCardPlayed", 1)[0]
        for term in ("!cardPlay.IsFirstInSeries", "cardPlay.Card.Owner.Creature != Owner",
                     "CopiedThisTurn = true", "_pendingCopyCard = cardPlay.Card", "_pendingCopyCount = Amount"):
            self.assertIn(term, before)
        self.assertNotIn("CloneCard", before)

    def test_new_activation_not_retroactive_for_any_replay(self):
        card = read("src/Cards/Iteration2NeutralCards.cs").split("public sealed class WindGodCloak", 1)[1]
        self.assertNotIn("IgnoreActivationCard", card)
        self.assertNotIn("_activationCardToIgnore", self.power())
        self.assertIn("PowerCmd.Apply<WindGodCloakPower>", card)
        self.assertIn("!cardPlay.IsLastInSeries || !ReferenceEquals(cardPlay.Card, _pendingCopyCard)", self.power())

    def test_same_instance_nested_series_depth_and_release_before_await(self):
        code = self.power()
        self.assertIn("_pendingSeriesDepth++", code)
        self.assertIn("if (--_pendingSeriesDepth > 0) return", code)
        after = code.split("public override async Task AfterCardPlayed", 1)[1]
        self.assertLess(after.index("ClearPendingCopy()"), after.index("await CardPileCmd"))
        self.assertIn("for (int i = 0; i < copies; i++)", after)
        self.assertIn("int copies = _pendingCopyCount", after)

    def test_only_owner_turn_resets_and_clones_clear_transients(self):
        code = self.power()
        self.assertIn("side == CombatSide.Player && creatures.Contains(Owner)", code)
        self.assertIn("[SavedProperty]\n    public bool CopiedThisTurn", code)
        clone = code.split("protected override void DeepCloneFields()", 1)[1]
        self.assertIn("base.DeepCloneFields()", clone)
        self.assertIn("ClearPendingCopy()", clone)
        self.assertNotIn("CopiedThisTurn = false", clone)

    def test_copy_uses_native_generation_and_drops_permanent_link(self):
        code = self.power()
        self.assertIn("combat.CloneCard(cardPlay.Card)", code)
        self.assertIn("Owner.CombatState != combat || CombatManager.Instance.IsOverOrEnding", code)
        self.assertIn("copy.DeckVersion = null", code)
        self.assertIn("CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, player)", code)
        self.assertNotIn("PileType.Deck", code)

    def test_exact_design_text_and_power_stack_number(self):
        expected = "将你在每回合打出的第一张耗能为0的牌的复制加入你的[gold]手牌[/gold]。"
        cards = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        powers = json.loads(read("MaidenSuccubus/localization/zhs/powers.json"))
        self.assertEqual(cards["MAIDEN_SUCCUBUS_CARD_WIND_GOD_CLOAK.description"], expected)
        self.assertEqual(powers["MAIDEN_SUCCUBUS_POWER_WIND_GOD_CLOAK_POWER.description"], expected)
        self.assertEqual(powers["MAIDEN_SUCCUBUS_POWER_WIND_GOD_CLOAK_POWER.smartDescription"],
            expected.replace("的复制", "的{Amount}张复制"))
        self.assertIn(expected.replace("[gold]", "").replace("[/gold]", ""), read("DesignDoc.md"))

    def test_runtime_payment_and_replay_use_native_commands(self):
        code = read("src/Debugging/CardEffects/DesignSyncWindGodCloakContract.cs")
        for term in ("await card.SpendResources()", "card.OnPlayWrapper", "isAutoPlay: false",
                     "discounted.EnergyCost.SetThisCombat(0)", "CombatEnchantmentCmd.ApplyVanilla<Glam>",
                     "ctx.Add<BlackVortex>", "ctx.AddFillerCards(PileType.Hand, 10",
                     "SavedProperties.From(Power(ctx))!.Fill(restored)", "ClonePreservingMutability",
                     "another player's extra turn", "new layer is not retroactive"):
            self.assertIn(term, code)
        self.assertIn("CustomVariants<WindGodCloak>(DesignSyncWindGodCloakContract.Run, 25)",
                      read("src/Debugging/CardEffects/CardEffectTestCatalog.cs"))


if __name__ == "__main__":
    unittest.main(verbosity=2)
