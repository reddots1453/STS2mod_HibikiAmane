"""Static DS27 armour integration checks; real commands use ds27-transformation."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


def power(path, name):
    source = read(path).split("public sealed class " + name, 1)[1]
    return source.split("[RegisterPower]", 1)[0]


class TransformationContract(unittest.TestCase):
    def test_approved_design_boundaries(self):
        design = read("DesignDoc.md")
        for text in ["魔装耐久上限为5层", "进入任何变身时，将魔装耐久重置为3层",
                     "损失前至少有1层时，使生命损失减少33%", "正数降至0时保持变身",
                     "一次损失即使超过剩余耐久也只降至0", "普通形态不能获得魔装耐久",
                     "0层耐久仍不能作为魔力解放的支付资源", "0层时再次尝试损失耐久不获得欲望"]:
            self.assertIn(text, design)

    def test_command_cap_payment_and_single_loss(self):
        source = read("src/Core/Transformation/TransformationCmd.cs")
        self.assertIn("const int InitialArmor = 3", source)
        self.assertIn("const int MaxArmor = 5", source)
        self.assertIn("MaxArmor - (int)(armor?.Amount ?? 0)", source)
        self.assertIn("-Math.Min(amount, (int)armor.Amount)", source)
        self.assertIn("await Exit(choiceContext, creature)", source)
        self.assertIn("return GetArmor(creature) is { Amount: > 0 }", source)
        self.assertNotIn("PowerCmd.Decrement(armor)", source)
        for file in ["src/Cards/MvpGeneratedCards.cs", "src/Cards/Curses/SemenCurse.cs"]:
            self.assertNotIn("PowerCmd.Decrement(armor)", read(file))
            self.assertIn("TransformationCmd.LoseArmor", read(file))

    def test_preview_is_pure_and_zero_has_actual_damage_callback(self):
        armor = power("src/Powers/TransformationPowers.cs", "MagicArmorPower")
        modifier = armor.split("public override decimal ModifyHpLostAfterOstyLate", 1)[1].split(
            "public override async Task AfterModifyingHpLostAfterOsty", 1)[0]
        self.assertIn("amount * 0.67m", modifier)
        self.assertNotIn("UsedThisTurn =", modifier)
        self.assertNotIn("_pendingDecrement", armor)
        actual = armor.split("public override async Task AfterDamageReceived", 1)[1]
        for guard in ["result.UnblockedDamage <= 0", "UsedThisTurn", "CombatSide.Enemy",
                      "ValueProp.Move", "ValueProp.Unpowered"]:
            self.assertIn(guard, actual)
        self.assertIn("TransformationCmd.LoseArmor(context, Owner, 1, cardSource)", actual)

    def test_zero_counter_patch_is_scoped_and_safe(self):
        patch = read("src/Patches/MagicArmorLifetimePatch.cs")
        for term in ["Safe.Run", "MagicArmorPower { Amount: 0 }", "IsTransformed(__instance.Owner)",
                     "ShouldRemoveDueToAmount", "if (preserve) __result = false"]:
            self.assertIn(term, patch)
        visual = read("src/UI/MaidenSuccubusCreatureVisuals.cs")
        self.assertIn('0 or 1 when transformed => prefix + "1.png"', visual)
        temptation = read("src/Core/Temptation/Temptation.cs")
        self.assertNotIn("TransformationCmd.MaxArmor", temptation)
        self.assertIn("TransformationCmd.InitialArmor", temptation)

    def test_form_mutual_exclusion_and_dialogue(self):
        source = read("src/Core/Transformation/TransformationCmd.cs")
        self.assertGreaterEqual(source.count("ImmaculateRobePower or CorruptRobePower or EternalRobePower"), 2)
        self.assertIn("if (creature.HasPower<T>()) return", source)
        self.assertIn("Enter<EternalRobePower>(choiceContext, creature, source, 9)", source)
        eternal = power("src/Powers/Iteration1HolyPowers.cs", "EternalRobePower")
        self.assertIn("AfterPlayerTurnStart", eternal)
        self.assertIn("Owner, 9, Owner, null", eternal)
        self.assertNotIn("GetPower<EternalRobePower>()?.Amount", read("src/Powers/TransformationPowers.cs"))
        dialogue = json.loads(read("MaidenSuccubus/localization/zhs/combat_messages.json"))
        self.assertEqual(dialogue["MAIDENSUCCUBUS_ALREADY_TRANSFORMED"], "我已经变身了！")

    def test_exact_armour_text_and_line_breaks(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/powers.json"))
        text = re.sub(r"\[/?\w+\]", "", loc["MAIDEN_SUCCUBUS_POWER_MAGIC_ARMOR_POWER.description"])
        self.assertEqual(text, "每回合第一次受到未格挡的攻击伤害时，失去1层魔装耐久；损失前至少有1层时，使生命损失减少33%。\n"
                              "低魔装耐久将会提高诱惑度，从而导致敌人发动色情攻击。\n"
                              "魔装耐久降至0后，再次失去魔装耐久时将解除变身。")
        hover = read("MaidenSuccubus/localization/zhs/static_hover_tips.json")
        self.assertNotIn("降至0时立即解除", hover)

    def test_real_command_oracles_and_test_registration(self):
        source = read("src/Debugging/CardEffects/DesignSyncTransformationContract.cs")
        for name in ["ds27-armour-lifecycle", "ds27-attack-boundaries", "ds27-form-switching", "ds27-release-payment"]:
            self.assertIn(name, source)
        for command in ["CreatureCmd.Damage", "PowerCmd.Decrement", "CardSelectCmd.UseSelector",
                        "ShouldRemoveDueToAmount", "await ctx.Play", "BeforeSideTurnStart"]:
            self.assertIn(command, source)
        self.assertGreaterEqual(source.count("ctx.Assert"), 50)
        self.assertIn("DesignSyncTransformationContract.Extend(_specs)", read("src/Debugging/CardEffects/CardEffectTestCatalog.cs"))
        self.assertIn('"ds27-transformation"', read("src/Debugging/CardEffects/CardEffectTestRunner.cs"))


if __name__ == "__main__":
    unittest.main()
