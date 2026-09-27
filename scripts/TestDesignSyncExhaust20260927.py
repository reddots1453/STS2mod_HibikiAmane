"""DS27-02H wiring, text and regression scope; not a substitute for engine tests."""
import json
import unittest
from TestDesignSyncNeutral20260927 import read


class ExhaustContracts(unittest.TestCase):
    def regen(self):
        return read("src/Cards/MvpExhaustCards.cs").split("public sealed class SuperRegeneration", 1)[1]

    def test_regeneration_pays_after_selected_card_is_played(self):
        code = self.regen()
        self.assertLess(code.index("await CardCmd.AutoPlay"), code.index("await OverdraftCmd.Offer"))
        self.assertIn("if (await OverdraftCmd.Offer(context, this, 1)) _returnAfterExhaust = true", code)
        self.assertIn("CardKeyword.Exhaust", code)

    def test_return_follows_real_exhaust_and_is_instance_local(self):
        code = self.regen().split("public override async Task AfterCardExhausted", 1)[1]
        for term in ["if (card != this || !_returnAfterExhaust) return", "_returnAfterExhaust = false",
                     "Pile?.Type == PileType.Exhaust", "await CardPileCmd.Add(this, PileType.Hand)"]:
            self.assertIn(term, code)
        self.assertNotIn("ModifyCardPlayResultPile", self.regen())
        self.assertNotIn("SavedProperty", self.regen())

    def test_pending_return_cannot_leak_into_replay_or_clone(self):
        code = self.regen()
        self.assertIn("cardPlay.Card == this && cardPlay.IsFirstInSeries", code)
        clone = code.split("protected override void DeepCloneFields()", 1)[1]
        self.assertIn("base.DeepCloneFields()", clone)
        self.assertIn("_returnAfterExhaust = false", clone)

    def test_exhume_target_filter_and_release_hover(self):
        self.assertIn("card is not SuperRegeneration && !card.Keywords.Contains(CardKeyword.Unplayable)", self.regen())
        self.assertIn("CanSelect)).FirstOrDefault()", self.regen())
        self.assertIn('CardHoverTipSupport.Static("MAIDENSUCCUBUS_OVERDRAFT")', self.regen())

    def test_staff_self_exhaust_not_only_generated_cards(self):
        code = read("src/Cards/Iteration1CorruptCards.cs").split("public sealed class DemonStaff", 1)[1].split("[RegisterCard", 1)[0]
        self.assertIn("CanonicalKeywords => [CardKeyword.Exhaust]", code)
        for term in ["Owner.UnlockState.CharacterCardPools", "Owner.RunState.CardMultiplayerConstraint",
                     "HasExhaustRule(upgraded)", "GeneratedCardCostCmd.SetFreeThisTurn(selected)"]:
            self.assertIn(term, code)

    def test_vortex_scope_is_around_only_child_autoplay(self):
        code = read("src/Cards/MvpExhaustCards2.cs").split("public sealed class BlackVortex", 1)[1].split("public sealed class GrudgeFire", 1)[0]
        self.assertIn("using var noAmplificationConsumption = AmplificationConsumptionScope.Enter(card)", code)
        self.assertIn("await CardCmd.AutoPlay(context, card, null)", code)
        self.assertIn("await CardPileCmd.ShuffleIfNecessary(context, Owner)", code)
        self.assertIn("await OverdraftCmd.Offer(context, this, 1)", code)

    def test_amplification_reservation_ignores_exempt_children(self):
        code = read("src/Powers/TransformationPowers.cs").split("public sealed class MagicAmplificationPower", 1)[1]
        self.assertIn("card == null || AmplificationConsumptionScope.IsExempt(card)", code)
        self.assertIn("!AmplificationConsumptionScope.IsExempt(cardPlay.Card)", code)
        self.assertIn("card is MaidenSuccubus.Cards.BlackVortex", code)
        self.assertIn("card.Pile?.Type == PileType.Play && card.Owner?.Creature == Owner", code)
        self.assertIn("-reservedForOverdraft", code)

    def test_scope_is_weak_instance_counter_not_async_global_flag(self):
        code = read("src/Util/WeakInstanceScope.cs")
        for term in ["ConditionalWeakTable<T, State>", "state.Depth++", "state.Depth--", "if (_disposed) return"]:
            self.assertIn(term, code)
        self.assertNotIn("ThreadStatic", code)
        self.assertIn("WeakInstanceScope<CardModel>", read("src/Core/Transformation/AmplificationConsumptionScope.cs"))
        self.assertIn("../../src/Util/WeakInstanceScope.cs", read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))

    def test_text_clause_and_punctuation_exact(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        self.assertEqual(loc["MAIDEN_SUCCUBUS_CARD_SUPER_REGENERATION.description"],
                         "从[gold]消耗[/gold]堆选择打出一张牌。\n[gold]魔力解放[/gold]：将此牌放回[gold]手牌[/gold]。")
        self.assertEqual(loc["MAIDEN_SUCCUBUS_CARD_BLACK_VORTEX.description"],
                         "打出牌堆顶2张牌。\n[gold]魔力解放[/gold]：将其[gold]消耗[/gold]并重复此效果。")
        self.assertIn("从消耗堆选择打出一张牌。消耗。魔力解放：将此牌放回手牌。", read("DesignDoc.md"))

    def test_native_keyword_line_moved_without_changing_rule_or_duplicate(self):
        code = read("src/Patches/SuperRegenerationDescriptionPatch.cs")
        for term in ["if (__instance is not SuperRegeneration) return", "Safe.Run", '"EXHAUST.title"',
                     "lines.RemoveAt(exhaust)", "lines.Insert(release, keyword)"]:
            self.assertIn(term, code)
        self.assertNotIn("RemoveKeyword", code)

    def test_runtime_tests_cover_actual_payment_and_pile_transitions(self):
        code = read("src/Debugging/CardEffects/DesignSyncExhaustContract.cs")
        for term in ["await ctx.Play(card, selectedIndices: [0])", "await ctx.Play(card, selectedIndices: [1])",
                     "FeelNoPainPower", "CardCmd.Exhaust", "release does not return the target",
                     "empty exhaust release still returns self", "amplification paid once"]:
            self.assertIn(term, code)

    def test_runtime_tests_cover_mid_effect_gain_and_normal_play_isolation(self):
        code = read("src/Debugging/CardEffects/DesignSyncExhaustContract.cs")
        for term in ["Add<MagiciansSecret>", "two releases permit three pairs", "second child did not steal new layer",
                     "short draw pile shuffles to play twice", "exception cleanup", "later normal play consumes"]:
            self.assertIn(term, code)
        catalog = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        self.assertIn("CustomVariants<BlackVortex>(DesignSyncExhaustContract.Vortex, 20)", catalog)
        self.assertIn("CustomVariants<SuperRegeneration>(DesignSyncExhaustContract.Regeneration, 19)", catalog)
        self.assertIn("staff exhausts after generation", catalog)


if __name__ == "__main__":
    unittest.main()
