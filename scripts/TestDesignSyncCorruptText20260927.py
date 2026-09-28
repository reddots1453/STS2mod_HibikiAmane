"""Independent complete design sentences; not a substitute for game rendering."""
import json
import re
import unittest
import AuditCardLocalization as audit
from TestDesignSyncNeutral20260927 import read
from TestDesignSyncHolyText20260927 import icons


def expectations():
    source = read('src/Debugging/CardEffects/DesignSyncCorruptTextContract.cs')
    literal = r'"(?:[^"\\]|\\.)*"'
    return [(name, json.loads(base), json.loads(upgrade)) for name, base, upgrade in
            re.findall(r'new\(typeof\((\w+)\), (' + literal + r'), (' + literal + r')\)', source)]


def sentence(title, lines, upgraded):
    text = audit.design_effect(title, lines)
    if not text:
        raise AssertionError('Missing design sentence: ' + title)
    text = re.sub(r'^\d+(?:/\d+)?费(?:\d+欲望)?\s*', '', text).replace('*', '')
    text = re.sub(r'(\d+)/(\d+)', lambda m: m[2 if upgraded else 1], text)
    text = text.replace('（升级：升级过的）', '升级过的' if upgraded else '')
    if title == '暗焰壁障':
        text, sep, note = text.partition('\n')
        if not sep or note != '（实现方式参考铁甲战士的“巨像”）':
            raise AssertionError('Dark barrier rule-note boundary changed')
    for keyword in ('固有', '虚无'):
        annotation = '升级后获得' + keyword + '。'
        if annotation in text:
            text = text.replace(annotation, '')
            if upgraded:
                text = keyword + '。' + text
    for keyword in ('保留', '固有', '虚无'):
        if keyword + '。' in text:
            text = keyword + '。' + text.replace(keyword + '。', '')
    # Confirmed native cost-icon wording, not permission to erase punctuation elsewhere.
    if title == '狂战士的假面':
        text = text.replace('攻击牌消耗变为0费', '攻击牌耗能变为0费')
    if title == '暗之惩戒':
        text = text.replace('费用减少1点', '费用减少1费')
    return re.sub(r'(\d+)费', lambda m: icons(m[1], '能量'), text)


class CorruptTextContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows = expectations()
        cls.lines = read('DesignDoc.md').splitlines()
        cls.loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))

    def test_exact_27_unique_cards_and_54_variants(self):
        self.assertEqual(len(self.rows), 27)
        self.assertEqual(len({row[0] for row in self.rows}), 27)
        pool = json.loads(read('docs/content_contract_20260824.json'))['cards']['MSCorruptCardPool']
        for model, base, upgraded in self.rows:
            self.assertIn(model, pool)
            for text in (base, upgraded):
                self.assertNotIn('{', text)
                self.assertNotIn('\n\n', text)
                self.assertTrue(text.endswith('。'))

    def test_all_54_full_sentences_match_design(self):
        for model, base, upgrade in self.rows:
            title = self.loc['MAIDEN_SUCCUBUS_CARD_' + audit.screaming_snake(model) + '.title']
            for upgraded, expected in ((False, base), (True, upgrade)):
                with self.subTest(model=model, upgraded=upgraded):
                    self.assertEqual(expected.replace('\n', ''), sentence(title, self.lines, upgraded))

    def test_punctuation_and_value_mutations_cannot_pass(self):
        for model, base, _ in self.rows:
            title = self.loc['MAIDEN_SUCCUBUS_CARD_' + audit.screaming_snake(model) + '.title']
            design = sentence(title, self.lines, False)
            self.assertNotEqual(base.replace('\n', '').replace('。', '，', 1), design)
            if re.search(r'\d', base):
                changed = re.sub(r'\d+', lambda m: str(int(m[0]) + 1), base, count=1)
                self.assertNotEqual(changed.replace('\n', ''), design)

    def test_run_and_combat_fulltext_do_not_replace_effect_tests(self):
        source = read('src/Debugging/CardEffects/DesignSyncCorruptTextContract.cs')
        for token in ('RunState.CreateCard(', 'CardCmd.Upgrade(outside)', '(outside, PileType.Deck)',
                      '(card, PileType.Hand)', 'instance.UpdateDynamicVarPreview(',
                      'instance.GetDescriptionForPile(pile, ctx.PrimaryEnemy)',
                      'DesignSyncHolyTextContract.Normalize(actual), effect: false'):
            self.assertIn(token, source)
        runner = read('src/Debugging/CardEffects/CardEffectTestRunner.cs')
        for token in ('DesignSyncCorruptTextContract.Validate(context, card, scenario.Upgraded);',
                      '"ds27-corrupt-text"', 'batch.Length != 27', 'await scenario.Execute(context, card);',
                      'scenarioResult.EffectAssertionCount < scenario.MinimumEffectAssertions'):
            self.assertIn(token, runner)

    def test_expected_native_keyword_placement_and_energy_icons(self):
        rows = {model: (base, upgrade) for model, base, upgrade in self.rows}
        for model in ('AbnormalAdaptation', 'Coronation'):
            self.assertTrue(rows[model][1].startswith('固有。\n'))
        self.assertTrue(rows['ReflectiveBarrier'][1].startswith('虚无。\n'))
        self.assertTrue(all(text.startswith('虚无。\n') for text in rows['WinterHolly']))
        self.assertTrue(all(text.endswith('\n消耗。') for text in rows['DarkFlameBarrier']))
        self.assertEqual(rows['MiasmaAbsorption'], ('获得〈能量〉〈能量〉。', '获得〈能量〉〈能量〉〈能量〉。'))
        self.assertIn('，消耗其中的1张牌。', rows['DestructionReaction'][0])
        self.assertIn('抽2张牌。\n获得', rows['FleetingYears'][0])

    def test_implementation_fixes_exact_description_and_line_breaks(self):
        self.assertEqual(self.loc['MAIDEN_SUCCUBUS_CARD_FLEETING_YEARS.description'],
                         '抽{Cards:diff()}张牌。\n获得{Energy:maidenEnergyIcons()}。')
        self.assertEqual(self.loc['MAIDEN_SUCCUBUS_CARD_DARK_FLAME_BARRIER.description'],
                         '获得{Block:diff()}点[gold]格挡[/gold]。\n持续{Turns}个回合，[gold]燃烧[/gold]的敌人对你造成的伤害降低50%。\n[gold]魔力解放[/gold]：额外持续1个回合。')

    def test_rule_note_cannot_silently_swallow_new_design(self):
        lines = list(self.lines)
        index = lines.index('（实现方式参考铁甲战士的“巨像”）')
        lines[index] += '新增说明。'
        with self.assertRaisesRegex(AssertionError, 'rule-note boundary changed'):
            sentence('暗焰壁障', lines, False)


if __name__ == '__main__':
    unittest.main()
