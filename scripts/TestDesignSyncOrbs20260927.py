"""DS27-04B integration contracts; runtime command: ms_test_orbs confirm.

These source/text assertions do not claim game execution. Pure rules are also
executed by tests/DesignSyncContracts, linking the actual production C# file.
"""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


class OrbContract(unittest.TestCase):
    def test_exact_current_design_text_and_punctuation(self):
        design = read("DesignDoc.md")
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        for stem, suffix in [("TWIN_SOUL_CHALICE", "description"),
                             ("TWIN_SOUL_CHALICE", "descriptionCorrupt"),
                             ("TWIN_SOUL_CHALICE", "descriptionHoly"),
                             ("SKY_ORB", "description")]:
            text = re.sub(r"\[/?(?:gold|purple|green)\]", "", loc[f"MAIDEN_SUCCUBUS_RELIC_{stem}.{suffix}"])
            text = text.replace("{Heal}", "5").replace("{MaxHp}", "5")
            self.assertIn(text, design)
            self.assertNotIn("升变", text)
        self.assertEqual(loc["MAIDEN_SUCCUBUS_RELIC_TWIN_SOUL_CHALICE.title"], "全能宝珠")
        self.assertEqual(loc["MAIDEN_SUCCUBUS_RELIC_SKY_ORB.title"], "天穹宝珠")
        self.assertIn("RELIC-START-003 · READY", design)
        self.assertIn("RELIC-START-005 · READY", design)

    def test_same_reward_native_selection_not_extra_rewards(self):
        for file in ["TwinSoulChalice", "SkyOrb"]:
            code = read(f"src/Relics/{file}.cs")
            self.assertIn("ShouldAllowSelectingMoreCardRewards", code)
            self.assertIn("ReferenceEquals(player, Owner)", code)
            self.assertIn("ReferenceEquals(cardReward.Player, player)", code)
            self.assertIn("player.Character is MaidenSuccubusCharacter", code)
            self.assertIn("cardReward.Cards.Count()", code)
            self.assertNotIn("TryModifyRewards", code)
            self.assertNotIn("new CardReward", code)
        rules = read("src/Core/Relics/OrbRules.cs")
        self.assertIn("offeredCount > 1", rules)
        self.assertIn("sky || corruption <= -4", rules)

    def test_hp_and_ancient_mapping(self):
        starter = read("src/Relics/TwinSoulChalice.cs")
        sky = read("src/Relics/SkyOrb.cs")
        self.assertIn("[RegisterTouchOfOrobasRefinement(typeof(SkyOrb))]", starter)
        self.assertIn("RelicRarity.Starter", starter)
        self.assertIn("RelicRarity.Ancient", sky)
        for code in [starter, sky]:
            self.assertIn("Owner.Character is not MaidenSuccubusCharacter || Owner.Creature.IsDead", code)
            self.assertIn("new MaxHpVar(5)", code)
            self.assertIn("await CreatureCmd.GainMaxHp", code)
        self.assertIn("new HealVar(5)", starter)
        self.assertIn("await CreatureCmd.Heal", starter)
        self.assertNotIn("CreatureCmd.Heal", sky)
        self.assertIn("sky || corruption >= 4", read("src/Core/Relics/OrbRules.cs"))
        contract = json.loads(read("docs/content_contract_20260824.json"))
        self.assertIn("SkyOrb", contract["relics"])
        self.assertIn("TwinSoulChalice", contract["relics"])

    def test_dynamic_description_preserved(self):
        patch = read("src/Patches/TwinSoulChaliceDescriptionPatch.cs")
        for term in ["Safe.Run", "IsMutable", "corruption >= 4", "corruption <= -4",
                     '".descriptionCorrupt"', '".descriptionHoly"']:
            self.assertIn(term, patch)

    def test_trial_listener_independent_of_relic_inventory(self):
        lifecycle = read("src/Acts/FourthRouteLifecycle.cs")
        bootstrap = read("src/MaidenSuccubusMod.cs")
        self.assertIn("SubscribeForRunStateHooks(ModId + \".FourthRoute\", Acts.FourthRouteLifecycle.Listeners)", bootstrap)
        self.assertEqual(bootstrap.count("Acts.FourthRouteLifecycle.Listeners"), 1)
        self.assertIn("player.IsActiveForHooks && IsEligible(player)", lifecycle)
        self.assertIn("player.Character is MaidenSuccubusCharacter", lifecycle)
        self.assertIn("ConditionalWeakTable<Player, FourthRouteLifecycle>", lifecycle)
        self.assertIn("[RegisterSingleton]", lifecycle)
        self.assertIn("public FourthRouteLifecycle()", lifecycle)
        self.assertIn("ModelDb.Singleton<FourthRouteLifecycle>().MutableClone()", lifecycle)
        self.assertIn("ShouldReceiveCombatHooks => false", lifecycle)
        self.assertNotIn("GetRelic", lifecycle)
        self.assertNotIn("FourthRoute", read("src/Relics/TwinSoulChalice.cs"))
        for hook in ["BeforeCombatStart", "AfterRoomEntered", "AfterCombatVictory", "AfterPotionUsed",
                     "AfterRestSiteHeal", "AfterRestSiteSmith", "AfterCardChangedPiles", "ModifyMerchantPrice"]:
            self.assertIn(hook, lifecycle)
        self.assertIn("potion.Owner == _owner", lifecycle)

    def test_map_uses_local_character_and_retains_transition_safety(self):
        code = read("src/Patches/FourthRouteQuestSelectionPatch.cs")
        self.assertNotIn("GetRelic", code)
        for term in ["FourthRouteLifecycle.IsEligible(candidate)", "LocalContext.IsMe(candidate)",
                     "FourthRouteLifecycle.For(player)", "map.SetTravelEnabled(false)",
                     "finally", "currentMap.SetTravelEnabled(true)", "RunManager.Instance.DebugOnlyGetState() != runState"]:
            self.assertIn(term, code)

    def test_executable_rules_link_production(self):
        self.assertIn('../../src/Core/Relics/OrbRules.cs', read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        test = read("tests/DesignSyncContracts/Program.cs")
        for term in ["OrbRules.GrantsMaxHp", "OrbRules.AllowsMoreCards", "0, 1, 2, 3, 5", "corruption <= 5"]:
            self.assertIn(term, test)

    def test_runtime_test_uses_actual_rewards_refinement_and_dispatch(self):
        test = read("src/ConsoleCommands/DesignOrbTestConsoleCmd.cs")
        for term in ["TestMode.IsOn = previousTestMode", "CardSelectCmd.UseSelector", "RewardsCmd.OfferCustom",
                     "new[] { 0, 1, 2, 3 }", "cards.Count > 0", "player.Deck.Cards.Except(before)",
                     "touch.SetupForPlayer(player)", "RelicCmd.Obtain(touch, player)",
                     "Hook.AfterRestSiteSmith(run, player)", "run.IterateHookListeners(null)",
                     "CreatureCmd.SetCurrentHp", "orb.AfterCombatVictory", "args[0] != \"confirm\""]:
            self.assertIn(term, test)


if __name__ == "__main__":
    unittest.main()
