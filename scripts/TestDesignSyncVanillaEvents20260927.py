"""DS27-06C static contracts; engine tests are ms_test_vanilla_events confirm."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


class VanillaEventContracts(unittest.TestCase):
    def test_exact_fifteen_option_mapping(self):
        expected = {
            "ABYSSAL_BATHS": {"IMMERSE": 1, "ABSTAIN": -1},
            "AROMA_OF_CHAOS": {"LET_GO": 1, "MAINTAIN_CONTROL": -1},
            "DOORS_OF_LIGHT_AND_DARK": {"DARK": 1, "LIGHT": -1},
            "FIELD_OF_MAN_SIZED_HOLES": {"ENTER_YOUR_HOLE": 1, "RESIST": -1},
            "SPIRIT_GRAFTER": {"LET_IT_IN": 1, "REJECTION": -1},
            "SYMBIOTE": {"APPROACH": 1, "KILL_WITH_FIRE": -1},
            "WATERLOGGED_SCRIPTORIUM": {"TENTACLE_QUILL": 1},
            "WELLSPRING": {"BATHE": -1}, "WHISPERING_HOLLOW": {"HUG": 1},
        }
        flattened = {f"{event}.pages.INITIAL.options.{key}": value
                     for event, options in expected.items() for key, value in options.items()}
        actual = {key: int(value) for key, value in re.findall(
            r'\["([A-Z_]+\.pages\.INITIAL\.options\.[A-Z_]+)"\] = (-?1)',
            read("src/Patches/MvpEventPatches.cs"))}
        self.assertEqual(flattened, actual)

    def test_native_option_owner_not_any_player_in_run(self):
        code = read("src/Patches/MvpEventPatches.cs")
        for part in ("ConditionalWeakTable<EventOption, EventModel>", "GetConstructors()",
                     "Owners.GetValue(__instance, _ => __0)", "EventOptionOwnerPatch.OwnerOf(option)",
                     "model.Owner?.Character is MaidenSuccubusCharacter", "model.Owner!.RunState is RunState"):
            self.assertIn(part, code)
        self.assertNotIn("RunManager.Instance", code)
        self.assertNotIn("Players.Any", code)

    def test_completion_after_await_and_only_bath_accepts_intermediate_page(self):
        code = read("src/Patches/MvpEventPatches.cs")
        self.assertLess(code.index("await original();"), code.index("Completed.TryCommit"))
        self.assertIn('model.IsFinished, optionKey == "ABYSSAL_BATHS.pages.INITIAL.options.IMMERSE"', code)
        self.assertIn("!ReferenceEquals(previousPage, model.Description)", code)
        self.assertIn("bool wasFinished = model.IsFinished", code)
        self.assertIn("ConditionalWeakTable<TEvent, HashSet<string>>", read("src/Core/Events/EventCompletionLedger.cs"))

    def test_symbiose_one_legal_card_unlocks_zero_has_separate_reason(self):
        code = read("src/Patches/MvpEventPatches.cs")
        self.assertIn("Deck.Cards.Any(card =>", code)
        self.assertIn("LayeredEnchantments.HasOpenSlot(card)", code)
        self.assertIn("ModelDb.Enchantment<Corrupted>().CanEnchant(card)", code)
        self.assertIn('c < 3 ? symbiose + "_LOCKED" : canEnchant ? symbiose : symbiose + "_NO_CARDS"', code)
        self.assertNotIn("canEnchantTwo", code)
        self.assertIn("EnchantmentVfxCmd.Preview(card)", code)

    def test_reflection_uses_event_rng_and_upgrades_at_most_four(self):
        code = read("src/Patches/MvpEventPatches.cs")
        self.assertIn("Where(c => c.IsUpgradable)", code)
        self.assertIn("cards.StableShuffle(model.Rng)", code)
        self.assertIn("cards.Take(4)", code)
        self.assertNotIn("Rng.Niche", code)

    def test_exact_changed_player_text(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/events.json"))
        expected = {
            "LUMINOUS_CHOIR.MS_CALM": "失去所有[pink]欲望[/pink]值。将1张[gold]心神宁静[/gold]加入牌组。",
            "LUMINOUS_CHOIR.MS_SEIZE": "移除2张牌，不获得孢子心智。",
            "REFLECTIONS.MS_ACCEPT": "随机升级至多4张可升级的牌。",
            "REFLECTIONS.MS_SEIZE": "选择牌组中的1张牌，将它的2张复制加入牌组。",
            "SUNKEN_TREASURY.MS_SEE_THROUGH": "获得第二个箱子的金币。获得1个随机遗物。",
            "SUNKEN_TREASURY.MS_TAKE_ALL": "获得两个箱子的所有金币。获得1张[gold]贪婪[/gold]。",
            "SYMBIOTE.MS_SYMBIOSE": "选择2张可以被[gold]腐化[/gold]附魔的牌，为它们附魔：[gold]腐化[/gold]。",
            "SYMBIOTE.MS_SYMBIOSE_LOCKED": "需要+3或更高堕落值。",
            "SYMBIOTE.MS_SYMBIOSE_NO_CARDS": "牌组中没有可以被腐化附魔的牌。",
        }
        for short, text in expected.items():
            event, option = short.split(".")
            self.assertEqual(text, loc[f"{event}.pages.INITIAL.options.{option}.description"])
        design = read("DesignDoc.md")
        self.assertIn("同一事件中的同名后续按钮不重复触发堕落值变化", design)
        self.assertIn("牌组中没有可附魔牌时，该选项锁定", design)

    def test_production_ledger_is_executed_not_copied(self):
        self.assertIn("../../src/Core/Events/EventCompletionLedger.cs", read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        code = read("tests/DesignSyncContracts/Program.cs")
        for part in ("new EventCompletionLedger<Tuple<int>>()", "event completion {applicable}",
                     "no-op does not consume receipt", "rebuilt same-key option cannot duplicate",
                     "equal-valued independent event can commit"):
            self.assertIn(part, code)

    def test_opt_in_engine_tests_cover_ten_actual_effects_and_completion_probes(self):
        code = read("src/ConsoleCommands/DesignVanillaEventTestConsoleCmd.cs")
        for part in ('"ms_test_vanilla_events"', 'args[0] != "confirm"', "IsNetworked => false",
                     "CombatManager.Instance.IsInProgress", "Players.Count != 1",
                     "value = -5; value <= 5", "count = 0; count <= 2", "await Option(model, suffix).Chosen()",
                     "CardSelectCmd.UseSelector", "existing one/two attacks enchanted",
                     "purify removes selected one", "choir removes two without adding curse",
                     "calm clears resource and adds card", "upgrade zero/up to four",
                     "copies preserve upgrade and enchantment", "second chest gold", "both chest gold",
                     "tree grants exactly 300 gold", "tree grants branch and decay",
                     "fault/cancel does not commit", "rebuilt unfinished same-key option cannot duplicate",
                     "other owner's option does not affect Maiden corruption", "TestMode.IsOn = previousTestMode"):
            self.assertIn(part, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
