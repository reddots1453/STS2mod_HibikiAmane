"""DS27-02D static contracts; game execution is a separate ds27-holy suite."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model


class HolyDesignSync(unittest.TestCase):
    def test_approved_design_is_still_current(self):
        design = re.sub(r"\s", "", read("DesignDoc.md"))
        for expected in [
            "光子伏特攻击牌普通1费造成10/12点伤害。如果小于等于2点欲望，获得1/2层魔力增幅。",
            "武神的呼吸能力牌稀有1费当你虚弱时，额外获得50%/75%格挡。当你脆弱时，额外造成50%/75%伤害。",
            "太阳之舞技能牌罕见0费每拥有1层增益效果，在本回合中获得1层敏捷。消耗。升级后获得保留。",
            "镇静之雾技能牌普通1费抽1/2张牌。对一名角色给予2层虚弱。",
            "万劫不复能力牌稀有2/1费断罪审判不再清除断罪层数。沉底。",
            "火刑架技能牌普通0费给予2/3层燃烧。获得2/3点格挡。",
            "退魔香水技能牌普通0费失去10/15点诱惑度。抽1张牌。",
            "光之审判技能牌罕见1/0费给予1层断罪，并触发断罪审判。魔力解放：对其他敌人造成断罪审判的等量伤害。",
            "纯净宝珠技能牌罕见1/0费失去2欲望。获得1层魔装耐久。失去10诱惑度。消耗。随身。",
            "开祷技能牌普通0费接下来2/3个回合开始时，获得1层魔力增幅。",
        ]:
            with self.subTest(expected=expected):
                self.assertIn(expected, design)

    def test_changed_text_preserves_exact_punctuation(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        expected = {
            "PHOTON_VOLT": "造成{Damage:diff()}点伤害。\n如果小于等于{energyPrefix:maidenDesireIcons(2)}，获得{MagicAmplificationPower:diff()}层[gold]魔力增幅[/gold]。",
            "CALMING_MIST": "抽{Cards:diff()}张牌。\n对一名角色给予2层[gold]虚弱[/gold]。",
            "INWARD_DISCIPLINE": "当你[gold]虚弱[/gold]时，额外获得{InwardDisciplinePower:diff()}%[gold]格挡[/gold]。\n当你[gold]脆弱[/gold]时，额外造成{InwardDisciplinePower:diff()}%伤害。",
            "BURNING_RACK": "给予{BurningPower:diff()}层[gold]燃烧[/gold]。\n获得{Block:diff()}点[gold]格挡[/gold]。",
        }
        for key, text in expected.items():
            self.assertEqual(loc[f"MAIDEN_SUCCUBUS_CARD_{key}.description"], text)
        powers = json.loads(read("MaidenSuccubus/localization/zhs/powers.json"))
        self.assertEqual(powers["MAIDEN_SUCCUBUS_POWER_INWARD_DISCIPLINE_POWER.title"], "武神的呼吸")
        self.assertEqual(powers["MAIDEN_SUCCUBUS_POWER_INWARD_DISCIPLINE_POWER.smartDescription"],
                         "当你[gold]虚弱[/gold]时，额外获得{Amount}%[gold]格挡[/gold]。当你[gold]脆弱[/gold]时，额外造成{Amount}%伤害。")

    def test_photon_upgrade_changes_damage_and_amplification(self):
        source = model("PhotonVolt")
        self.assertIn("Damage.UpgradeValueBy(2)", source)
        self.assertIn('DynamicVars["MagicAmplificationPower"].UpgradeValueBy(1)', source)
        self.assertIn('DynamicVars["MagicAmplificationPower"].BaseValue', source)
        self.assertIn("Data.Desire.Get(Owner) <= 2", source)

    def test_burning_rack_is_block_not_weak_and_exhaust(self):
        source = model("BurningRack")
        self.assertIn("GainsBlock => true", source)
        self.assertIn("new BlockVar(2, ValueProp.Move)", source)
        self.assertIn("CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play)", source)
        self.assertNotIn("WeakPower", source)
        self.assertNotIn("CardKeyword.Exhaust", source)

    def test_targeting_scope_and_cleanup(self):
        source = read("src/Patches/Iteration1CardTargetPatch.cs")
        self.assertIn("card is Stigma or CalmingMist", source)
        self.assertIn("if (AllowsFriendlyTarget(__instance))", source)
        self.assertIn("AllowsFriendlyTarget(card.Model)", source)
        self.assertIn("Safe.Run(StigmaTargetingStartPatch.Reset", source)
        self.assertIn("StigmaTargetingStartPatch.IsActive && creature.IsAlive", source)

    def test_runtime_suite_has_independent_metadata_and_real_effects(self):
        source = read("src/Debugging/CardEffects/DesignSyncHolyContract.cs")
        self.assertEqual(len(re.findall(r"new\(typeof\(", source)), 10)
        runner = read("src/Debugging/CardEffects/CardEffectTestRunner.cs")
        self.assertIn("DesignSyncHolyContract.Validate(context, card, scenario.Upgraded)", runner)
        self.assertIn('"ds27-holy"', runner)
        catalog = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        for expected in ["rack is reusable", "selected character receives weak", "real block while weak", "real damage while frail", "judgment upgrade does not grant retain", "release copies exact judgment damage to other enemy", "prayer removed after final scheduled turn"]:
            self.assertIn(expected, catalog)


if __name__ == "__main__":
    unittest.main(verbosity=2)
