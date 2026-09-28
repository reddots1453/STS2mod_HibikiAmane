"""DS27 hero/eternal and starter selector integration/text contracts.

Execution tests live in the production-rule C# host, ms_test_retention_orbs and
ms_test_orbs. These checks do not assert that a UI screenshot was validated.
"""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


class RetentionStarterContract(unittest.TestCase):
    def test_exact_description_all_forms_and_names(self):
        design = read("DesignDoc.md")
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        for name in ["HERO_ORB", "ETERNAL_ORB"]:
            for key, value in loc.items():
                if key.startswith(f"MAIDEN_SUCCUBUS_RELIC_{name}.description"):
                    self.assertIn(re.sub(r"\[/?(?:gold|purple)\]", "", value), design)
        self.assertEqual(loc["MAIDEN_SUCCUBUS_RELIC_HERO_ORB.title"], "英雄宝珠")
        self.assertEqual(loc["MAIDEN_SUCCUBUS_RELIC_ETERNAL_ORB.title"], "永恒宝珠")
        self.assertIn("RELIC-START-002 · READY", design)
        self.assertIn("RELIC-START-004 · READY", design)
        self.assertIn("英雄宝珠与全能宝珠之间", design)

    def test_lifecycle_owner_selection_and_distinct_retention(self):
        code = read("src/Relics/RetentionOrbs.cs")
        for term in ["BeforeFlushLate", "player != Owner", "player.Character is not MaidenSuccubusCharacter",
                     "CardSelectCmd.FromHand", "new CardSelectorPrefs(SelectionScreenPrompt, 1)",
                     "card.Pile?.Type != PileType.Hand", "CombatEnchantmentCmd.IsCombatClone(card)",
                     "CardCmd.ApplyKeyword(card, CardKeyword.Retain)", "card.GiveSingleTurnRetain()",
                     "rule.Upgrade && card.IsUpgradable", "CardCmd.Upgrade(card)"]:
            self.assertIn(term, code)
        self.assertNotIn("card.DeckVersion", code)

    def test_audited_vanilla_enchantment_and_first_phase(self):
        code = read("src/Relics/RetentionOrbs.cs")
        for term in ["rule.Enchant && card.Enchantment == null", "CombatEnchantmentCmd.ApplyVanilla<SlumberingEssence>",
                     "firstEnchantmentTick = enchantment.BeforeFlush(context, player)",
                     "HoverTipFactory.FromEnchantment<SlumberingEssence>()", "ControlQuery.SuppressPresentation()"]:
            self.assertIn(term, code)
        scope = code.split("using (ControlQuery.SuppressPresentation())", 1)[1].split("await firstEnchantmentTick", 1)[0]
        self.assertNotIn("await ", re.sub(r"//[^\n]*", "", scope))
        self.assertNotIn("AddUntilPlayed", code)  # Calls native behavior instead of copying it.
        self.assertIn("typeof(T) != typeof(SlumberingEssence)", read("src/Commands/CombatEnchantmentCmd.cs"))

    def test_ancient_mapping_and_formal_starter_candidates(self):
        code = read("src/Relics/RetentionOrbs.cs")
        self.assertIn("[RegisterTouchOfOrobasRefinement(typeof(EternalOrb))]", code)
        for term in ["RelicRarity.Starter", "RelicRarity.Ancient", "class HeroOrb", "class EternalOrb"]:
            self.assertIn(term, code)
        choices = read("src/Characters/Starts/MaidenSuccubusStartProfiles.cs")
        self.assertIn("[typeof(TwinSoulChalice), typeof(HeroOrb)]", choices)
        contract = json.loads(read("docs/content_contract_20260824.json"))
        self.assertEqual(len(contract["relics"]), 33)  # Includes the approved TonysCharm addition.
        self.assertIn("BalancedLens", contract["relics"])  # Legacy identity is not silently repurposed.

    def test_selection_is_player_lobby_data_not_process_global(self):
        code = read("src/Data/StarterRelicChoice.cs")
        self.assertIn("PlayerRunSavedData<StarterRelicChoiceState>", code)
        self.assertIn("Kind { get; set; } = StarterRelicKind.Omnipotent", code)
        bootstrap = read("src/MaidenSuccubusMod.cs").split("StarterRelicChoice.Handle =", 1)[1].split("M5Progress.Handle", 1)[0]
        self.assertIn("store.RegisterPerPlayer", bootstrap)
        self.assertIn("SyncLobbyOnChange = true", bootstrap)
        ui = read("src/UI/StarterRelicSelector.cs")
        self.assertIn("Handle.Lobby.Modify(lobby, lobby.LocalPlayer.id", ui)
        self.assertNotRegex(ui, r"static\s+StarterRelicKind\s+\w+\s*[=;]")

    def test_starter_initialization_exact_once_and_load_boundary(self):
        code = read("src/Characters/Starts/StarterRelicSelection.cs")
        for term in ["player.Character is not MaidenSuccubusCharacter", "if (state.Applied) return",
                     "player.GetRelic<TwinSoulChalice>()", "original != null", "data.Applied = true",
                     "FloorAddedToDeck = original.FloorAddedToDeck", "silent: true"]:
            self.assertIn(term, code)
        self.assertNotIn("RelicCmd.Obtain", code)
        patch = read("src/Patches/StarterRelicSelectionPatch.cs")
        self.assertIn("RunManager.FinalizeStartingRelics", patch)
        self.assertIn("[HarmonyPrefix]", patch)
        self.assertIn("Safe.Run", patch)
        self.assertNotIn("FromSerializable", patch)

    def test_ui_scopes_visibility_and_input(self):
        code = read("src/UI/StarterRelicSelector.cs")
        for term in ["ConditionalWeakTable<NCharacterSelectScreen, StarterRelicSelector>",
                     "!button.IsLocked && !button.IsRandom", "lobby.LocalPlayer.isReady",
                     "lobby.LocalPlayer.character is not MaidenSuccubusCharacter", "_row.IsVisibleInTree()",
                     "_previous.Pressed += ToggleSafely", "_next.Pressed += ToggleSafely",
                     "FocusNeighborRight", "FocusNeighborLeft", "_closed = true", "_row.Hide()",
                     "relic.DynamicDescription.GetFormattedText()"]:
            self.assertIn(term, code)
        self.assertNotIn("_Process", code)
        self.assertNotIn("NModalContainer", code)

    def test_ui_each_patch_has_one_target(self):
        code = read("src/Patches/StarterRelicSelectorUiPatch.cs")
        self.assertEqual(code.count("[HarmonyPatch(typeof(NCharacterSelectScreen)"), 6)
        self.assertNotRegex(code, r"\[HarmonyPatch\([^\n]+\n\s*\[HarmonyPatch\(")
        for term in ["SelectCharacter", "OnEmbarkPressed", "OnUnreadyPressed", "PlayerChanged", "BeginRun", "OnSubmenuClosed"]:
            self.assertIn(term, code)

    def test_production_rules_are_executed_with_independent_table(self):
        self.assertIn("../../src/Core/Relics/RetentionOrbRules.cs", read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        test = read("tests/DesignSyncContracts/Program.cs")
        for term in ["RetentionOrbRule[] retention", "RetentionOrbRule.At(corruption, false)", "RetentionOrbRule.At(corruption, true)"]:
            self.assertIn(term, test)

    def test_actual_game_test_entrypoints(self):
        combat = read("src/ConsoleCommands/DesignRetentionOrbTestConsoleCmd.cs")
        for term in ["Hook.BeforeFlush(ctx.Combat, ctx.Player)", "card.DeckVersion = deck", "card.EndOfTurnCleanup()",
                     "GetWithModifiers(CostModifiers.Local)", "await ctx.Play(card, ctx.PrimaryEnemy)",
                     "ApplyVanilla<Sharp>", "touch.SetupForPlayer", "TestMode.IsOn = previousTestMode"]:
            self.assertIn(term, combat)
        startup = read("src/ConsoleCommands/DesignOrbTestConsoleCmd.cs")
        for term in ["StarterRelicSelection.Apply(player)", "StarterRelicChoiceState { Kind = kind }",
                     "(StarterRelicKind)99", "ReferenceEquals(selectedInstance", "StarterRelicSelection.Apply(foreign)"]:
            self.assertIn(term, startup)


if __name__ == "__main__":
    unittest.main()
