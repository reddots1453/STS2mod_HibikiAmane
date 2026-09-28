"""Design-derived full text checks; native rendering is a separate, unrun game test."""
import json
import re
import subprocess
import unittest

import AuditCardLocalization as audit
from TestDesignSyncNeutral20260927 import read


def expectations():
    source = read('src/Debugging/CardEffects/DesignSyncCombatTextContract.cs')
    literal = r'"(?:[^"\\]|\\.)*"'
    return [(name, json.loads(base), json.loads(upgrade)) for name, base, upgrade in
            re.findall(r'new\(typeof\((\w+)\), (' + literal + r'), (' + literal + r')\)', source)]


def design_sentence(title, lines, upgraded):
    text = audit.design_effect(title, lines)
    if not text:
        raise AssertionError('Missing complete design entry: ' + title)
    text = re.sub(r'^\d+(?:/\d+)?费\s*', '', text)
    text = text.removeprefix('不显示初始费用').lstrip()
    # These are explicit annotations, not arbitrary punctuation removal.
    if title == '休憩':
        text = text.removesuffix('\n卡图对应“摸鱼”')
    if title == '冰雾':
        text = text.removesuffix('）')
    if title == '魔力爆发':
        text = text.removesuffix('（造成？点伤害）')
    if title == '建御雷神':
        text = text.removesuffix('（造成？次伤害）')
    text = re.sub(r'(\d+)/(\d+)', lambda m: m[2 if upgraded else 1], text)
    if '升级后获得保留。' in text:
        text = text.replace('升级后获得保留。', '')
        if upgraded:
            text = '保留。' + text
    if title == '冰雾':
        text = '保留。' + text.replace('保留。', '')
    if title == '休憩':
        text = text.replace('获得2费', '获得〈能量〉〈能量〉')
    return text


class CombatTextContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows = expectations()
        cls.loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        cls.lines = read('DesignDoc.md').splitlines()

    def test_eight_distinct_registered_models_with_literal_base_and_upgrade(self):
        expected_types = {'FlameBloom', 'Lullaby', 'IceMist', 'MagicBurst', 'Takemikazuchi',
                          'Rest', 'BattleTechniqueReplay', 'LightWings'}
        self.assertEqual(len(self.rows), 8)
        self.assertEqual({row[0] for row in self.rows}, expected_types)
        cards, _ = audit.registered_cards()
        for name, base, upgraded in self.rows:
            self.assertIn(name, cards)
            for sentence in (base, upgraded):
                self.assertNotIn('{', sentence)
                self.assertNotIn('\n\n', sentence)
                self.assertTrue(sentence.endswith('。'))

    def test_sixteen_independent_full_sentences_match_design(self):
        for name, base, upgraded in self.rows:
            title = self.loc['MAIDEN_SUCCUBUS_CARD_' + audit.screaming_snake(name) + '.title']
            for is_upgraded, expected in ((False, base), (True, upgraded)):
                with self.subTest(card=name, upgraded=is_upgraded):
                    self.assertEqual(expected.replace('\n', ''), design_sentence(title, self.lines, is_upgraded))

    def test_wrong_punctuation_and_numbers_do_not_match(self):
        for name, base, _ in self.rows:
            title = self.loc['MAIDEN_SUCCUBUS_CARD_' + audit.screaming_snake(name) + '.title']
            oracle = design_sentence(title, self.lines, False)
            self.assertNotEqual(base.replace('\n', '').replace('。', '，', 1), oracle)
            if re.search(r'\d', base):
                changed = re.sub(r'\d+', lambda m: str(int(m[0]) + 1), base, count=1)
                self.assertNotEqual(changed.replace('\n', ''), oracle)

    def test_conditional_totals_own_the_only_newline(self):
        for name, var, unit in (('MAGIC_BURST', 'CalculatedDamage', '点'), ('TAKEMIKAZUCHI', 'Hits', '次')):
            text = self.loc['MAIDEN_SUCCUBUS_CARD_' + name + '.description']
            token = '{InCombat:\n（造成{' + var + ':diff()}' + unit + '伤害）|}'
            self.assertTrue(text.endswith('。' + token))
            self.assertEqual(text.count('{InCombat:'), 1)
            self.assertNotIn('\n{InCombat:', text)
            # Only model this literal layout fragment; do not pretend this is SmartFormat.
            outside = text.replace(token, '')
            combat = text.replace(token, '\n（造成7点伤害）')
            self.assertFalse(outside.endswith('\n'))
            self.assertNotIn('\n\n', combat)
            mutated = text.replace(token, '\n' + token)
            self.assertTrue(mutated.replace(token, '').endswith('\n'))
            self.assertIn('\n\n', mutated.replace(token, '\n（造成7点伤害）'))

    def test_true_run_and_combat_instances_without_whitespace_normalization(self):
        source = read('src/Debugging/CardEffects/DesignSyncCombatTextContract.cs')
        for token in ('RunState.CreateCard(', 'CardCmd.Upgrade(outside)', 'PileType.Deck', 'PileType.Hand',
                      'UpdateDynamicVarPreview(', 'GetDescriptionForPile(pile, ctx.PrimaryEnemy)',
                      'DesignSyncHolyTextContract.Normalize(actual)', 'effect: false'):
            self.assertIn(token, source)
        for forbidden in ('.Trim(', 'RemoveEmptyEntries', 'Replace("\\n",', 'DynamicVars.Damage.BaseValue'):
            self.assertNotIn(forbidden, source)
        self.assertIn('MagicBurst => "\\n（造成7点伤害）"', source)
        self.assertIn('Takemikazuchi => "\\n（造成2次伤害）"', source)

    def test_actual_powershell_sentence_rule_preserves_conditional_and_plain_lines(self):
        source = read('scripts/FormatLocalizationStyle.ps1')
        pattern_line = next(line.strip() for line in source.splitlines() if '$sentencePattern =' in line)
        replacement_line = next(line.strip() for line in source.splitlines() if '$sentencePattern, $fullStop' in line)
        cases = [
            ('伤害。格挡。', r'伤害。\n格挡。'),
            (r'伤害。\n格挡。', r'伤害。\n格挡。'),
            (r'伤害。{InCombat:\n（总数{Hits}）|}', r'伤害。{InCombat:\n（总数{Hits}）|}'),
            (r'伤害。{InCombat:（总数{Hits}）|}', r'伤害。\n{InCombat:（总数{Hits}）|}'),
            (r'伤害。{Damage:diff()}', r'伤害。\n{Damage:diff()}'),
            ('伤害。', '伤害。'),
            (r'伤害。{InCombat:\n（总数{Hits}）|}后句。再一句。',
             r'伤害。{InCombat:\n（总数{Hits}）|}后句。\n再一句。'),
        ]
        # Execute the production .NET regex lines, not a Python translation or a
        # self-written formatter. No localization files are written by this test.
        command = (
            "$ErrorActionPreference='Stop'; $fullStop=[string][char]0x3002; "
            "$cases=[Console]::In.ReadToEnd()|ConvertFrom-Json; " + pattern_line + '; '
            "foreach($case in $cases){ $value=$case[0]; " + replacement_line + '; '
            "if($value -cne $case[1]){throw 'Sentence formatting mismatch'}; "
            "$expected=$value; " + replacement_line + '; '
            "if($value -cne $expected){throw 'Sentence rule is not idempotent'} }; 'PASS'"
        )
        result = subprocess.run(['pwsh', '-NoProfile', '-Command', command],
                                input=json.dumps(cases, ensure_ascii=True),
                                capture_output=True, text=True, timeout=30)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn('PASS', result.stdout)

    def test_runner_keeps_actual_effect_scenarios_and_minimum_gate(self):
        source = read('src/Debugging/CardEffects/CardEffectTestRunner.cs')
        for token in ('"ds27-combat-text"', 'batch.Length != 8 || batch.Length != DesignSyncCombatTextContract.Entries.Length',
                      'DesignSyncCombatTextContract.Validate(context, card, scenario.Upgraded);',
                      'await scenario.Execute(context, card);',
                      'scenarioResult.EffectAssertionCount < scenario.MinimumEffectAssertions'):
            self.assertIn(token, source)

    def test_dynamic_tracker_and_replay_transitions_check_actual_descriptions(self):
        source = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs')
        self.assertIn('tracker.PlayedEnchantedCards = 3;\n            DesignSyncCombatTextContract.TrackedHits(ctx, card, upgraded);', source)
        self.assertIn('DesignSyncCombatTextContract.AssertText(ctx, projected, PileType.Hand,', source)
        self.assertIn('"造成6点伤害。", "replay projection renders copied strike not source text"', source)
        self.assertIn('DesignSyncCombatTextContract.AssertText(ctx, restored, PileType.Discard,', source)
        contract = read('src/Debugging/CardEffects/DesignSyncCombatTextContract.cs')
        self.assertIn('expected + "\\n（造成5次伤害）"', contract)
        self.assertIn('"run preview does not inherit active tracker total"', contract)

    def test_native_keywords_energy_counts_and_open_stacking_not_invented(self):
        rows = {name: (base, up) for name, base, up in self.rows}
        self.assertTrue(all(text.startswith('保留。\n') and text.endswith('\n消耗。') for text in rows['IceMist']))
        for name in ('Rest', 'BattleTechniqueReplay'):
            self.assertFalse(rows[name][0].startswith('保留。'))
            self.assertTrue(rows[name][1].startswith('保留。\n'))
        self.assertTrue(all(text.count('〈能量〉') == 2 for text in rows['Rest']))
        self.assertEqual(rows['Lullaby'][0], rows['Lullaby'][1])
        self.assertEqual(rows['Lullaby'][0].count('回合结束时'), 2)


if __name__ == '__main__':
    unittest.main()
