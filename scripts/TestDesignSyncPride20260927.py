"""Pride design text, unchanged rule wiring, and explicit native gameplay regression coverage."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


def relic():
    return read("src/Relics/FourthRouteRelics.cs").split("public sealed class PrideRouteRelic", 1)[1].split("[RegisterRelic", 1)[0]


def plain(text):
    return re.sub(r"\[/?gold\]", "", text)


class PrideContracts(unittest.TestCase):
    def test_exact_stage_text_and_color(self):
        design = read("DesignDoc.md").split("#### 傲慢\n", 1)[1].split("####", 1)[0]
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        prefix = "MAIDEN_SUCCUBUS_RELIC_PRIDE_ROUTE_RELIC.descriptionStage"
        for stage, label in ((1, "残缺"), (2, "完整"), (4, "觉醒")):
            expected = re.search(rf"^{label}：(.*)$", design, re.M).group(1)
            self.assertEqual(expected, plain(loc[prefix + str(stage)]))
            self.assertIn("[gold]力量[/gold]", loc[prefix + str(stage)])
        self.assertEqual(loc[prefix + "3"], loc[prefix + "4"])

    def test_power_description_exact_design_in_both_views(self):
        design = read("DesignDoc.md").split("#### 傲慢\n", 1)[1].split("####", 1)[0]
        expected = re.search(r"^自负：(.*)$", design, re.M).group(1)
        loc = json.loads(read("MaidenSuccubus/localization/zhs/powers.json"))
        for suffix in ("description", "smartDescription"):
            actual = loc["MAIDEN_SUCCUBUS_POWER_SELF_IMPORTANT_POWER." + suffix]
            self.assertEqual(expected, plain(actual))
            self.assertNotIn("每次失去生命值", actual)

    def test_relic_retains_design_stage_grants(self):
        code = relic()
        for text in ("if (Stage == 0) return", "int amount = Math.Min(Stage, 3)",
                     "PowerCmd.Apply<StrengthPower>", "PowerCmd.Apply<SelfImportantPower>"):
            self.assertIn(text, code)

    def test_power_retains_owner_unblocked_counter_debuff_rules(self):
        code = read("src/Powers/FourthRoutePowers.cs")
        for text in ("PowerType.Debuff", "PowerStackType.Counter", "target != Owner",
                     "result.UnblockedDamage <= 0", "Amount <= 0", "Owner, -1, Owner, null"):
            self.assertIn(text, code)
        self.assertNotIn("AfterPowerAmountChanged", code)
        self.assertNotIn("AfterRemoved", code)
        self.assertNotIn("props.IsPoweredAttack", code)

    def test_hover_tips_only_when_relic_active(self):
        self.assertIn("AdditionalHoverTips => Stage == 0", relic())
        for text in ("HoverTipFactory.FromPower<StrengthPower>()", "HoverTipFactory.FromPower<SelfImportantPower>()"):
            self.assertIn(text, relic())
        self.assertIn("HoverTipFactory.FromPower<StrengthPower>()", read("src/Powers/FourthRoutePowers.cs"))
        self.assertIn("HoverTipFactory.Static(StaticHoverTip.Block)", read("src/Powers/FourthRoutePowers.cs"))

    def test_real_damage_commands_cover_block_and_owner(self):
        code = read("src/ConsoleCommands/DesignPrideTestConsoleCmd.cs")
        for text in ("CreatureCmd.Damage", "CreatureCmd.GainBlock", "fully blocked and zero damage do not spend layers",
                     "partial block spends one layer not damage count", "damage to someone else does not spend own layer",
                     "each hit spends one layer until exhausted", "last layer removes power and cannot recur",
                     "unblocked nonattack damage consumes final layer and can lower strength below zero"):
            self.assertIn(text, code)

    def test_actual_purification_and_saved_stage_cases(self):
        code = read("src/ConsoleCommands/DesignPrideTestConsoleCmd.cs")
        for text in ("purification.GetCandidates()", "purification.AfterPlayerTurnStart(choice, player)",
                     "purification removes layer without losing strength", "SavedProperties.From(relic)!.Fill(restored)",
                     "saved stage restores correct new-combat grant", "fresh combat recreates full grant"):
            self.assertIn(text, code)

    def test_destructive_debug_entry_is_guarded(self):
        code = read("src/ConsoleCommands/DesignPrideTestConsoleCmd.cs")
        for text in ("#if DEBUG", 'args[0] != "confirm"', "issuingPlayer.RunState.Players.Count != 1",
                     "!CombatManager.Instance.IsInProgress", "_running", "TestMode.IsOn = previousTestMode"):
            self.assertIn(text, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
