"""Evidence classification regressions; no Godot/game execution."""
import contextlib
import io
from pathlib import Path
import unittest
from unittest.mock import patch

import ReportCardTextCoverage20260927 as coverage


class CardTextInventoryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.report = coverage.inventory()
        cls.rows = {row['model']: row for row in cls.report['cards']}

    def test_all_registered_models_present_once_and_no_execution_claim(self):
        self.assertEqual(len(self.rows), 227)
        self.assertEqual(len(self.report['cards']), 227)
        self.assertEqual(self.report['integrityErrors'], [])
        self.assertFalse(self.report['summary']['goalCompleted'])
        self.assertTrue(all(row['runtime'] == 'not_run' for row in self.rows.values()))

    def test_metadata_entries_cannot_create_full_text_coverage(self):
        providers = {item['provider'] for row in self.rows.values() for item in row['providers']}
        self.assertNotIn(coverage.PREFIX + 'DesignSyncNeutralContract.cs', providers)
        self.assertNotIn(coverage.PREFIX + 'DesignSyncHolyContract.cs', providers)
        self.assertEqual(self.rows['HumilityLesson']['textEvidence'], coverage.MISSING)

    def test_partial_and_same_combat_instance_none_are_not_double_scene_full_text(self):
        for model in ('DarkStorm', 'CurseInfection'):
            self.assertIn(coverage.PARTIAL, [item['scope'] for item in self.rows[model]['providers']])
            self.assertEqual(self.rows[model]['textEvidence'], coverage.BOTH)
        for model in ('FlameSword', 'WindGodCloak', 'BeyondReasonForge', 'SuperRegeneration', 'GagCurse'):
            self.assertIn(coverage.COMBAT, [item['scope'] for item in self.rows[model]['providers']])
            self.assertEqual(self.rows[model]['textEvidence'], coverage.BOTH)
        for model in ('CalmMind', 'GoddessOfIce', 'AcceleratedMotion', 'InsatiableGreed', 'ResistanceGloves'):
            self.assertEqual(self.rows[model]['textEvidence'], coverage.BOTH)

    def test_technical_retired_and_unidentified_are_distinct(self):
        self.assertEqual(self.rows['LibraryPileChoice']['category'], 'technical')
        self.assertEqual(self.rows['MagicResonance']['category'], 'retired_compatibility')
        self.assertEqual(self.rows['MaidenStrike']['category'], 'current_card_or_derivative')
        self.assertEqual(self.rows['MaidenStrike']['textEvidence'], coverage.BOTH)
        self.assertEqual(self.report['summary']['current'], 217)

    def test_new_provider_disconnect_restores_partial_not_false_full_coverage(self):
        report = self.mutated(coverage.RUNNER, 'DesignSyncRemainingTextContract.Validate(context, card, scenario.Upgraded);', '')
        self.assertTrue(report['integrityErrors'])
        rows = {row['model']: row for row in report['cards']}
        for model in ('DarkStorm', 'CurseInfection'):
            self.assertEqual(rows[model]['textEvidence'], coverage.PARTIAL)
        for model in ('FlameSword', 'WindGodCloak', 'BeyondReasonForge', 'SuperRegeneration', 'GagCurse'):
            self.assertEqual(rows[model]['textEvidence'], coverage.COMBAT)

    def test_starter_provider_disconnect_does_not_use_old_effect_only_tests(self):
        report = self.mutated(coverage.RUNNER, 'DesignSyncStarterTextContract.Validate(context, card, scenario.Upgraded);', '')
        self.assertTrue(report['integrityErrors'])
        rows = {row['model']: row for row in report['cards']}
        for model in ('MaidenStrike', 'MaidenDefend', 'Transform', 'DrowsyStatus', 'BindingInsight'):
            self.assertEqual(self.rows[model]['textEvidence'], coverage.BOTH)
            self.assertEqual(rows[model]['textEvidence'], coverage.MISSING)

    def mutated(self, path, old, new):
        def read(file):
            text = coverage.read(file)
            if file == path:
                self.assertIn(old, text)
                return text.replace(old, new)
            return text
        return coverage.inventory(read)

    def test_global_runner_disconnect_invalidates_provider(self):
        report = self.mutated(coverage.RUNNER, 'DesignSyncHolyTextContract.Validate(context, card, scenario.Upgraded);', '')
        self.assertTrue(report['integrityErrors'])
        row = next(r for r in report['cards'] if r['model'] == 'ResistanceGloves')
        self.assertEqual(row['textEvidence'], coverage.MISSING)

    def test_missing_real_run_instance_or_assertion_invalidates_provider(self):
        path = coverage.PREFIX + 'DesignSyncCalmMindContract.cs'
        for old, new in (('RunState.CreateCard', 'Combat.CreateCard'), ('calm mind exact run text', 'removed assertion')):
            self.assertTrue(self.mutated(path, old, new)['integrityErrors'])

    def test_catalog_disconnect_and_unregistered_type_are_errors(self):
        report = self.mutated(coverage.CATALOG, 'CustomVariants<CalmMind>(DesignSyncCalmMindContract.Run, 90)', 'Missing();')
        self.assertTrue(report['integrityErrors'])
        report = self.mutated(coverage.PREFIX + 'DesignSyncHolyTextContract.cs', 'typeof(ResistanceGloves)', 'typeof(UnknownCard)')
        self.assertTrue(any('unregistered model UnknownCard' in error for error in report['integrityErrors']))

    def test_missing_provider_file_is_error_not_empty_success(self):
        def read(path):
            if path == coverage.PREFIX + 'DesignSyncHolyTextContract.cs':
                raise FileNotFoundError(path)
            return coverage.read(path)
        self.assertTrue(coverage.inventory(read)['integrityErrors'])

    def test_default_is_read_only_and_output_cannot_target_shared_report(self):
        with patch.object(Path, 'open', side_effect=AssertionError('unexpected write')), \
                patch.object(coverage, 'inventory', return_value=self.report), contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(coverage.main([]), 0)
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            coverage.main(['--output', str(coverage.audit.ROOT / '.review/card_localization_audit.json')])


if __name__ == '__main__':
    unittest.main()
