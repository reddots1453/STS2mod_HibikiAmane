"""Remaining scoped full-text oracles and the three confirmed implementation differences."""
import json
import re
import unittest

import AuditCardLocalization as audit
from TestDesignSyncNeutral20260927 import read
from TestDesignSyncHolyText20260927 import icons


def expectations():
    literal = r'"(?:[^"\\]|\\.)*"'
    return [(name, json.loads(base), json.loads(upgrade)) for name, base, upgrade in
            re.findall(r'new\(typeof\((\w+)\), (' + literal + r'), (' + literal + r')\)',
                       read('src/Debugging/CardEffects/DesignSyncFinalTextContract.cs'))]


def sentence(title, lines, upgraded):
    text = audit.design_effect(title, lines)
    if not text:
        raise AssertionError('Missing design entry: ' + title)
    text = re.sub(r'^\d+(?:/\d+)?费\s*', '', text)
    text = re.sub(r'(\d+)/(\d+)', lambda m: m[2 if upgraded else 1], text)
    if title == '谦逊':
        if not text.endswith('消耗。）'):
            raise AssertionError('Humility parenthetical boundary changed')
        text = text[:-1]
    note = '升级后移除消耗。'
    if note in text:
        text = text.replace(note, '')
        if upgraded:
            text = text.replace('消耗。', '')
    text = re.sub(r'(\d+)费', lambda m: icons(m[1], '能量'), text)
    text = re.sub(r'(\d+)(?:点)?欲望', lambda m: icons(m[1], '欲望'), text)
    # User-confirmed inline resource representation, not a punctuation normalization.
    text = text.replace('花费欲望', '花费〈欲望〉')
    return text


class FinalTextContracts(unittest.TestCase):
    def test_twelve_scoped_oracles_match_design_including_semicolon(self):
        rows = expectations()
        self.assertEqual(len(rows), 12)
        self.assertEqual(len({row[0] for row in rows}), 12)
        loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        lines = read('DesignDoc.md').splitlines()
        for name, base, upgrade in rows:
            title = loc['MAIDEN_SUCCUBUS_CARD_' + audit.screaming_snake(name) + '.title']
            for upgraded, expected in ((False, base), (True, upgrade)):
                with self.subTest(card=name, upgraded=upgraded):
                    self.assertEqual(expected.replace('\n', ''), sentence(title, lines, upgraded))
                    self.assertNotIn('\n\n', expected)
                    self.assertNotIn('{', expected)

    def test_three_exact_localization_fixes_not_masked_by_oracle(self):
        loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        self.assertEqual(loc['MAIDEN_SUCCUBUS_CARD_BLASPHEMOUS_DESIRE.description'],
                         '本回合失去3点[gold]力量[/gold]。\n获得{DesireIcons}。')
        self.assertEqual(loc['MAIDEN_SUCCUBUS_CARD_DEEP_SEA_SLIME_CURSE.description'],
                         '打出后移除出牌组。\n失去1层[gold]魔装耐久[/gold]；')
        self.assertEqual(loc['MAIDEN_SUCCUBUS_CARD_TRANSPARENT_OUTFIT_CURSE.description'],
                         '当这张牌在你的[gold]手牌[/gold]中，获得20点[gold]诱惑度[/gold]。')

    def test_twenty_per_hand_card_and_actual_pile_movement_tests(self):
        code = read('src/Core/Temptation/Temptation.cs')
        self.assertIn('TransparentOutfitAmount = 20;', code)
        self.assertIn('.Count(card => card is TransparentOutfitCurse)', code)
        self.assertIn('* TransparentOutfitAmount', code)
        catalog = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs')
        self.assertIn('HandTemptation<TransparentOutfitCurse>(20);', catalog)
        for text in ('two hand modifiers stack', 'first departure leaves second modifier',
                     'all departures remove hand modifiers', 'temptation reapplied once on reentry'):
            self.assertIn(text, catalog)

    def test_game_contract_uses_real_run_and_combat_instances(self):
        code = read('src/Debugging/CardEffects/DesignSyncFinalTextContract.cs')
        for text in ('RunState.CreateCard(', 'CardCmd.Upgrade(outside)', 'PileType.Deck',
                     'PileType.Hand', 'final exact run text', 'final exact combat text'):
            self.assertIn(text, code)
        self.assertNotIn('.Trim(', code)
        self.assertNotIn('DynamicVars.', code)
        runner = read('src/Debugging/CardEffects/CardEffectTestRunner.cs')
        self.assertIn('DesignSyncFinalTextContract.Validate(context, card, scenario.Upgraded);', runner)
        self.assertIn('"ds27-final-text"', runner)


if __name__ == '__main__':
    unittest.main()
