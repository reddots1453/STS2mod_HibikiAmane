"""DS27-02O wiring/text regressions, not an engine-execution substitute."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model


class CurseInfectionContracts(unittest.TestCase):
    def test_complete_design_contract(self):
        text = re.sub(r"\s", "", read("DesignDoc.md"))
        self.assertIn("咒印传染技能牌罕见2/1费当这张牌被消耗时，抽2张牌，并将这一效果附加到随机手牌上。消耗。", text)
        self.assertIn("不是附魔。不能叠加给已有该效果的牌。可以附加给所有手牌包括状态牌和诅咒牌。被附加此效果的卡牌追加卡面文字和悬停说明。", text)

    def test_card_metadata_and_native_exhaust_trigger(self):
        code = model("CurseInfection")
        for part in ("base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)", "[CardKeyword.Exhaust]",
                     "EnergyCost.UpgradeBy(-1)", "if (card != this) return", "CardPileCmd.Draw(context, 2, Owner)"):
            self.assertIn(part, code)
        self.assertLess(code.index("CardPileCmd.Draw"), code.index("TryApplyToRandomHandCard"))

    def test_annotation_not_enchantment_and_accepts_all_types(self):
        code = read("src/Cards/CurseInfectionStatus.cs")
        self.assertIn("card.AddKeyword(CurseInfectionKeyword.Value)", code)
        self.assertIn("card is CurseInfection || Has(card)", code)
        self.assertIn("card is not CurseInfection && !Has(card)", code)
        self.assertNotIn("CardType.", code)
        self.assertNotIn("CardCmd.Enchant", code)
        self.assertNotIn("DeckVersion", code)
        self.assertIn("StableShuffle(player.RunState.Rng.CombatCardSelection)", code)

    def test_combat_annotation_survives_native_card_save_payload(self):
        card = model("CurseInfection")
        code = read("src/Cards/CurseInfectionStatus.cs")
        self.assertIn("[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]", card)
        self.assertIn("nameof(CurseInfection.CurseInfectionAnnotationMarker)", code)
        self.assertIn("nameof(CardModel.ToSerializable)", code)
        self.assertIn("nameof(CardModel.FromSerializable)", code)
        self.assertIn("__result.Props.bools.Add", code)
        self.assertIn("CurseInfectionStatus.TryApply(__result)", code)

    def test_async_wrapper_waits_and_does_not_swallow_original_failure(self):
        code = read("src/Cards/CurseInfectionStatus.cs")
        wrapper = code[code.index("private static async Task ResolveAfterOriginal"):]
        self.assertLess(wrapper.index("await original;"), wrapper.index("try"))
        self.assertLess(wrapper.index("await original;"), wrapper.index("CurseInfectionStatus.ResolveAfterExhaust"))
        self.assertIn("Task wrapped = original", code)
        self.assertIn("Safe.Run(() => wrapped = ResolveAfterOriginal", code)
        self.assertIn("__result = wrapped", code)

    def test_text_patch_preserves_original_on_exception(self):
        code = read("src/Cards/CurseInfectionStatus.cs")
        text_patch = code[code.index("public static class CurseInfectionCardTextPatch"):]
        for part in ("string result = __result", "Safe.Run(() =>", "__result = result", "[purple]{extra}[/purple]"):
            self.assertIn(part, text_patch)
        self.assertIn("CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None",
                      read("src/Keywords/CurseInfectionKeyword.cs"))

    def test_exact_card_extra_and_hover_text(self):
        sentence = "当这张牌被[gold]消耗[/gold]时，抽2张牌，并将这一效果附加到随机[gold]手牌[/gold]上。"
        cards = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        tips = json.loads(read("MaidenSuccubus/localization/zhs/static_hover_tips.json"))
        keywords = json.loads(read("MaidenSuccubus/localization/zhs/card_keywords.json"))
        self.assertEqual(sentence, cards["MAIDEN_SUCCUBUS_CARD_CURSE_INFECTION.description"])
        self.assertEqual(sentence, tips["MAIDENSUCCUBUS_CURSE_INFECTION.extraCardText"])
        for description in (tips["MAIDENSUCCUBUS_CURSE_INFECTION.description"],
                            keywords["MAIDEN_SUCCUBUS_KEYWORD_CURSE_INFECTION.description"]):
            self.assertEqual(sentence + "不能叠加给已有该效果的牌。", description)

    def test_game_contract_covers_all_types_real_exhaust_copy_and_text(self):
        code = read("src/Debugging/CardEffects/DesignSyncCurseInfectionContract.cs")
        for part in ("typeof(MaidenStrike), typeof(MaidenDefend), typeof(MagicIndex), typeof(Dazed), typeof(Injury)",
                     "await CardCmd.Exhaust", "source draws exactly two not four", "recipient passes effect exactly once",
                     "CheckText(target)", "annotation hover has exact full text", "copy retains enchantment",
                     "save payload carries combat annotation", "CardModel.FromSerializable(saved)",
                     "fresh permanent clone is unmarked"):
            self.assertIn(part, code)
        self.assertIn("CustomVariants<CurseInfection>(DesignSyncCurseInfectionContract.Run, 65)",
                      read("src/Debugging/CardEffects/CardEffectTestCatalog.cs"))

    def test_game_contract_covers_full_empty_and_async_failure(self):
        code = read("src/Debugging/CardEffects/DesignSyncCurseInfectionContract.cs")
        for part in ("full hand leaves two undrawn cards", "full hand still receives effect",
                     "empty deck still transfers to existing curse", "empty hand transfer is a no-op",
                     "Task.FromCanceled", "Task.FromException", "CurseInfectionExhaustPatch.Postfix",
                     "adapter waits for full original completion", "successful original draws once afterwards"):
            self.assertIn(part, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
