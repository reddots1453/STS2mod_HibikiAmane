"""Prayer/barrier registration, exact text and lifecycle wiring contracts.

The production pure rules run in DesignSyncContracts; actual game commands are
covered by ms_test_magic_relics confirm, which must be run in a disposable game.
"""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


class MagicRelicContracts(unittest.TestCase):
    def test_approved_requirements_and_exact_descriptions(self):
        design = read("DesignDoc.md")
        for heading in ("祈祷耳环 `[RELIC-CHAR-001 · READY]`", "结界生成装置 `[RELIC-CHAR-004 · READY]`"):
            self.assertIn(heading, design)
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        for suffix, expected in (
            ("PRAYER_EARRINGS.description", "战斗开始时，获得1层魔力增幅。堕落值≤-2：变奏。"),
            ("PRAYER_EARRINGS.descriptionHoly", "战斗开始时和进入变身时，获得1层魔力增幅。"),
            ("BARRIER_GENERATOR.description", "每5个回合，获得1层圣域。"),
        ):
            self.assertIn(expected, design)
            actual = loc["MAIDEN_SUCCUBUS_RELIC_" + suffix]
            self.assertEqual(re.sub(r"\[/?(?:gold|purple)\]", "", actual), expected)
            self.assertIn("[gold]", actual)

    def test_character_registration_rarity_and_hover_tips(self):
        source = read("src/Relics/MagicSupportRelics.cs")
        contract = json.loads(read("docs/content_contract_20260824.json"))
        for name in ("PrayerEarrings", "BarrierGenerator"):
            self.assertEqual(contract["relics"].count(name), 1)
            self.assertIn(f"[RegisterRelic(typeof(MSRelicPool))]\npublic sealed class {name}", source)
        self.assertEqual(len(contract["relics"]), 32)
        self.assertIn("RelicRarity.Common", source)
        self.assertIn("RelicRarity.Rare", source)
        self.assertIn("HoverTipFactory.FromPower<MagicAmplificationPower>()", source)
        self.assertIn("HoverTipFactory.FromPower<SanctuaryPower>()", source)

    def test_prayer_successful_application_not_ui_event(self):
        source = read("src/Relics/MagicSupportRelics.cs")
        for token in ("AfterPowerAmountChanged", "ReferenceEquals(power.Owner, Owner.Creature)",
                      "ImmaculateRobePower or CorruptRobePower or EternalRobePower", "BeforeCombatStart()",
                      "power.Amount, amount", "await PowerCmd.Apply<MagicAmplificationPower>"):
            self.assertIn(token, source)
        self.assertNotIn("TransformationEvents", source)
        self.assertNotIn("Task.Run", source)
        self.assertNotIn("_ =", source)

    def test_variation_uses_current_run_and_safe_description_patch(self):
        source = read("src/Relics/MagicSupportRelics.cs")
        self.assertIn("MagicSupportRelicRules.PrayerVariation(CorruptionQuery.Get(run))", source)
        patch = read("src/Patches/TwinSoulChaliceDescriptionPatch.cs")
        self.assertIn("Safe.Run(", patch)
        self.assertIn("PrayerEarrings { HolyVariation: true }", patch)
        self.assertIn('earrings.Id.Entry + ".descriptionHoly"', patch)

    def test_barrier_late_phase_and_owner_participation(self):
        source = read("src/Relics/MagicSupportRelics.cs").split("public sealed class BarrierGenerator", 1)[1]
        self.assertIn("AfterSideTurnStartLate", source)
        for guard in ("Owner.Character is not MaidenSuccubusCharacter", "HasBeenRemovedFromState",
                      "Owner.Creature.IsDead", "ReferenceEquals(Owner.Creature.CombatState, combatState)",
                      "side != Owner.Creature.Side", "!participants.Contains(Owner.Creature)"):
            self.assertIn(guard, source)
        self.assertNotIn("AfterPlayerTurnStart", source)
        self.assertIn("if (TurnsSeen != 0) return", source)
        self.assertIn("PowerCmd.Apply<SanctuaryPower>", source)

    def test_counter_is_saved_visible_and_not_combat_reset(self):
        source = read("src/Relics/MagicSupportRelics.cs").split("public sealed class BarrierGenerator", 1)[1]
        for token in ("[SavedProperty]", "public int TurnsSeen", "ShowCounter => true", "DisplayAmount => TurnsSeen",
                      "InvokeDisplayAmountChanged()", "RelicStatus.Active", "Math.Clamp(value, 0, 4)"):
            self.assertIn(token, source)
        end = source.split("public override Task AfterCombatEnd", 1)[1]
        self.assertNotIn("TurnsSeen =", end)
        self.assertNotIn("BeforeCombatStart", source)

    def test_production_rules_compiled_and_not_test_duplicates(self):
        self.assertIn("../../src/Core/Relics/MagicSupportRelicRules.cs",
                      read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        suite = read("tests/DesignSyncContracts/Program.cs")
        for token in ("MagicSupportRelicRules.GrantsOnForm", "MagicSupportRelicRules.NextBarrierTurn",
                      "int[] counterSequence = [1, 2, 3, 4, 0]", "(9, 9, true)", "(18, 9, false)"):
            self.assertIn(token, suite)

    def test_real_game_script_exercises_hooks_commands_and_save(self):
        source = read("src/ConsoleCommands/DesignMagicRelicTestConsoleCmd.cs")
        for token in ('args[0] != "confirm"', "player.RunState.Players.Count != 1", "CombatManager.Instance.IsEnding",
                      "TransformationCmd.EnterImmaculateRobe", "TransformationCmd.EnterCorruptRobe",
                      "TransformationCmd.EnterEternalRobe", "TransformationCmd.GainArmor", "TransformationCmd.Exit",
                      "Hook.AfterSideTurnStart", "barrier.AfterCombatEnd", "RelicModel.FromSerializable",
                      "Sanctuary() == (expected == 0 ? 1 : 0)", "foreignEarrings", "foreignBarrier",
                      "TestMode.IsOn = previousTestMode", "finally"):
            self.assertIn(token, source)


if __name__ == "__main__":
    unittest.main()
