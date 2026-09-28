"""Seven full-text declarations; execution in Godot is explicitly separate."""
import json
import re
import unittest

import AuditCardLocalization as audit
from TestDesignSyncNeutral20260927 import read


def expectations():
    source = read('src/Debugging/CardEffects/DesignSyncRemainingTextContract.cs')
    literal = r'"(?:[^"\\]|\\.)*"'
    return [(name, json.loads(base), json.loads(upgrade)) for name, base, upgrade in
            re.findall(r'new\(typeof\((\w+)\), (' + literal + r'), (' + literal + r')\)', source)]


def design_sentence(title, lines, upgraded):
    text = audit.design_effect(title, lines)
    if not text:
        raise AssertionError('Missing formal entry: ' + title)
    text = re.sub(r'^\d+(?:/\d+)?费\s*', '', text)
    if title == '超再生':
        if not text.startswith('稀有\n'):
            raise AssertionError('SuperRegeneration rarity annotation changed')
        text = text.removeprefix('稀有\n')
    text = re.sub(r'(\d+)/(\d+)', lambda m: m[2 if upgraded else 1], text)
    if title == '炎之剑':
        text = text.replace('（还剩？场战斗）', '（还剩5场战斗）')
    if title == '口球':
        text = text.replace('耗能1点', '耗能〈能量〉')
    if title == '黑暗风暴' and upgraded:
        text += '重放1。'  # Native zhs REPLAY.extraText, Times=1; not an added DesignDoc rule.
    return text


class RemainingTextContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows = expectations()
        cls.loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        cls.lines = read('DesignDoc.md').splitlines()

    def test_seven_models_and_thirteen_applicable_full_sentences(self):
        self.assertEqual(len(self.rows), 7)
        self.assertEqual({r[0] for r in self.rows}, {'FlameSword', 'WindGodCloak', 'BeyondReasonForge',
                          'SuperRegeneration', 'GagCurse', 'DarkStorm', 'CurseInfection'})
        count = 0
        for name, base, up in self.rows:
            title = self.loc['MAIDEN_SUCCUBUS_CARD_' + audit.screaming_snake(name) + '.title']
            variants = [(False, base)] if name == 'GagCurse' else [(False, base), (True, up)]
            for upgraded, expected in variants:
                with self.subTest(card=name, upgraded=upgraded):
                    self.assertEqual(expected.replace('\n', ''), design_sentence(title, self.lines, upgraded))
                    self.assertNotIn('{', expected)
                    self.assertNotIn('\n\n', expected)
                    self.assertFalse(expected.endswith('\n'))
                    count += 1
        self.assertEqual(count, 13)

    def test_punctuation_and_number_mutations_are_not_equivalent(self):
        for name, base, _ in self.rows:
            title = self.loc['MAIDEN_SUCCUBUS_CARD_' + audit.screaming_snake(name) + '.title']
            expected = design_sentence(title, self.lines, False)
            self.assertNotEqual(base.replace('\n', '').replace('。', '，', 1), expected)
            if re.search(r'\d', base):
                wrong = re.sub(r'\d+', lambda m: str(int(m[0]) + 1), base, count=1)
                self.assertNotEqual(wrong.replace('\n', ''), expected)

    def test_real_run_instance_no_trim_and_base_only_curse(self):
        source = read('src/Debugging/CardEffects/DesignSyncRemainingTextContract.cs')
        for token in ('RunState.CreateCard(', 'CardCmd.Upgrade(outside)', 'PileType.Deck', 'PileType.Hand',
                      'card is GagCurse && upgraded', 'remaining exact run text', 'remaining exact combat text'):
            self.assertIn(token, source)
        for forbidden in ('.Trim(', 'RemoveEmptyEntries', 'DynamicVars.Damage.BaseValue', 'GetFormattedText('):
            self.assertNotIn(forbidden, source)
        catalog = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs')
        self.assertIn('HandCostRestriction<GagCurse>(CardType.Skill, 1, expectedOwnCost: 1);', catalog)

    def test_flame_hidden_counter_cannot_leave_blank_line(self):
        template = self.loc['MAIDEN_SUCCUBUS_CARD_FLAME_SWORD.description']
        conditional = '{ShowRemaining:\n（还剩{Remaining:diff()}场战斗）|}'
        self.assertTrue(template.endswith('。' + conditional))
        self.assertNotIn('\n{ShowRemaining:', template)
        hidden = template.replace(conditional, '')
        self.assertFalse(hidden.endswith('\n'))
        self.assertNotIn('\n\n', hidden + '\n永恒。')
        mutated = template.replace(conditional, '\n' + conditional)
        self.assertIn('\n\n', mutated.replace(conditional, '') + '\n永恒。')

    def test_native_suffixes_and_progress_are_explicit(self):
        rows = {name: (base, up) for name, base, up in self.rows}
        self.assertEqual(rows['DarkStorm'][1], rows['DarkStorm'][0] + '\n重放1。')
        self.assertEqual(rows['SuperRegeneration'][0], '从消耗堆选择打出一张牌。\n消耗。\n魔力解放：将此牌放回手牌。')
        source = read('src/Debugging/CardEffects/DesignSyncRemainingTextContract.cs')
        self.assertIn('completed == 5 ? (upgraded ? "15" : "12") : (upgraded ? "12" : "9")', source)
        self.assertIn('completed == 5 ? "\\n永恒。"', source)
        flame = read('src/Debugging/CardEffects/DesignSyncFlameSwordContract.cs')
        self.assertIn('FlameProgress(ctx, deck, PileType.Deck, upgraded, completed)', flame)
        self.assertIn('FlameProgress(ctx, nextCombat, PileType.Hand, upgraded, 5)', flame)

    def test_runner_keeps_existing_effects_and_exact_group(self):
        runner = read('src/Debugging/CardEffects/CardEffectTestRunner.cs')
        for token in ('DesignSyncRemainingTextContract.Validate(context, card, scenario.Upgraded);',
                      '"ds27-remaining-text"', 'batch.Length != 7 || batch.Length != DesignSyncRemainingTextContract.Entries.Length',
                      'await scenario.Execute(context, card);', 'scenario.MinimumEffectAssertions'):
            self.assertIn(token, runner)


if __name__ == '__main__':
    unittest.main()
