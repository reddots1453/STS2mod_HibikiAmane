"""Bounded binding audit regression tests; mutations remain in memory."""
import contextlib
import io
import json
import re
import unittest
from pathlib import Path
from unittest.mock import patch

import AuditUpgradeTextBindings as audit


def card(body):
    return "class Example { protected override void OnUpgrade() { " + body + " } }"


class UpgradeBindingTests(unittest.TestCase):
    def inspect(self, body, text, extra=""):
        return audit.inspect_upgrade([card(body) + extra], [text])

    def test_property_and_index_are_direct(self):
        row = self.inspect('DynamicVars.Damage.UpgradeValueBy(2); DynamicVars["Block"].UpgradeValueBy(3);',
                           '{Damage:diff()} {Block:diff()}')
        self.assertEqual(row["status"], "BOUND")
        self.assertEqual(row["direct"], ["Block", "Damage"])

    def test_literal_does_not_bind(self):
        row = self.inspect('DynamicVars["ShatterPower"].UpgradeValueBy(1);', "给予1层破碎。")
        self.assertEqual(row["status"], "MISSING_BINDING")
        self.assertEqual(row["missing"], ["ShatterPower"])

    def test_comments_and_string_expressions_are_ignored(self):
        row = self.inspect('''// DynamicVars.Fake.UpgradeValueBy(9);
            /* DynamicVars.Other.UpgradeValueBy(2); */
            var message = "DynamicVars.Nope.UpgradeValueBy(1);";
            DynamicVars.Damage.UpgradeValueBy(2);''', "{Damage}")
        self.assertEqual(row["writes"], ["Damage"])
        self.assertEqual(row["status"], "BOUND")

    def test_inherited_upgrade_and_explicit_base_call(self):
        parent = card("DynamicVars.Damage.UpgradeValueBy(2);")
        for child in ("class Child {}", card("base.OnUpgrade();")):
            row = audit.inspect_upgrade([child, parent], ["{Damage}"])
            self.assertEqual(row["writes"], ["Damage"])
        row = audit.inspect_upgrade([card("AddKeyword(CardKeyword.Exhaust);"), parent], ["消耗。"])
        self.assertEqual(row["writes"], [])

    def test_expression_bodied_upgrade(self):
        row = audit.inspect_upgrade(['protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);'], ["{Damage}"])
        self.assertEqual(row["status"], "BOUND")

    def test_calculated_dependencies_require_source_evidence(self):
        for definition, output in (("new CalculatedDamageVar(1)", "CalculatedDamage"),
                                   ("new CalculatedBlockVar(1)", "CalculatedBlock"),
                                   ('new CalculatedVar("CalculatedHeal", 1)', "CalculatedHeal")):
            row = self.inspect('DynamicVars["CalculationBase"].UpgradeValueBy(2);',
                               "{" + output + ":diff()}", definition)
            self.assertEqual(row["indirect"], ["CalculationBase"])
            row = self.inspect('DynamicVars["CalculationBase"].UpgradeValueBy(2);', "{" + output + "}")
            self.assertEqual(row["status"], "MISSING_BINDING")

    def test_fake_calculated_definition_is_not_evidence(self):
        for definition in ('// new CalculatedDamageVar(1)', '"new CalculatedDamageVar(1)"'):
            row = self.inspect('DynamicVars["CalculationBase"].UpgradeValueBy(2);', "{CalculatedDamage}", definition)
            self.assertEqual(row["status"], "MISSING_BINDING")

    def test_icon_string_requires_upgrade_update(self):
        body = 'DynamicVars["Resource"].UpgradeValueBy(1);'
        update = '((StringVar)DynamicVars["Icons"]).StringValue = Format(DynamicVars["Resource"].IntValue);'
        row = self.inspect(body + update, "{Icons}")
        self.assertEqual(row["indirect"], ["Resource"])
        row = self.inspect(body, "{Icons}", 'new StringVar("Icons", Format(1))')
        self.assertEqual(row["status"], "MISSING_BINDING")

    def test_transitive_dependency_and_cycle_terminate(self):
        row = self.inspect('''DynamicVars["Resource"].UpgradeValueBy(1);
            ((StringVar)DynamicVars["Icons"]).StringValue = Format(DynamicVars["Intermediate"].IntValue);
            ((StringVar)DynamicVars["Intermediate"]).StringValue = Format(DynamicVars["Icons"].IntValue + DynamicVars["Resource"].IntValue);''', "{Icons}")
        self.assertEqual(row["indirect"], ["Resource"])

    def test_unsupported_writes_do_not_silently_pass(self):
        for operation in ("BaseValue = 4", "IntValue = 4", "SetValue(4)"):
            row = self.inspect("DynamicVars.Damage." + operation + ";", "{Damage}")
            self.assertEqual(row["status"], "UNRESOLVED")
            self.assertTrue(row["unsupported"])

    def test_absent_description_fails_even_without_scalar_upgrade(self):
        rows = audit.audit({"Example": ("", None)}, {"Example": ("class Example {}", "CardModel", None)}, {})
        self.assertEqual(rows[0]["status"], "MISSING_DESCRIPTION")

    def test_inheritance_cycle_is_an_error(self):
        with self.assertRaisesRegex(ValueError, "Inheritance cycle"):
            audit.inherited_blocks("A", {"A": ("", "B", None), "B": ("", "A", None)})

    def test_cli_json_and_failure_exit_code(self):
        cards = {"Example": ("", None)}
        classes = {"Example": (card("DynamicVars.Damage.UpgradeValueBy(1);"), "CardModel", None)}
        with patch.object(audit, "registered_cards", return_value=(cards, classes)), \
             patch.object(audit, "load_json_with_review_comments", return_value={}), \
             patch.object(Path, "write_text", side_effect=AssertionError("must be read-only")), \
             contextlib.redirect_stdout(io.StringIO()) as output:
            self.assertEqual(audit.main(["--json"]), 1)
        self.assertEqual(json.loads(output.getvalue())[0]["status"], "MISSING_DESCRIPTION")

    def test_localization_gate_calls_read_only_audit_and_checks_exit(self):
        gate = (Path(__file__).parent / "ValidateLocalizationStyle.ps1").read_text(encoding="utf-8-sig")
        self.assertRegex(gate, r'\$upgradeBindingScript = Join-Path \$ProjectDir "scripts/AuditUpgradeTextBindings.py"\s*& python \$upgradeBindingScript\s*if \(\$LASTEXITCODE -ne 0\)')
        self.assertIn('$failures.Add("upgrade scalar description binding audit failed")', gate)


class LiveRegistryBindingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.cards, cls.classes = audit.registered_cards()
        cls.loc = audit.load_json_with_review_comments(audit.LOC_PATH)

    def test_live_registry_has_no_unresolved_bindings(self):
        rows = audit.audit(self.cards, self.classes, self.loc)
        self.assertEqual(len(rows), len(self.cards))
        self.assertGreaterEqual(len(rows), 225)
        failures = [row for row in rows if row["status"] not in ("BOUND", "NO_DIRECT_SCALAR_UPGRADE")]
        self.assertEqual(failures, [])
        self.assertTrue(any(row["indirect"] for row in rows))

    def test_cyclone_literal_regression_is_detected(self):
        loc = dict(self.loc)
        key = "MAIDEN_SUCCUBUS_CARD_CYCLONE_RUPTURE.description"
        self.assertIn("{ShatterPower:diff()}", loc[key])
        loc[key] = loc[key].replace("{ShatterPower:diff()}", "1")
        row = next(row for row in audit.audit({"CycloneRupture": self.cards["CycloneRupture"]}, self.classes, loc))
        self.assertEqual(row["status"], "MISSING_BINDING")
        self.assertIn("ShatterPower", row["missing"])

    def test_missing_icon_refresh_regression_is_detected(self):
        classes = dict(self.classes)
        block, parent, path = classes["Tranquilizer"]
        mutated, count = re.subn(r'\(\(StringVar\)DynamicVars\["DesireIcons"\]\)\.StringValue\s*=[^;]+;', "", block)
        self.assertEqual(count, 1)
        classes["Tranquilizer"] = (mutated, parent, path)
        row = audit.audit({"Tranquilizer": self.cards["Tranquilizer"]}, classes, self.loc)[0]
        self.assertEqual(row["status"], "MISSING_BINDING")
        self.assertIn("DesireLoss", row["missing"])


if __name__ == "__main__":
    unittest.main()
