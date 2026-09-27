"""Stage descriptions and real draw/cost/prevention test wiring; X/Y remains Q17."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


def relic(name):
    return read("src/Relics/FourthRouteRelics.cs").split(f"public sealed class {name}RouteRelic", 1)[1].split("[RegisterRelic", 1)[0]


def plain(text):
    return re.sub(r"\[/?pink\]", "", text).replace("{energyPrefix:maidenEnergyIcons(1)}", "1费")


class ResourceRelicContracts(unittest.TestCase):
    def test_stage_descriptions_exact_design(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        for name, chinese in (("LUST", "色欲"), ("CHASTITY", "贞洁")):
            design = read("DesignDoc.md").split(f"#### {chinese}\n", 1)[1].split("####", 1)[0]
            prefix = f"MAIDEN_SUCCUBUS_RELIC_{name}_ROUTE_RELIC.descriptionStage"
            for stage, label in ((1, "残缺"), (2, "完整"), (4, "觉醒")):
                self.assertEqual(re.search(rf"^{label}：(.*)$", design, re.M).group(1), plain(loc[prefix + str(stage)]))
            self.assertEqual(loc[prefix + "3"], loc[prefix + "4"])

    def test_lust_only_real_own_gain_and_per_combat_receipt(self):
        code = relic("Lust")
        for text in ("context.Reason != SecondaryResourceChangeReason.Gain", "context.Delta <= 0",
                     "context.Player != Owner", "context.Definition.Id != DesireResource.Id", "CombatManager.Instance.IsEnding",
                     "context.CombatState != Owner.Creature.CombatState", "[SavedProperty]", "BeforeCombatStart", "AfterCombatEnd"):
            self.assertIn(text, code)
        self.assertLess(code.index("_used = true"), code.index("await CardPileCmd.Draw"))

    def test_exact_draw_result_not_hand_difference(self):
        code = relic("Lust")
        self.assertIn("IEnumerable<CardModel> drawn = await CardPileCmd.Draw", code)
        self.assertIn("Stage == 1 ? 2 : 3", code)
        self.assertIn("GeneratedCardCostCmd.SetFreeUntilPlayed(card)", code)
        self.assertNotIn("HashSet<CardModel>", code)
        self.assertNotIn("SetThisTurnOrUntilPlayed", code)

    def test_fixed_cost_duration_and_unresolved_x_not_guessed(self):
        code = read("src/Commands/GeneratedCardCostCmd.cs").split("public static void SetFreeUntilPlayed", 1)[1].split("public static void SetFreeThisTurn", 1)[0]
        for text in ("EnergyCost.SetUntilPlayed(0)", "SetStarCostUntilPlayed(0)", "SecondaryResourceCost.Free",
                     "SecondaryResourceCostDuration.UntilPlayed", "!card.EnergyCost.CostsX", "!card.HasStarCostX", "!costs.Get(resourceId).CostsX", "await Q17"):
            self.assertIn(text, code)
        self.assertNotIn("SecondaryResourceCostDuration.ThisTurn", code)

    def test_chastity_counts_and_own_live_turn(self):
        code = relic("Chastity")
        for text in ("Math.Min(Stage, 2)", "player == Owner && Stage >= 3", "Data.Desire.Get(Owner) <= 2",
                     "CombatManager.Instance.IsInProgress", "PlayerCmd.GainEnergy(1, Owner)", "HoverTipFactory.FromPower<PreventNextDesireGainPower>()"):
            self.assertIn(text, code)
        power = read("src/Powers/HolyCardPowers.cs").split("public sealed class PreventNextDesireGainPower", 1)[1].split("[RegisterPower", 1)[0]
        for text in ("PowerStackType.Counter", "Amount <= 0 || amount <= 0", "context.Player.Creature != Owner",
                     "if (Amount > 1)", "Owner, -1, Owner, null", "PowerCmd.Remove(this)"):
            self.assertIn(text, power)

    def test_power_formal_name_and_per_layer_description(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/powers.json"))
        prefix = "MAIDEN_SUCCUBUS_POWER_PREVENT_NEXT_DESIRE_GAIN_POWER."
        self.assertEqual("贞洁", loc[prefix + "title"])
        self.assertIn("失去1层贞洁", loc[prefix + "description"])
        self.assertNotIn("移除此效果", loc[prefix + "description"])
        self.assertIn("{Amount}", loc[prefix + "smartDescription"])

    def test_real_commands_cover_trigger_and_cost_boundaries(self):
        code = read("src/ConsoleCommands/DesignDesireRelicTestConsoleCmd.cs")
        for text in ("Data.Desire.Modify", "Data.Desire.Set", "ctx.Play", "card.EndOfTurnCleanup()", "card.GetStarCostWithModifiers()",
                     "SavedProperties.From(lust)!.Fill(saved)", "actual gain draws two three three by stage", "undrawn cards not discounted",
                     "actual play spends neither fixed resource", "second play pays original costs", "full hand consumes first gain without extra cards",
                     "NoDrawPower", "empty piles safe and consume first trigger", "prevented increase does not trigger lust",
                     "exactly one protection layer consumed", "foreign turn never grants energy"):
            self.assertIn(text, code)

    def test_debug_only_disposable_combat_guard(self):
        code = read("src/ConsoleCommands/DesignDesireRelicTestConsoleCmd.cs")
        for text in ("#if DEBUG", 'args[0] != "confirm"', "player.RunState.Players.Count != 1",
                     "!CombatManager.Instance.IsInProgress", "_running", "TestMode.IsOn = previousTestMode"):
            self.assertIn(text, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
