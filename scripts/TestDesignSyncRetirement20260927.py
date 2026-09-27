"""DS27-02I acquisition gates and audit mutation tests; engine save tests are separate."""
import json
import unittest
import AuditCardLocalization as audit
from TestDesignSyncNeutral20260927 import read


class RetiredContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.cards, _ = audit.registered_cards()
        cls.lines = read("DesignDoc.md").splitlines()
        cls.loc = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))

    def validate(self, cards=None, lines=None, replace=None):
        def source(path):
            text = read(path)
            return replace(path, text) if replace else text
        return audit.retired_compatibility(cards if cards is not None else self.cards,
            lines if lines is not None else self.lines, self.loc, source)

    def test_explicit_retirement_complete_not_inferred_from_absence(self):
        retired, failures = self.validate()
        self.assertEqual(retired, {"MagicResonance", "SemenAppetite"})
        self.assertEqual(failures, [])

    def test_missing_gate_cannot_be_hidden_by_manifest(self):
        for name in ("MagicResonance", "SemenAppetite"):
            for gate in ("CanBeGeneratedInCombat => false", "CanBeGeneratedByModifiers => false",
                         "shouldShowInCardLibrary: false"):
                cards = dict(self.cards)
                block, path = cards[name]
                cards[name] = (block.replace(gate, "/* " + gate + " */"), path)
                retired, failures = self.validate(cards=cards)
                self.assertFalse(retired)
                self.assertTrue(any("missing acquisition gate" in f for f in failures))

    def test_removed_registration_is_not_compatibility(self):
        cards = dict(self.cards)
        del cards["MagicResonance"]
        retired, failures = self.validate(cards=cards)
        self.assertFalse(retired)
        self.assertTrue(any("no longer registered" in f for f in failures))

    def test_reintroduced_design_requires_reassessment(self):
        title = self.loc["MAIDEN_SUCCUBUS_CARD_MAGIC_RESONANCE.title"]
        retired, failures = self.validate(lines=self.lines + [title, "能力牌 罕见", "1费 测试。"])
        self.assertFalse(retired)
        self.assertTrue(any("contradicts" in f for f in failures))

    def test_policy_and_manifest_must_match(self):
        retired, failures = self.validate(replace=lambda p, s:
            s.replace("or SemenAppetite", "or DreamPigment") if p.endswith("RetiredCardCatalog.cs") else s)
        self.assertFalse(retired)
        self.assertTrue(any("identities differ" in f for f in failures))

    def test_native_pool_and_custom_candidate_paths_both_required(self):
        for target in ("src/Pools/MSNeutralCardPool.cs", "src/Pools/MSCorruptCardPool.cs",
                       "src/Core/Routes/AllMaidenSuccubusCards.cs"):
            retired, failures = self.validate(replace=lambda p, s:
                s.replace("RetiredCardCatalog", "OtherPolicy") if p == target else s)
            self.assertFalse(retired)
            self.assertTrue(any("filter" in f for f in failures))

    def test_native_library_flag_is_forwarded_not_just_declared(self):
        retired, failures = self.validate(replace=lambda p, s:
            s.replace("base(cost, type, rarity, target, shouldShowInCardLibrary)",
                      "base(cost, type, rarity, target, true)", 1) if p.endswith("MSCardBases.cs") else s)
        self.assertFalse(retired)
        self.assertTrue(any("visibility" in f for f in failures))

    def test_retired_enchantment_registered_but_rejects_new_application(self):
        manifest = json.loads(read("docs/content_contract_20260824.json"))
        self.assertEqual(manifest["retiredCompatibility"]["enchantments"], ["EnergyOverloadEnchantment"])
        code = read("src/Enchantments/MvpEnchantments.cs")
        self.assertIn("[RegisterEnchantment]\npublic sealed class EnergyOverloadEnchantment", code)
        block = code.split("class EnergyOverloadEnchantment", 1)[1].split("[RegisterEnchantment]", 1)[0]
        self.assertIn("public override bool CanEnchant(CardModel card) => false", block)

    def test_model_registration_and_legacy_payload_remain(self):
        manifest = json.loads(read("docs/content_contract_20260824.json"))
        for name, pool in (("MagicResonance", "MSNeutralCardPool"), ("SemenAppetite", "MSCorruptCardPool")):
            self.assertIn(name, manifest["cards"][pool])
            block, path = self.cards[name]
            self.assertIn(f"PowerVar<{name}Power>", block)
            self.assertIn("OnUpgrade", block)
            self.assertIn(f"[RegisterCard(typeof({pool}))]\npublic sealed class {name}", path.read_text(encoding="utf-8-sig"))
        for pool in ("MSNeutralCardPool", "MSCorruptCardPool"):
            code = read(f"src/Pools/{pool}.cs")
            self.assertNotIn("override IEnumerable<CardModel> AllCards", code)
            self.assertNotIn("GenerateAllCards", code)

    def test_runtime_contract_uses_native_paths_and_does_not_execute_legacy_effects(self):
        code = read("src/ConsoleCommands/DesignRetiredContentTestConsoleCmd.cs")
        for term in ("GetUnlockedCards", "GetDefaultTransformationOptions", "CardFactory.FilterForCombat",
                     "CardModel.FromSerializable(copy.ToSerializable())", "loaded.CreateClone()",
                     "ModelDb.GetById<CardModel>", "EnergyOverloadEnchantment { Amount: 2 }",
                     "SetEquals(expected)", "ModelDb.Card<Bash>()", "foreach (bool upgraded",
                     "Enum.GetValues<CardMultiplayerConstraint>()"):
            self.assertIn(term, code)
        for forbidden in ("CardCmd.AutoPlay", "CardPileCmd.Add", "PowerCmd.Apply", "SaveManager"):
            self.assertNotIn(forbidden, code)
        self.assertIn("IsNetworked => false", code)
        self.assertIn("issuingPlayer.RunState.Players.Count != 1", code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
