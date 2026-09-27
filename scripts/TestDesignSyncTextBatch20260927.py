"""DS27-02L exact source-text contracts; engine rendering is tested separately."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model


EXPECTED = {
    "PLAYING_WITH_FIRE": "获得1层[gold]燃烧[/gold]。\n抽{Cards:diff()}张牌。",
    "BURNING_BLADE_RITUAL": "[gold]消耗[/gold]1张牌。\n造成{Damage:diff()}点伤害。",
    "MIASMA_AFFINITY": "从[gold]抽牌堆[/gold]选择{Cards:diff()}张牌放入[gold]手牌[/gold]，并使其获得[gold]虚无[/gold]。",
    "PRICE_OF_STRENGTH": "获得{StrengthPower:diff()}点[gold]力量[/gold]。\n获得{ShatterPower:diff()}层[gold]破碎[/gold]。",
    "DESIRE_WARD": "造成{Damage:diff()}点伤害。\n防止下一次增加[pink]欲望[/pink]。",
    "WORSHIP": "获得{DexterityPower:diff()}层[gold]敏捷[/gold]。\n获得{PurificationPower:diff()}层[gold]净化[/gold]。",
    "JUDGMENT": "造成{Damage:diff()}点伤害。\n给予{Condemnation:diff()}层[gold]断罪[/gold]。\n[gold]魔力解放[/gold]：额外给予1层[gold]断罪[/gold]。",
    "ORIGINAL_SIN_BRAND": "对所有敌人给予{Condemnation:diff()}层[gold]断罪[/gold]。\n选择[gold]弃牌堆[/gold]{Cards:diff()}张牌加入[gold]手牌[/gold]。",
    "FORGE_NIMBLE": "获得{Block:diff()}点[gold]格挡[/gold]。\n为1张牌[gold]附魔[/gold]：[purple]伶俐[/purple]。",
    "FORGE_CHARGE": "从[gold]弃牌堆[/gold]中选择1张牌置于牌堆顶，并为它[gold]附魔[/gold]：[purple]充能：{Charge:diff()}[/purple]。",
    "JUDGMENT_BLADE": "造成{CalculationBase:diff()}点伤害。\n目标每有一层负面效果，额外造成{ExtraDamage:diff()}点伤害。\n{InCombat:（造成{CalculatedDamage:diff()}点伤害）|}",
    "HEALING_ART": "恢复{Heal:diff()}点生命值。\n每拥有1层增益效果，额外恢复{PerBuff:diff()}点生命值。\n{InCombat:（恢复{CalculatedHeal:diff()}点生命值）|}",
}


class TextBatchContracts(unittest.TestCase):
    def test_exact_twelve_templates_including_punctuation_colors_and_newlines(self):
        actual = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        self.assertEqual(len(EXPECTED), 12)
        for name, text in EXPECTED.items():
            with self.subTest(card=name):
                self.assertEqual(actual[f"MAIDEN_SUCCUBUS_CARD_{name}.description"], text)

    def test_design_anchors_preserve_punctuation_and_metadata(self):
        design = re.sub(r"\s", "", read("DesignDoc.md"))
        snippets = [
            "玩火技能牌罕见0费获得1层燃烧。抽2/3张牌。",
            "焚刃祭仪攻击牌普通1费消耗1张牌。造成10/13点伤害。",
            "瘴气亲和技能牌稀有0费从抽牌堆选择1/2张牌放入手牌，并使其获得虚无。虚无。",
            "力量的代价能力牌罕见1费获得4点力量。获得4层破碎。虚无。升级后移除虚无。",
            "挥剑压制攻击牌罕见1费造成8/11点伤害。防止下一次增加欲望。",
            "净化之叶能力牌罕见1费获得1/2层敏捷。获得1/2层净化。",
            "裁决！攻击牌普通1费造成7点伤害。给予2/3层断罪。魔力解放：额外给予1层断罪。",
            "原罪烙印技能牌罕见1费对所有敌人给予2层断罪。选择弃牌堆1/2张牌加入手牌。",
            "锻成・伶俐技能牌普通1费获得5/8点格挡。为1张牌附魔：伶俐。",
            "锻成・充能技能牌罕见1费从弃牌堆中选择1张牌置于牌堆顶，并为它附魔：充能：2/3。消耗。",
            "审判之刃攻击牌稀有1费造成7点伤害。目标每有一层负面效果，额外造成3/5点伤害。（造成？点伤害）",
            "高级治疗技能牌稀有2费恢复4/8点生命值。每拥有1层增益效果，额外恢复2点生命值。消耗。（恢复？点生命值）",
        ]
        for text in snippets:
            with self.subTest(design=text):
                self.assertIn(text, design)

    def test_empty_hand_does_not_gate_ritual_damage(self):
        code = model("BurningBladeRitual")
        self.assertNotIn("return;", code)
        self.assertIn("if (selected != null)", code)
        self.assertIn("card => card != this", code)
        self.assertLess(code.index("await CardCmd.Exhaust"), code.index("await DamageCmd.Attack"))

    def test_keywords_remain_native_and_not_duplicated_in_json(self):
        for name, keyword in (("MiasmaAffinity", "Ethereal"), ("PriceOfStrength", "Ethereal"),
                              ("ForgeCharge", "Exhaust"), ("HealingArt", "Exhaust")):
            self.assertIn(f"[CardKeyword.{keyword}]", model(name))
        self.assertIn("RemoveKeyword(CardKeyword.Ethereal)", model("PriceOfStrength"))
        for name in ("FORGE_CHARGE", "HEALING_ART"):
            self.assertNotIn("消耗", EXPECTED[name])

    def test_healing_layout_only_reorders_native_keyword_safely(self):
        code = read("src/Patches/ComputedCardDescriptionLayoutPatch.cs")
        for term in ("__instance is not (HealingArt or JudgmentBlade)", "__instance is not HealingArt", "Safe.Run", 'new LocString("card_keywords", "EXHAUST.title")',
                     "exhaust <= preview", "lines.RemoveAt(exhaust)", "lines.Insert(preview, keyword)"):
            self.assertIn(term, code)
        self.assertNotIn("RemoveKeyword", code)

    def test_combat_preview_is_conditional_without_extra_blank_line(self):
        for name in ("JUDGMENT_BLADE", "HEALING_ART"):
            self.assertIn("。\n{InCombat:（", EXPECTED[name])
            self.assertTrue(EXPECTED[name].endswith("）|}"))

    def test_native_contract_and_suite_cover_all_twelve(self):
        code = read("src/Debugging/CardEffects/DesignSyncTextBatchContract.cs")
        types = code.split("internal static readonly Type[] Types =", 1)[1].split("];", 1)[0]
        self.assertEqual(len(re.findall(r"typeof\(\w+\)", types)), 12)
        for term in ("UpdateDynamicVarPreview", "GetDescriptionForPile", "RunState.CreateCard",
                     "CardCmd.Upgrade(outside)", "effect: false", "PileType.Deck", "PileType.Hand"):
            self.assertIn(term, code)
        runner = read("src/Debugging/CardEffects/CardEffectTestRunner.cs")
        self.assertIn('"ds27-text"', runner)
        self.assertIn("batch.Length != 12", runner)
        self.assertIn("DesignSyncTextBatchContract.Validate(context, card, scenario.Upgraded)", runner)

    def test_runtime_boundary_probes_are_wired(self):
        code = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        self.assertIn("CustomVariants<BurningBladeRitual>", code)
        for term in ("no other hand card still deals independent damage", "damage after actual exhaustion",
                     "healing preview after buff removal", "healing still exhausts once",
                     "clearing target resets damage preview", "retargeting restores calculated preview"):
            self.assertIn(term, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
