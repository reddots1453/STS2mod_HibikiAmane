"""Static wiring checks only. Real codec and in-game card suites run separately."""
import json
import unittest
from TestDesignSyncNeutral20260927 import read


class LightWingsContract(unittest.TestCase):
    def test_card_metadata_and_upgrade(self):
        code = read("src/Cards/Iteration2ExpansionCards.cs").split("public sealed class LightWings", 1)[1].split("[RegisterCard", 1)[0]
        for term in ["base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)",
                     "new DamageVar(9, ValueProp.Move)", "Damage.UpgradeValueBy(3)"]:
            self.assertIn(term, code)

    def test_exact_text_and_sentence_layout(self):
        text = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))["MAIDEN_SUCCUBUS_CARD_LIGHT_WINGS.description"]
        self.assertEqual(text, "造成{Damage:diff()}点伤害。\n抽1张[gold]附魔牌[/gold]。\n可以被多重[gold]附魔[/gold]。")
        self.assertIn("造成9/12点伤害。抽1张附魔牌。可以被多重附魔。", read("DesignDoc.md"))

    def test_only_light_wings_gets_open_slot(self):
        code = read("src/Enchantments/LayeredEnchantments.cs")
        self.assertIn("Supports(CardModel card) => card is LightWings", code)
        self.assertIn("if (!Supports(card) || incoming is LayeredEnchantment)", code)
        self.assertIn("if (!incoming.CanEnchant(card))", code)
        patch = read("src/Patches/LayeredEnchantmentPatches.cs")
        self.assertIn("if (!LayeredEnchantments.Supports(card)) return true", patch)

    def test_eligibility_scope_cannot_cross_await(self):
        code = read("src/Enchantments/LayeredEnchantments.cs")
        self.assertNotIn("await ", code)
        self.assertIn("_eligibilityCard = previous", code)
        patch = read("src/Patches/LayeredEnchantmentPatches.cs")
        self.assertIn("Finalizer(Exception? __exception, IDisposable? __state)", patch)
        self.assertIn("__state?.Dispose()", patch)
        self.assertIn("return __exception", patch)

    def test_real_children_not_amount_only_for_glam(self):
        code = read("src/Enchantments/LayeredEnchantment.cs")
        for term in ["incoming.IsStackable", "existing.Amount += (int)amount", "_layers.Add(incoming)",
                     "incoming.ModifyCard()", "layer.EnchantPlayCount(current)"]:
            self.assertIn(term, code)

    def test_direct_modifiers_and_on_play_forwarded(self):
        code = read("src/Enchantments/LayeredEnchantment.cs")
        for term in ["Layers.Sum(layer => layer.EnchantDamageAdditive", "factor * layer.EnchantDamageMultiplicative",
                     "Layers.Sum(layer => layer.EnchantBlockAdditive", "factor * layer.EnchantBlockMultiplicative",
                     "await layer.OnPlay(context, play)", "layer.InvokeExecutionFinished()"]:
            self.assertIn(term, code)

    def test_both_listener_streams_expand_only_containers(self):
        patch = read("src/Patches/LayeredEnchantmentPatches.cs")
        self.assertIn("typeof(CombatState), nameof(CombatState.IterateHookListeners)", patch)
        self.assertIn("typeof(RunState), nameof(RunState.IterateHookListeners)", patch)
        code = read("src/Enchantments/LayeredEnchantments.cs")
        self.assertIn("if (model is LayeredEnchantment layered)", code)
        self.assertIn("if (layers == null) { yield return model; continue; }", code)
        self.assertIn("!layer.Card.HasBeenRemovedFromState", code)

    def test_clone_rebind_and_clear_lifecycle(self):
        code = read("src/Enchantments/LayeredEnchantment.cs")
        for term in ["layer.ClonePreservingMutability()", "if (!layer.HasCard) layer.ApplyInternal(Card, layer.Amount)",
                     "if (layer.HasCard) layer.ClearInternal()"]:
            self.assertIn(term, code)
        self.assertIn("layered.ClearChildren()", read("src/Patches/LayeredEnchantmentPatches.cs"))

    def test_native_savedproperties_payload_is_shared_with_executable_host(self):
        code = read("src/Enchantments/LayeredEnchantment.cs")
        for term in ["[SavedProperty]", "public string LayerData", "LayeredEnchantmentSerialization.Serialize",
                     "LayeredEnchantmentSerialization.Deserialize", "EnchantmentModel.FromSerializable",
                     "Nested layered enchantments are not supported"]:
            self.assertIn(term, code)
        self.assertIn("IncludeFields = true", read("src/Enchantments/LayeredEnchantmentSerialization.cs"))
        self.assertIn("../../src/Enchantments/LayeredEnchantmentSerialization.cs", read("tests/LayeredSaveContracts/LayeredSaveContracts.csproj"))

    def test_permanent_and_temporary_commands_remain_separate(self):
        self.assertIn("CardsEnchanted.Add", read("src/Patches/LayeredEnchantmentPatches.cs"))
        code = read("src/Commands/CombatEnchantmentCmd.cs")
        self.assertIn("EnsureCombatClone(card)", code)
        self.assertIn("LayeredEnchantments.Apply(enchantment, card, amount)", code)
        self.assertNotIn("CardsEnchanted.Add", code)
        self.assertNotIn("DeckVersion =", read("src/Enchantments/LayeredEnchantments.cs"))

    def test_native_clone_rest_supplements_only_layered_cards_after_success(self):
        code = read("src/Patches/LayeredCloneRestSitePatch.cs")
        for term in ["card.Enchantment is LayeredEnchantment", "Layers.OfType<Clone>()", "if (!await original) return false",
                     "CloneCard(card)", "await CardPileCmd.Add(clone, PileType.Deck)"]:
            self.assertIn(term, code)
        self.assertNotIn("ThreadStatic", code)

    def test_selectors_and_layer_identity_consumers_updated(self):
        for file in ["src/Cards/MvpAdvancedCards.cs", "src/Cards/MvpHolyCardsBatch3.cs", "src/Cards/MvpStrengthCards.cs",
                     "src/Relics/FourthRouteRelics.cs", "src/Patches/MvpEventPatches.cs"]:
            self.assertIn("LayeredEnchantments.HasOpenSlot", read(file))
        self.assertIn("LayeredEnchantments.Has<SoulLinkEnchantment>", read("src/Enchantments/MvpEnchantments.cs"))
        self.assertIn("LayeredEnchantments.Has<InfectionEnchantment>", read("src/Patches/InfectionOverlayPatch.cs"))

    def test_explicit_no_existing_enchantment_effects_not_widened(self):
        self.assertIn("rule.Enchant && card.Enchantment == null", read("src/Relics/RetentionOrbs.cs"))
        self.assertIn("candidate.Enchantment == null", read("src/Enchantments/InfectionHandSnapshot.cs"))

    def test_display_lists_actual_children_and_counts(self):
        code = read("src/Enchantments/LayeredEnchantment.cs")
        self.assertIn("DisplayAmount => _layers.Count", code)
        self.assertIn("Layers.GroupBy(layer => layer.Id)", code)
        self.assertIn("Layers.SelectMany(layer => layer.HoverTips)", code)
        patch = read("src/Patches/LayeredEnchantmentPatches.cs")
        self.assertIn("layer.DynamicExtraCardText?.GetFormattedText()", patch)
        self.assertIn('result.Add("LayerSummary", layered.Summary)', patch)

    def test_in_game_suite_uses_real_execution_and_independent_oracles(self):
        code = read("src/Debugging/CardEffects/DesignSyncLightWingsContract.cs")
        for term in ["await ctx.Play(layered, ctx.PrimaryEnemy)", "upgraded ? 51 : 42", "upgraded ? 27 : 20",
                     "layered.CreateClone()", "LoadCard(saved, ctx.Player)", "CardModel.FromSerializable",
                     "legacy cost mutation not applied twice", "Hook.BeforeFlush", "CloneRestSiteOption(ctx.Player).OnSelect()",
                     "combatCopy.DeckVersion = deck", "finally { TestMode.IsOn = previousTestMode; }"]:
            self.assertIn(term, code)
        self.assertIn("CustomVariants<LightWings>(DesignSyncLightWingsContract.Run, 35)", read("src/Debugging/CardEffects/CardEffectTestCatalog.cs"))


if __name__ == "__main__":
    unittest.main()
