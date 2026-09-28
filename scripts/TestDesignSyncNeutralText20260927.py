"""Check independent runtime text oracles against DesignDoc, not against themselves.

This is not a SmartFormat renderer. Godot/native keyword rendering is exercised
by the separately compiled ds27-neutral-text suite, not this offline test.
"""
import json
import re
import unittest
import AuditCardLocalization as audit
from TestDesignSyncNeutral20260927 import read


def expectations():
    source = read("src/Debugging/CardEffects/DesignSyncNeutralTextContract.cs")
    literal = r'"(?:[^"\\]|\\.)*"'
    matches = re.findall(r'new\(typeof\((\w+)\), (' + literal + r'), (' + literal + r')\)', source)
    return [(name, json.loads(base), json.loads(upgraded)) for name, base, upgraded in matches]


def design_sentence(title, lines, upgraded):
    effect = audit.design_effect(title, lines)
    if not effect:
        raise AssertionError("No full design entry for " + title)
    effect = re.sub(r'^\d+(?:/\d+)?费\s*', '', effect).replace('*', '')
    effect = re.sub(r'(\d+)/(\d+)', lambda m: m[2 if upgraded else 1], effect)
    effect = effect.replace('（升级：升级过的）', '升级过的' if upgraded else '')
    if title == '冰之盾':
        effect = effect.replace('（升级后改为“冰晶碎片+”）', '')
        if upgraded:
            effect = effect.replace('冰晶碎片', '冰晶碎片+')
    if '升级后获得保留。' in effect:
        effect = effect.replace('升级后获得保留。', '')
        if upgraded:
            effect = '保留。' + effect
    if title == '冰晶碎片':
        effect = '保留。' + effect.replace('保留。', '')
    # User explicitly requested energy icons. Preserve their independent count
    # and the established >=4 compact-number convention; don't erase them.
    def icons(number):
        amount = int(number)
        return '〈能量〉' * amount if 0 < amount < 4 else str(amount) + '〈能量〉'
    effect = re.sub(r'(\d+)费', lambda m: icons(m[1]), effect)
    if title in ('燃烧手环', '冰冻手环'):
        effect = effect.replace('耗能减少1点', '耗能减少〈能量〉')
    return effect


class NeutralFullTextContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows = expectations()
        cls.loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        cls.lines = read('DesignDoc.md').splitlines()

    def test_exact_30_registered_models_and_both_literal_variants(self):
        self.assertEqual(len(self.rows), 30)
        self.assertEqual(len({name for name, _, _ in self.rows}), 30)
        cards, _ = audit.registered_cards()
        for name, base, upgraded in self.rows:
            self.assertIn(name, cards)
            for text in (base, upgraded):
                self.assertNotIn('{', text)
                self.assertNotIn('\n\n', text)
                self.assertNotIn('res://', text)
                self.assertTrue(text.endswith('。'))

    def test_all_60_independent_sentences_match_current_design_punctuation_and_values(self):
        for name, base, upgraded in self.rows:
            title = self.loc['MAIDEN_SUCCUBUS_CARD_' + audit.screaming_snake(name) + '.title']
            for is_upgraded, expected in ((False, base), (True, upgraded)):
                with self.subTest(card=name, upgraded=is_upgraded):
                    self.assertEqual(expected.replace('\n', ''), design_sentence(title, self.lines, is_upgraded))

    def test_runtime_preserves_exact_layout_and_uses_true_run_instance(self):
        source = read('src/Debugging/CardEffects/DesignSyncNeutralTextContract.cs')
        for token in ('RunState.CreateCard(', 'CardCmd.Upgrade(outside)', '(outside, PileType.Deck)',
                      '(card, PileType.Hand)', 'instance.UpdateDynamicVarPreview(',
                      'instance.GetDescriptionForPile(pile, ctx.PrimaryEnemy)', 'Normalize(actual), effect: false'):
            self.assertIn(token, source)
        self.assertNotIn('.Trim(', source)
        self.assertNotIn('expected.Replace', source)
        self.assertNotIn('DynamicVars.Damage.BaseValue', source)

    def test_runtime_checks_use_the_unchanged_actual_effect_scenarios(self):
        source = read('src/Debugging/CardEffects/CardEffectTestRunner.cs')
        self.assertIn('DesignSyncNeutralTextContract.Validate(context, card, scenario.Upgraded);', source)
        self.assertIn('"ds27-neutral-text"', source)
        self.assertIn('batch.Length != 30 || batch.Length != DesignSyncNeutralTextContract.Entries.Length', source)
        self.assertIn('await scenario.Execute(context, card);', source)
        self.assertIn('scenarioResult.EffectAssertionCount < scenario.MinimumEffectAssertions', source)

    def test_energy_counts_and_wrong_resource_are_not_erased(self):
        source = read('src/Debugging/CardEffects/DesignSyncNeutralTextContract.cs')
        self.assertIn('"[img]" + MaidenEnergyIconAssets.TextIconResourcePath + "[/img]"', source)
        self.assertIn('text.Replace(icon, "〈能量〉", StringComparison.Ordinal)', source)
        rows = {name: (base, upgrade) for name, base, upgrade in self.rows}
        self.assertIn('〈能量〉〈能量〉〈能量〉', rows['Surf'][0])
        self.assertIn('4〈能量〉', rows['Surf'][1])
        self.assertIn('〈能量〉〈能量〉', rows['BorrowedForceStrike'][1])
        self.assertEqual(rows['MagicStarBomb'][0].count('〈能量〉'), 1)

    def test_numeric_and_punctuation_regressions_cannot_match_oracle(self):
        for name, base, _ in self.rows:
            title = self.loc['MAIDEN_SUCCUBUS_CARD_' + audit.screaming_snake(name) + '.title']
            oracle = design_sentence(title, self.lines, False)
            self.assertNotEqual(base.replace('\n', '').replace('。', '，', 1), oracle)
            if re.search(r'\d', base):
                mutated = re.sub(r'\d+', lambda m: str(int(m[0]) + 1), base, count=1)
                self.assertNotEqual(mutated.replace('\n', ''), oracle)

    def test_dream_pigment_route_colors_and_all_localization_has_no_purple_holy_label(self):
        text = self.loc['MAIDEN_SUCCUBUS_CARD_DREAM_PIGMENT.description']
        self.assertIn('[gold]圣洁牌[/gold]', text)
        self.assertIn('[purple]堕落牌[/purple]', text)
        for path in (audit.ROOT / 'MaidenSuccubus/localization/zhs').glob('*.json'):
            values = audit.load_json_with_review_comments(path)
            for key, value in values.items():
                if isinstance(value, str):
                    self.assertNotRegex(value, r'\[purple\]圣洁[^\[]*\[/purple\]', msg=path.name + '/' + key)

    def test_keywords_and_generated_chain_are_explicit_not_inferred(self):
        rows = {name: (base, upgrade) for name, base, upgrade in self.rows}
        self.assertNotIn('消耗', ''.join(rows['MindsEye']))
        self.assertNotIn('保留', rows['SwordVerdict'][0])
        self.assertTrue(rows['SwordVerdict'][1].startswith('保留。\n'))
        self.assertTrue(all(text.startswith('保留。\n') and text.endswith('\n消耗。') for text in rows['IceShard']))
        self.assertTrue(rows['CounterBarrier'][0].endswith('\n沉底。'))
        self.assertIn('冰晶碎片+', rows['IceShield'][1])
        for name in ('CounterBarrierII', 'CounterBarrierIII', 'CounterBarrierIV'):
            self.assertNotIn('沉底', ''.join(rows[name]))


if __name__ == '__main__':
    unittest.main()
