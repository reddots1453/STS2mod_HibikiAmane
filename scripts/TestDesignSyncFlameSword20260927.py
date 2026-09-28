"""DS27-02J lifecycle/text wiring. Native engine assertions require ms_test_cards."""
import json
import unittest
from TestDesignSyncNeutral20260927 import read


class FlameSwordContracts(unittest.TestCase):
    def card(self):
        return read("src/Cards/Iteration2ExpansionCards.cs").split("public sealed class FlameSword", 1)[1]

    def test_design_is_five_completed_battles_not_six_plays(self):
        self.assertIn("完成5场战斗后，为这张牌附魔：特兹卡塔拉的余烬。（还剩？场战斗）", read("DesignDoc.md"))
        self.assertIn("private const int RequiredCombats = 5", self.card())
        self.assertNotIn("RequiredPlays", self.card())
        play = self.card().split("protected override Task OnPlay", 1)[1].split("public override Task AfterCombatVictory", 1)[0]
        self.assertIn("DamageCmd.Attack(DynamicVars.Damage.BaseValue)", play)
        self.assertNotIn("Enchant", play)
        self.assertNotIn("CompletedCombats", play)
        self.assertNotIn("TimesPlayed", play)

    def test_only_permanent_deck_instance_receives_growth(self):
        code = self.card()
        for term in ("Pile?.Type != PileType.Deck", "HasBeenRemovedFromState", "!Owner.Deck.Cards.Contains(this)"):
            self.assertIn(term, code)
            self.assertLess(code.index(term), code.index("CompletedCombats++"))
        self.assertNotIn("DeckVersion as FlameSword ?? this", code)
        self.assertNotIn("AfterCombatEnd(", code)

    def test_room_deduplication_does_not_retain_entire_old_combat(self):
        code = self.card()
        self.assertIn("WeakReference<CombatRoom>? _lastCountedRoom", code)
        self.assertIn("ReferenceEquals(room, previousRoom)", code)
        self.assertLess(code.index("_lastCountedRoom = new(room)"), code.index("CompletedCombats++"))
        clone = code.split("protected override void DeepCloneFields()", 1)[1]
        self.assertIn("base.DeepCloneFields()", clone)
        self.assertIn("_lastCountedRoom = null", clone)

    def test_new_progress_saved_old_plays_not_reinterpreted(self):
        code = self.card()
        self.assertIn("[SavedProperty]\n    public int TimesPlayed { get; set; }", code)
        self.assertIn("[SavedProperty]\n    public int CompletedCombats", code)
        self.assertEqual(code.count("TimesPlayed"), 1)
        self.assertIn("Math.Clamp(value, 0, RequiredCombats)", code)
        self.assertIn('DynamicVars["Remaining"].BaseValue = RequiredCombats - _completedCombats', code)

    def test_native_enchantment_one_slot_and_local_preview(self):
        code = self.card()
        self.assertIn("CompletedCombats == RequiredCombats && Enchantment == null", code)
        self.assertIn("CardCmd.Enchant<TezcatarasEmber>(this, 1)", code)
        self.assertIn("if (LocalContext.IsMine(this)) EnchantmentVfxCmd.Preview(this)", code)
        self.assertNotIn("ClearEnchantment", code)
        self.assertNotIn("CombatEnchantmentCmd", code)

    def test_exact_text_conditional_is_not_combat_only(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        self.assertEqual(loc["MAIDEN_SUCCUBUS_CARD_FLAME_SWORD.description"],
            "造成{Damage:diff()}点伤害。\n完成5场战斗后，为这张牌[gold]附魔[/gold]：[purple]特兹卡塔拉的余烬[/purple]。{ShowRemaining:\n（还剩{Remaining:diff()}场战斗）|}")
        self.assertIn('description.Add("ShowRemaining", Enchantment is not TezcatarasEmber)', self.card())

    def test_native_contract_registered_and_old_probe_removed(self):
        catalog = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        self.assertIn("CustomVariants<FlameSword>(DesignSyncFlameSwordContract.Run, 35)", catalog)
        self.assertNotIn("sixth play applies Tezcatara", catalog)
        self.assertNotIn("six cross-combat-counted plays", catalog)

    def test_native_contract_observes_commands_saves_and_boundaries(self):
        code = read("src/Debugging/CardEffects/DesignSyncFlameSwordContract.cs")
        for term in ("await ctx.Play(card, ctx.PrimaryEnemy)", "deck.AfterCombatVictory(room)",
                     "card.AfterCombatVictory(room)", "IterateHookListeners(ctx.Combat).Contains(deck)",
                     "CardModel.FromSerializable(deck.ToSerializable())", "ctx.Combat.CloneCard(deck)",
                     "CardCmd.Enchant<Sharp>", "CardCmd.ClearEnchantment", "legacy.TimesPlayed = 6",
                     "old plays never converted", "next combat suffix remains hidden",
                     "duplicate room callback is idempotent", "generated.AfterCombatVictory",
                     "finally", "CardPileCmd.RemoveFromDeck"):
            self.assertIn(term, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
