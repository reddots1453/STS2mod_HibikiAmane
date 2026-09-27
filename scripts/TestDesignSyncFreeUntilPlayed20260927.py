"""Structural guards, not substitutes for the compiled in-game payment tests."""
import unittest
from TestDesignSyncNeutral20260927 import read


class FreeUntilPlayedContracts(unittest.TestCase):
    def test_registered_instance_capability_not_global_card_id(self):
        code = read("src/Core/Cards/FreeUntilPlayedCapability.cs")
        for value in ('StableEntryStem = "free_until_played"', "CardCapability", "ModelCapabilities.TryGet(card",
                      "card.IsMutable", "card.CombatState != null", "card.Pile?.Type != PileType.Deck"):
            self.assertIn(value, code)
        self.assertNotIn("Dictionary", code)

    def test_attach_idempotent_without_replacing_other_capabilities(self):
        code = read("src/Commands/GeneratedCardCostCmd.cs")
        for value in ("set.Get<FreeUntilPlayedCapability>() == null", "ModelCapabilityRegistry.Create<FreeUntilPlayedCapability>()",
                      "allowMerge: false"):
            self.assertIn(value, code)

    def test_final_query_patches_preserve_x_negative_and_nonlocal_queries(self):
        code = read("src/Patches/FreeUntilPlayedPatches.cs")
        for value in ("nameof(CardEnergyCost.GetWithModifiers)", "nameof(CardModel.GetStarCostWithModifiers)",
                      "modifiers.HasFlag(CostModifiers.Local)", "!__instance.CostsX", "!__instance.HasStarCostX",
                      "if (free && __result >= 0)", "CardModel ____card"):
            self.assertIn(value, code)
        self.assertGreaterEqual(code.count("HarmonyPriority(Priority.Last)"), 3)
        self.assertEqual(code.count("Safe.Run("), 4)

    def test_secondary_required_only_and_clear_replacement_commit(self):
        code = read("src/Patches/FreeUntilPlayedPatches.cs")
        for value in ('typeof(SecondaryResourcePaymentResolver), "ResolveLine"', "!result.CostsX",
                      "result.Kind == SecondaryResourceUseKind.RequiredCost", "AmountToSpend = 0",
                      "OriginalShortfall = 0, CoveredShortfall = 0, Shortfall = 0",
                      "ShortfallResolution = SecondaryResourceShortfallResolution.None"):
            self.assertIn(value, code)
        self.assertNotIn("typeof(SecondaryResourceHook)", code)

    def test_cleanup_at_native_play_boundary_not_after_first_replay(self):
        code = read("src/Patches/FreeUntilPlayedPatches.cs")
        self.assertIn("nameof(CardEnergyCost.AfterCardPlayedCleanup)", code)
        self.assertIn("RemoveCapability<FreeUntilPlayedCapability>() != null", code)
        self.assertIn("__result |= removed", code)
        capability = read("src/Core/Cards/FreeUntilPlayedCapability.cs")
        self.assertIn("BeforeCombatStart()", capability)
        self.assertIn("AfterCombatEnd(CombatRoom room)", capability)
        self.assertNotIn("AfterCardPlayed", capability)
        self.assertNotIn("AfterTurnEnd", capability)

    def test_payment_test_uses_paid_wrapper_not_autoplay(self):
        code = read("src/ConsoleCommands/DesignDesireRelicTestConsoleCmd.cs")
        for value in ("await card.SpendResources()", "card.OnPlayWrapper", "isAutoPlay: false",
                      "await DesignSyncFreeUntilPlayedContract.Run(ctx, Check)"):
            self.assertIn(value, code)
        self.assertNotIn("ctx.Play(", code)

    def test_dynamic_clone_serialization_and_scope_test_coverage(self):
        code = read("src/Debugging/CardEffects/DesignSyncFreeUntilPlayedContract.cs")
        for value in ("ctx.Add<FocusedSlash>", "ctx.Combat.CloneCard(card)", "CardModel.FromSerializable(card.ToSerializable())",
                      "clone cleanup cannot consume original entitlement", "consumed entitlement not resurrected by save",
                      "global-only query not changed", "unmarked card retains dynamic cost", "pending X semantics unchanged",
                      "pending Y semantics unchanged", "combat end removes entitlement"):
            self.assertIn(value, code)

    def test_test_only_providers_cleanup_and_no_false_handtest_claim(self):
        code = read("src/Debugging/CardEffects/DesignSyncFreeUntilPlayedContract.cs")
        self.assertTrue(code.startswith("#if DEBUG"))
        for value in ("harmony.UnpatchAll(harmony.Id)", "_probeCard = null", "actual paid-play spends no energy",
                      "DesirePaidWithHpPower", "optional and extra spending records unchanged",
                      "second actual paid-play pays dynamic energy", "natural combat end remains hand-test coverage"):
            self.assertIn(value, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
