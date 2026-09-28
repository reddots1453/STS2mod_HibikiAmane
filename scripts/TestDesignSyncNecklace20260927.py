"""DS27-04D wiring/text checks; not a substitute for in-game acceptance.

Production rule execution: dotnet run --project tests/DesignSyncContracts.
Real model/resource/save tests: ms_test_necklace confirm (disposable combat).
"""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


class NecklaceContract(unittest.TestCase):
    def test_approved_requirement_and_exact_text(self):
        design = read("DesignDoc.md")
        self.assertIn("清心项链／浊心项链 `[RELIC-CHAR-005 · READY]`", design)
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        for branch, expected in [
            ("CLEAR", "回合开始时，如果≥5点欲望，失去1点欲望值。堕落值≥3：变奏。"),
            ("MURKY", "回合开始时，如果≤5点欲望，获得1点欲望值。堕落值＜3：变奏。"),
        ]:
            self.assertIn(expected, design)
            actual = loc[f"MAIDEN_SUCCUBUS_RELIC_{branch}_HEART_NECKLACE.description"]
            self.assertEqual(re.sub(r"\[/?(?:pink|purple|gold)\]", "", actual), expected)
            for tag in ("[pink]欲望[/pink]", "[pink]欲望值[/pink]", "[purple]堕落值[/purple]", "[gold]变奏[/gold]"):
                self.assertIn(tag, actual)

    def test_both_save_ids_retained_but_one_rollable_candidate(self):
        source = read("src/Relics/Iteration2Relics.cs")
        for name in ("ClearHeartNecklace", "MurkyHeartNecklace"):
            self.assertIn(f"[RegisterRelic(typeof(MSRelicPool))]\npublic sealed class {name} : HeartNecklace", source)
        self.assertIn("RelicRarity.Rare", source)
        self.assertNotIn("RelicRarity.Shop", source)
        self.assertNotIn("runState.Players.Any", source)  # Do not ban other multiplayer owners' bags.
        legacy = source.split("public sealed class MurkyHeartNecklace", 1)[1]
        self.assertIn("IsAllowed(IRunState runState) => false", legacy)
        self.assertIn("IsAllowedInShops => false", legacy)

    def test_variation_does_not_reacquire_or_subscribe(self):
        source = read("src/Relics/Iteration2Relics.cs")
        for old in ("CorruptionEvents", "RelicCmd.Replace", "SynchronizeVariation", "AfterObtained", "_ ="):
            self.assertNotIn(old, source)
        self.assertIn("HeartNecklaceRules.IsMurky(CorruptionQuery.Get(run))", source)
        self.assertIn("IsMutable && Owner is", source)
        self.assertIn("this is MurkyHeartNecklace", source)

    def test_title_and_description_share_branch_and_keep_wax(self):
        source = read("src/Relics/Iteration2Relics.cs")
        self.assertIn('ActiveEntry + ".title"', source)
        self.assertIn("ToyBox.WaxRelicPrefix", source)
        self.assertIn('wax.Add("Title", title)', source)
        patch = read("src/Patches/TwinSoulChaliceDescriptionPatch.cs")
        self.assertIn("Safe.Run(", patch)
        self.assertIn("__instance is HeartNecklace necklace", patch)
        self.assertIn('necklace.ActiveEntry + ".description"', patch)

    def test_real_resource_pipeline_and_owner_isolation(self):
        source = read("src/Relics/Iteration2Relics.cs")
        for guard in ("!ReferenceEquals(player, Owner)", "Owner.Character is not MaidenSuccubusCharacter",
                      "HasBeenRemovedFromState", "Owner.Creature.IsDead"):
            self.assertIn(guard, source)
        self.assertIn("await Data.Desire.Modify(Owner, delta)", source)
        self.assertNotIn("Data.Desire.Set", source)

    def test_actual_production_rule_compiled_against_independent_table(self):
        project = read("tests/DesignSyncContracts/DesignSyncContracts.csproj")
        self.assertIn("../../src/Core/Relics/HeartNecklaceRules.cs", project)
        suite = read("tests/DesignSyncContracts/Program.cs")
        self.assertIn("int[] clearDeltas = [0, 0, 0, 0, 0, -1, -1, -1, -1, -1, -1]", suite)
        self.assertIn("int[] murkyDeltas = [1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0]", suite)
        self.assertIn("HeartNecklaceRules.TurnDelta(corruption, desire)", suite)

    def test_runtime_suite_uses_commands_hooks_and_native_save(self):
        suite = read("src/ConsoleCommands/DesignNecklaceTestConsoleCmd.cs")
        for term in ("RelicCmd.Obtain(necklace, player)", "Hook.AfterPlayerTurnStart", "Data.Desire.Set(player, 5)",
                     "RelicModel.FromSerializable(necklace.ToSerializable())", "restored.Owner = player",
                     "PowerCmd.Apply<PreventNextDesireGainPower>", "floor", "otherMaiden", "foreignCopy",
                     "ReferenceEquals(player.Relics.Single(), necklace)", "RelicCmd.Remove(necklace)"):
            self.assertIn(term, suite)

    def test_test_entry_requires_explicit_disposable_confirmation(self):
        suite = read("src/ConsoleCommands/DesignNecklaceTestConsoleCmd.cs")
        for term in ('args[0] != "confirm"', "player.RunState.Players.Count != 1", "#if DEBUG",
                     "CombatManager.Instance.IsEnding", "finally", "TestMode.IsOn = previousTestMode"):
            self.assertIn(term, suite)


if __name__ == "__main__":
    unittest.main()
