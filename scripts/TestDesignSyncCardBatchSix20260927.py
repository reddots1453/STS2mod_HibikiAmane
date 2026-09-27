"""DS27 batch6 independent static contracts; runtime is ms_test_cards confirm ds27-batch6."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model
from AuditCardLocalization import registered_cards, source_metadata


class CardBatchSix(unittest.TestCase):
    def test_exact_approved_design(self):
        design = re.sub(r"\s", "", read("DesignDoc.md"))
        for expected in [
            "梦色的颜料技能牌罕见1/0费从抽牌堆中抽取堕落牌、圣洁牌和中立牌各1张。",
            "黑暗突刺攻击牌普通1费1欲望造成9/12点伤害。抽2张牌。",
            "瘴气吸收技能牌普通0费2欲望获得2/3费。",
            "沉溺快感技能牌普通1费获得7/10点格挡。抽2张牌。将2张发情洗入抽牌堆。",
            "背水一战攻击牌普通0费造成3/4点伤害。每有1层负面状态，额外造成3/4点伤害。（造成？点伤害）",
            "反射屏障技能牌普通1费获得8点格挡。当这张牌被消耗时，获得8点格挡和1层魔力增幅。升级后获得虚无。",
            "千咒之大镰攻击牌稀有2费造成8点伤害。被消耗时，这张牌在本局游戏中的伤害永久增加4/6。消耗。",
            "瘴炎攻击牌普通0费2欲望对所有敌人造成7/10点伤害并给予3层燃烧。",
            "终焉的斩击攻击牌普通1费造成9/11点伤害。选择抽牌堆的1/2张牌放入弃牌堆。",
        ]:
            with self.subTest(expected=expected):
                self.assertIn(expected, design)

    def test_metadata_from_all_source_not_only_leaf(self):
        _, classes = registered_cards()
        expected = {
            "DreamPigment": ("Uncommon", 1, 0), "DarkThrust": ("Common", 1, 1),
            "MiasmaAbsorption": ("Common", 0, 0), "LastStand": ("Common", 0, 0),
            "PleasureDrowning": ("Common", 1, 1), "ReflectiveBarrier": ("Common", 1, 1),
            "ThousandCurseScythe": ("Rare", 2, 2), "MiasmaFlame": ("Common", 0, 0),
            "FinalSlash": ("Common", 1, 1),
        }
        for name, values in expected.items():
            with self.subTest(name=name):
                data = source_metadata(name, classes)
                self.assertEqual((data["rarity"], data["cost"], data["upgradedCost"]), values)
                self.assertEqual(data["unresolvedFields"], [])

    def test_exact_changed_render_templates(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        self.assertEqual(loc["MAIDEN_SUCCUBUS_CARD_DREAM_PIGMENT.description"],
                         "从[gold]抽牌堆[/gold]中抽取[purple]堕落牌[/purple]、[purple]圣洁牌[/purple]和[gold]中立牌[/gold]各1张。")
        self.assertEqual(loc["MAIDEN_SUCCUBUS_CARD_LAST_STAND.description"],
                         "造成{CalculationBase:diff()}点伤害。\n每有1层负面状态，额外造成{ExtraDamage:diff()}点伤害。\n{InCombat:\n（造成{CalculatedDamage:diff()}点伤害）|}")
        remaining = {
            "DARK_THRUST": "造成{Damage:diff()}点伤害。\n抽2张牌。",
            "MIASMA_ABSORPTION": "获得{Energy:maidenEnergyIcons()}。",
            "PLEASURE_DROWNING": "获得{Block:diff()}点[gold]格挡[/gold]。\n抽{Cards:diff()}张牌。\n将2张[gold]发情[/gold]洗入[gold]抽牌堆[/gold]。",
            "REFLECTIVE_BARRIER": "获得{Block:diff()}点[gold]格挡[/gold]。\n当这张牌被[gold]消耗[/gold]时，获得{Block:diff()}点[gold]格挡[/gold]和1层[gold]魔力增幅[/gold]。",
            "THOUSAND_CURSE_SCYTHE": "造成{Damage:diff()}点伤害。\n被[gold]消耗[/gold]时，这张牌在本局游戏中的伤害永久增加{Growth:diff()}。",
            "MIASMA_FLAME": "对所有敌人造成{Damage:diff()}点伤害并给予{BurningPower:diff()}层[gold]燃烧[/gold]。",
            "FINAL_SLASH": "造成{Damage:diff()}点伤害。\n选择[gold]抽牌堆[/gold]的{Cards:diff()}张牌放入[gold]弃牌堆[/gold]。",
        }
        for key, description in remaining.items():
            with self.subTest(key=key):
                self.assertEqual(loc[f"MAIDEN_SUCCUBUS_CARD_{key}.description"], description)

    def test_dream_draw_lifecycle_and_route_not_pool(self):
        source = model("DreamPigment")
        for text in ["RouteCardQuery.TryGet", "RouteCardKind.Corrupt, RouteCardKind.Holy, RouteCardKind.Neutral",
                     "Hook.ShouldDraw", "Hook.AfterPreventingDraw", "Cards.Count >= 10",
                     "History.CardDrawn", "Hook.AfterCardDrawn", "selected.InvokeDrawn()"]:
            self.assertIn(text, source)
        self.assertNotIn("PileType.Discard", source)
        self.assertNotIn("Shuffle", source)
        self.assertNotIn("MagicResonance", source)

    def test_changed_values_and_upgrade_axes(self):
        self.assertIn("new DamageVar(9, ValueProp.Move)", model("DarkThrust"))
        self.assertIn("DynamicVars.Block.UpgradeValueBy(3)", model("PleasureDrowning"))
        self.assertNotIn("DynamicVars.Cards.UpgradeValueBy", model("PleasureDrowning"))
        self.assertIn("new BlockVar(8, ValueProp.Move)", model("ReflectiveBarrier"))
        self.assertIn("AddKeyword(CardKeyword.Ethereal)", model("ReflectiveBarrier"))
        self.assertNotIn("AddKeyword(CardKeyword.Exhaust)", model("ReflectiveBarrier"))
        self.assertIn('DynamicVars["Growth"].UpgradeValueBy(2)', model("ThousandCurseScythe"))
        self.assertIn("PermanentCardCmd.TryModifyDeckVersion", model("ThousandCurseScythe"))
        self.assertIn("new DamageVar(7, ValueProp.Move), new PowerVar<BurningPower>(3)", model("MiasmaFlame"))
        self.assertIn("DynamicVars.Damage.UpgradeValueBy(3)", model("MiasmaFlame"))
        self.assertIn("new DamageVar(9, ValueProp.Move)", model("FinalSlash"))

    def test_new_model_registered_and_runtime_suite_wired(self):
        contract = json.loads(read("docs/content_contract_20260824.json"))
        self.assertIn("DreamPigment", contract["cards"]["MSNeutralCardPool"])
        self.assertIn("MagicResonance", contract["cards"]["MSNeutralCardPool"])  # no silent save-ID reuse
        source = read("src/Debugging/CardEffects/DesignSyncCardBatchSixContract.cs")
        self.assertEqual(len(re.findall(r"new\(typeof\(", source)), 9)
        runner = read("src/Debugging/CardEffects/CardEffectTestRunner.cs")
        self.assertIn("DesignSyncCardBatchSixContract.Validate(context, card, scenario.Upgraded)", runner)
        self.assertIn('"ds27-batch6"', runner)
        catalog = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        for text in ["missing-holy", "near-full", "no-draw", "no discard reshuffle", "draw notifications",
                     "variation does not use original pool", "repeated exhaust compounds permanent growth",
                     "combat total updates to debuff layers", "played barrier is discarded, not exhausted"]:
            self.assertIn(text, catalog)


if __name__ == "__main__":
    unittest.main(verbosity=2)
