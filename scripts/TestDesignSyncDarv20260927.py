"""DS27 Darv/Compass static integration and exact-design text tests.

Executable pure production rules: dotnet run --project tests/DesignSyncContracts.
Actual game event/reward commands: ms_test_darv confirm (disposable run only).
"""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


def plain(text):
    return re.sub(r"\[/?(?:gold|purple|blue)\]", "", text)


class DarvCompassContract(unittest.TestCase):
    def test_approved_maturity_and_exact_relic_rule(self):
        design = read("DesignDoc.md")
        self.assertIn("灵魂罗盘 `[RELIC-EVENT-005 · READY]`", design)
        self.assertIn("达弗的援助 `[EVENT-NEW-002 · READY]`", design)
        expected = "-3＜堕落值＜3时，从卡牌奖励中获得圣洁和堕落路线卡牌的概率各提高20个百分点，同时中立路线概率降低40个百分点。拾起时，获得3次卡牌奖励。"
        self.assertIn(expected, design)
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        self.assertEqual(plain(loc["MAIDEN_SUCCUBUS_RELIC_SOUL_COMPASS.description"]), expected)

    def test_every_event_text_matches_design_including_punctuation(self):
        design = read("DesignDoc.md").split("#### 达弗的援助", 1)[1].split("#### 亡灵集会", 1)[0]
        loc = json.loads(read("MaidenSuccubus/localization/zhs/events.json"))
        prefix = "MAIDEN_SUCCUBUS_EVENT_DARV_ASSISTANCE."
        entries = {key[len(prefix):]: value for key, value in loc.items() if key.startswith(prefix)}
        self.assertEqual(len(entries), 17)
        for key, text in entries.items():
            with self.subTest(key=key):
                for paragraph in text.split("\n\n"):
                    self.assertIn(plain(paragraph), plain(design))
        for key, title in [("DARK", "黑暗的洞见"), ("HOLY", "光明的洞见"), ("BALANCE", "平衡的洞见")]:
            self.assertEqual(entries[f"pages.INITIAL.options.{key}.title"], title)

    def test_event_scope_and_all_locks(self):
        event = read("src/Events/DarvAssistance.cs")
        for fragment in ["[RegisterSharedEvent]", "runState.CurrentActIndex == 2",
                         "runState.Players.All(player => player.Character is MaidenSuccubusCharacter)",
                         "Corruption >= 3 ? DarkInsight : null", "Corruption <= -3 ? HolyInsight : null",
                         "Corruption is > -3 and < 3 ? BalancedInsight : null"]:
            self.assertIn(fragment, event)
        self.assertEqual(event.count("RelicCmd.Obtain<SoulCompass>"), 1)
        balance = event.split("private async Task BalancedInsight()", 1)[1]
        self.assertNotIn("RewardsCmd", balance)
        self.assertIn('PageDescription("BALANCE")', balance)

    def test_fixed_rare_reward_uses_vanilla_event_selection(self):
        event = read("src/Events/DarvAssistance.cs")
        for term in ["ForNonCombatWithUniformOdds", "card.Rarity == CardRarity.Rare", "WithRngOverride(rng)",
                     "CardCreationFlags.NoCardPoolModifications",
                     "MSHolyCardPool", "MSCorruptCardPool", "new CardReward(options, 3, player)",
                     "await RewardsCmd.OfferCustom", "SetEventFinished(PageDescription(page))"]:
            self.assertIn(term, event)
        self.assertNotIn("PileType.Deck", event)  # Real reward selection owns deck addition.

    def test_compass_owner_band_and_once_only_pickup(self):
        relic = read("src/Relics/SoulCompass.cs")
        for term in ["RelicRarity.Event", "IMSRouteRewardModifierRelic", "ReferenceEquals(player, Owner)",
                     "player.Character is MaidenSuccubusCharacter", "[SavedProperty] public bool PickupRewardsGranted",
                     "Enumerable.Range(0, 3)", "CardCreationOptions.ForRoom(player, RoomType.Monster)",
                     "RouteRewardProbabilityBonus.ForSoulCompass(corruption)"]:
            self.assertIn(term, relic)
        self.assertLess(relic.index("PickupRewardsGranted = true"), relic.index("await RewardsCmd.OfferCustom"))
        self.assertIn("if (PickupRewardsGranted || Owner.Character is not MaidenSuccubusCharacter) return", relic)
        modifier = read("src/Core/Routes/RouteRewardProbabilityModifier.cs")
        self.assertIn("player.Relics", modifier)
        self.assertIn("GetRouteRewardProbabilityBonus(player)", modifier)
        service = read("src/Core/Rewards/RouteCardRewardService.cs")
        self.assertIn("creationOptions.Source == CardCreationSource.Encounter", service)

    def test_production_probability_not_a_copied_test_formula(self):
        project = read("tests/DesignSyncContracts/DesignSyncContracts.csproj")
        for path in ["RouteRewardProbabilities.cs", "RouteRewardProbabilityBonus.cs"]:
            self.assertIn("../../src/Core/Routes/" + path, project)
        rule = read("src/Core/Routes/RouteRewardProbabilityBonus.cs")
        self.assertIn("corruption is > -3 and < 3", rule)
        self.assertIn("new(Holy: 0.20m, Corrupt: 0.20m)", rule)
        executable = read("tests/DesignSyncContracts/Program.cs")
        self.assertIn("corruption = -5; corruption <= 5", executable)
        self.assertIn("(.40m, .28m, .32m)", executable)
        self.assertIn("(.30m, .30m, .40m)", executable)
        self.assertIn("actual.Neutral - ordinary.Neutral", executable)

    def test_runtime_suite_uses_commands_and_restores_test_mode(self):
        test = read("src/ConsoleCommands/DesignDarvTestConsoleCmd.cs")
        for term in ['args[0] != "confirm"', "CombatManager.Instance.IsInProgress", "BeginEvent",
                     "option.Chosen()", "balanceOption.Chosen()", "RelicCmd.Obtain", "PrepareToSelectCardReward",
                     "first.Populate()", "offered == 3", "TestMode.IsOn = previousTestMode", "finally"]:
            self.assertIn(term, test)
        self.assertIn("event rng does not consume encounter reward rng", test)
        self.assertIn("skip finishes event without adding a card", test)

    def test_registration_and_legacy_identity_remain_separate(self):
        content = json.loads(read("docs/content_contract_20260824.json"))
        self.assertEqual(content["relics"].count("SoulCompass"), 1)
        self.assertIn("BalancedLens", content["relics"])
        self.assertEqual(len(content["relics"]), 28)
        self.assertIn('Compile Remove="tests/**/*.cs"', read("MaidenSuccubus.csproj"))


if __name__ == "__main__":
    unittest.main()
