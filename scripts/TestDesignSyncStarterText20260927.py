"""Starter text/variation declarations, not a claim of game rendering execution."""
import json
from pathlib import Path
import re
import unittest

import AuditCardLocalization as audit
from TestDesignSyncNeutral20260927 import read

CONTRACT = 'src/Debugging/CardEffects/DesignSyncStarterTextContract.cs'


def expectations():
    literal = r'"(?:[^"\\]|\\.)*"'
    return {name: (json.loads(base), json.loads(up)) for name, base, up in
            re.findall(r'new\(typeof\((\w+)\), (' + literal + r'), (' + literal + r')\)', read(CONTRACT))}


def formal_transform(design, upgraded, corrupt):
    block = design.split('\n变身\n技能牌  基础（圣洁卡）\n', 1)[1].split('\n黑暗元素\n', 1)[0]
    sentences = re.findall(r'^1费\s+(.+)$', block, re.M)
    if len(sentences) != 2:
        raise AssertionError('Transform must have two complete formal variants')
    text = sentences[1 if corrupt else 0].replace('*', '')
    if not text.endswith('升级后获得固有。'):
        raise AssertionError('Transform upgrade rule changed')
    text = text.removesuffix('升级后获得固有。')
    return ('固有。' if upgraded else '') + text


class StarterTextContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows = expectations()
        cls.source = read(CONTRACT)
        cls.design = read('DesignDoc.md')

    def test_five_registered_models_nine_applicable_full_texts(self):
        self.assertEqual(set(self.rows), {'MaidenStrike', 'MaidenDefend', 'Transform', 'DrowsyStatus', 'BindingInsight'})
        registered, _ = audit.registered_cards()
        self.assertTrue(set(self.rows).issubset(registered))
        count = 0
        for name, pair in self.rows.items():
            for text in pair[:1] if name == 'DrowsyStatus' else pair:
                self.assertNotIn('{', text)
                self.assertNotIn('\n\n', text)
                self.assertTrue(text.endswith('。'))
                count += 1
        self.assertEqual(count, 9)

    def test_transform_formal_variants_both_upgrade_states(self):
        corrupt = json.loads(re.search(r'CorruptText = ("(?:[^"\\]|\\.)*");', self.source)[1])
        for upgraded in (False, True):
            self.assertEqual(self.rows['Transform'][upgraded].replace('\n', ''), formal_transform(self.design, upgraded, False))
            expected = ('固有。' if upgraded else '') + corrupt.replace('\n', '')
            self.assertEqual(expected, formal_transform(self.design, upgraded, True))
            self.assertNotEqual(expected.replace('＜3', '≤3'), formal_transform(self.design, upgraded, True))
            self.assertNotEqual(expected.replace('。', '，', 1), formal_transform(self.design, upgraded, True))

    def test_binding_full_design_and_one_energy_icon(self):
        text = audit.design_effect('绳缚的心得', self.design.splitlines())
        self.assertTrue(text.startswith('1/0费'))
        text = re.sub(r'^1/0费\s*', '', text).replace('获得1费', '获得〈能量〉')
        for sentence in self.rows['BindingInsight']:
            self.assertEqual(sentence.replace('\n', ''), text)
            self.assertEqual(sentence.count('〈能量〉'), 1)
            self.assertTrue(sentence.endswith('\n随身。'))

    def test_drowsy_native_wording_not_an_empty_description(self):
        text = audit.design_effect('困了', self.design.splitlines())
        self.assertEqual(text, '无法被打出。保留）')
        self.assertEqual(self.rows['DrowsyStatus'][0], '不能被打出。\n保留。')
        self.assertIn('card is DrowsyStatus && upgraded', self.source)
        catalog = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs')
        self.assertIn('EmptyDescription<DrowsyStatus>();', catalog)
        # Native keyword titles/order supply rendered text even with empty localization.
        loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        self.assertEqual(loc['MAIDEN_SUCCUBUS_CARD_DROWSY_STATUS.description'], '')

    def test_native_starter_defaults_are_not_fabricated_design_numbers(self):
        self.assertIn('4张打击，4张防御。', self.design)
        kb = json.loads((audit.ROOT.parents[1] / 'KnowledgeBase/cards.json').read_text(encoding='utf-8-sig'))
        for model, native, key, values in (('MaidenStrike', 'StrikeIronclad', 'Damage', (6, 9)),
                                          ('MaidenDefend', 'DefendIronclad', 'Block', (5, 8))):
            entry = kb[audit.screaming_snake(native)]
            self.assertEqual(entry['vars'][key][0], values[0])
            self.assertEqual(entry['cost'], 1)
            native_source = (audit.ROOT.parents[1] / '_decompiled/sts2-v0.111.0/MegaCrit.Sts2.Core.Models.Cards' / (native + '.cs')).read_text(encoding='utf-8-sig')
            self.assertIn('DynamicVars.' + key + '.UpgradeValueBy(3m)', native_source)
            for upgraded, amount in enumerate(values):
                expected = f'造成{amount}点伤害。' if key == 'Damage' else f'获得{amount}点格挡。'
                self.assertEqual(self.rows[model][upgraded], expected)

    def test_variation_boundary_reentry_and_cleanup(self):
        cases = re.search(r'RouteCases\s*=\s*\[(.*?)\];', self.source, re.S)[1]
        actual = [(int(value), state == 'true') for value, state in re.findall(r'\((-?\d+), (true|false)\)', cases)]
        self.assertEqual(actual, [(-5, False), (-3, False), (-2, False), (0, False), (2, False),
                                  (3, True), (5, True), (2, False), (3, True), (0, False)])
        for token in ('int saved = CorruptionQuery.Get(run)', 'finally { CorruptionCmd.Set(run, saved); }',
                      'RouteCardQuery.Get(instance)', '!CombatSealQuery.IsSealed(run, instance)',
                      'foreach (var test in RouteCases)'):
            self.assertIn(token, self.source)

    def test_true_run_combat_metadata_and_no_trim(self):
        for token in ('RunState.CreateCard(', 'CardCmd.Upgrade(outside)', 'PileType.Deck', 'PileType.Hand',
                      'card.Keywords.SetEquals(keywords)', 'card.Tags.ToHashSet().SetEquals',
                      'typeof(MSNeutralCardPool)', 'typeof(MSHolyCardPool)', 'typeof(MSGeneratedCardPool)', 'effect: false'):
            self.assertIn(token, self.source)
        for token in ('.Trim(', 'RemoveEmptyEntries', 'GetFormattedText(', 'DynamicVars.Damage.BaseValue'):
            self.assertNotIn(token, self.source)

    def test_global_and_five_card_group_keep_real_effects(self):
        runner = read('src/Debugging/CardEffects/CardEffectTestRunner.cs')
        for token in ('DesignSyncStarterTextContract.Validate(context, card, scenario.Upgraded);',
                      '"ds27-starter-text"', 'batch.Length != 5 || batch.Length != DesignSyncStarterTextContract.Entries.Length',
                      'await scenario.Execute(context, card);', 'scenario.MinimumEffectAssertions'):
            self.assertIn(token, runner)


if __name__ == '__main__':
    unittest.main()
