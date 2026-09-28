"""Independent literal-oracle/design checks, not a native SmartFormat renderer."""
import json
import re
import unittest
import AuditCardLocalization as audit
from TestDesignSyncNeutral20260927 import read


def expectations():
    source = read('src/Debugging/CardEffects/DesignSyncHolyTextContract.cs')
    literal = r'"(?:[^"\\]|\\.)*"'
    return [(name, json.loads(base), json.loads(upgrade)) for name, base, upgrade in
            re.findall(r'new\(typeof\((\w+)\), (' + literal + r'), (' + literal + r')\)', source)]


def icons(amount, kind):
    amount = int(amount)
    return f'〈{kind}〉' * amount if 0 < amount < 4 else str(amount) + f'〈{kind}〉'


def design_sentence(title, lines, upgraded):
    text = audit.design_effect(title, lines)
    if not text:
        raise AssertionError('No design sentence: ' + title)
    text = re.sub(r'^(?:\d+(?:/\d+)?|X)费\s*', '', text).replace('*', '')
    text = re.sub(r'(\d+)%/(\d+)%', lambda m: m[2 if upgraded else 1] + '%', text)
    text = re.sub(r'(\d+)/(\d+)', lambda m: m[2 if upgraded else 1], text)
    text = text.replace('（升级：升级过的）', '升级过的' if upgraded else '')
    if title == '万劫不复':
        text = text.replace('\n（关键字 沉底：战斗开始时，将这张牌放入弃牌堆。）', '')
    if title == '冰镜反射':
        text = text.replace('冰雾/冰雾+', '冰雾+' if upgraded else '冰雾')
    for keyword in ('保留', '固有'):
        annotation = '升级后获得' + keyword + '。'
        if annotation in text:
            text = text.replace(annotation, '')
            if upgraded:
                text = keyword + '。' + text
    for keyword in ('消耗', '虚无'):
        for annotation in ('升级移除' + keyword + '。', '升级后移除' + keyword + '。'):
            if annotation in text:
                text = text.replace(annotation, '')
                if upgraded:
                    text = text.replace(keyword + '。', '')
    if title == '福音':
        text = text.replace('升级后变化为守护圣言+和惩戒圣言+。', '')
        if upgraded:
            text = text.replace('守护圣言', '守护圣言+').replace('惩戒圣言', '惩戒圣言+')
    # Native front keywords move to the front; punctuation and all other words remain exact.
    for keyword in ('保留', '固有', '虚无'):
        if keyword + '。' in text:
            text = keyword + '。' + text.replace(keyword + '。', '')
    # Previously confirmed UI conventions override the prose spelling of resources/enchantments.
    text = re.sub(r'(\d+)(?:点)?欲望', lambda m: icons(m[1], '欲望'), text)
    text = re.sub(r'(\d+)费', lambda m: icons(m[1], '能量'), text)
    text = text.replace('耗能增加1点', '耗能增加〈能量〉')
    text = text.replace('附魔“灵魂联结”', '附魔：灵魂联结')
    return text


class HolyFullTextContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows = expectations()
        cls.loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        cls.lines = read('DesignDoc.md').splitlines()

    def test_exact_44_holy_models_and_88_literal_variants(self):
        self.assertEqual(len(self.rows), 44)
        self.assertEqual(len({name for name, _, _ in self.rows}), 44)
        pool = json.loads(read('docs/content_contract_20260824.json'))['cards']['MSHolyCardPool']
        for name, base, upgrade in self.rows:
            self.assertIn(name, pool)
            for text in (base, upgrade):
                self.assertNotIn('{', text)
                self.assertNotIn('\n\n', text)
                self.assertTrue(text.endswith('。'))

    def test_all_88_sentences_match_design_without_erasing_punctuation(self):
        for name, base, upgrade in self.rows:
            title = self.loc['MAIDEN_SUCCUBUS_CARD_' + audit.screaming_snake(name) + '.title']
            for upgraded, expected in ((False, base), (True, upgrade)):
                with self.subTest(card=name, upgraded=upgraded):
                    self.assertEqual(expected.replace('\n', ''), design_sentence(title, self.lines, upgraded))

    def test_seven_production_corrections_and_no_lost_icon_bindings(self):
        loc = self.loc
        def description(name):
            return loc['MAIDEN_SUCCUBUS_CARD_' + name + '.description']
        self.assertTrue(description('MULTIPLE_REPRODUCTION').startswith('在下个回合结束后，获得1个额外的回合。\n'))
        self.assertTrue(description('HOLY_PUNISHMENT').endswith('[gold]魔力解放[/gold]：并抽取它。'))
        self.assertEqual(description('TRANQUILIZER'), '失去{DesireIcons}。\n抽1张牌。')
        self.assertTrue(description('NO_LEWDNESS').startswith('获得{Energy:maidenEnergyIcons()}。\n抽{Cards:diff()}张牌。\n'))
        self.assertTrue(description('DEVOUT_BULWARK').startswith('获得{Block:diff()}[gold]格挡[/gold]。'))
        self.assertIn('失去{Temptation}[gold]诱惑度[/gold]。', description('PURIFICATION_ORB'))
        self.assertIn('失去{DesireIcons}。', description('PURIFICATION_ORB'))
        self.assertIn('为这三张牌[gold]附魔[/gold]：[purple]灵魂联结[/purple]。', description('YARUS_MEMORY'))

    def test_both_actual_pile_instances_and_existing_effects_are_retained(self):
        source = read('src/Debugging/CardEffects/DesignSyncHolyTextContract.cs')
        for token in ('RunState.CreateCard(', 'CardCmd.Upgrade(outside)', '(outside, PileType.Deck)',
                      '(card, PileType.Hand)', 'instance.UpdateDynamicVarPreview(',
                      'instance.GetDescriptionForPile(pile, ctx.PrimaryEnemy)', 'Normalize(actual), effect: false'):
            self.assertIn(token, source)
        self.assertNotIn('.Trim(', source)
        runner = read('src/Debugging/CardEffects/CardEffectTestRunner.cs')
        for token in ('DesignSyncHolyTextContract.Validate(context, card, scenario.Upgraded);',
                      '"ds27-holy-text"', 'batch.Length != 44 || batch.Length != DesignSyncHolyTextContract.Entries.Length',
                      'await scenario.Execute(context, card);', 'scenarioResult.EffectAssertionCount < scenario.MinimumEffectAssertions'):
            self.assertIn(token, runner)

    def test_two_resource_types_have_separate_exact_paths_and_counts(self):
        source = read('src/Debugging/CardEffects/DesignSyncHolyTextContract.cs')
        self.assertIn('"[img]" + MaidenDesireIconAssets.TextIconResourcePath + "[/img]"', source)
        self.assertIn('DesignSyncNeutralTextContract.Normalize(text.Replace(icon, "〈欲望〉", StringComparison.Ordinal))', source)
        rows = {name: (base, upgrade) for name, base, upgrade in self.rows}
        self.assertEqual(rows['Tranquilizer'][0].count('〈欲望〉'), 2)
        self.assertEqual(rows['Tranquilizer'][1].count('〈欲望〉'), 3)
        self.assertIn('4〈能量〉。\n抽4张牌。', rows['NoLewdness'][1])
        self.assertIn('〈欲望〉，耗能增加〈能量〉', rows['FocusedSlash'][0])
        self.assertEqual(rows['PhotonVolt'][0].count('〈欲望〉'), 2)

    def test_upgrade_keywords_and_generated_names_are_explicit(self):
        rows = {name: (base, upgrade) for name, base, upgrade in self.rows}
        for name in ('Consecration', 'MemoryImprint'):
            self.assertFalse(rows[name][0].startswith('固有。'))
            self.assertTrue(rows[name][1].startswith('固有。\n'))
        self.assertTrue(rows['TerminalSanctuary'][0].startswith('虚无。\n'))
        self.assertNotIn('虚无', rows['TerminalSanctuary'][1])
        self.assertNotIn('消耗', rows['HolyCurse'][1])
        self.assertNotIn('保留', ''.join(rows['FinalJudgment']))
        self.assertTrue(rows['PurificationOrb'][0].endswith('\n消耗。\n随身。'))
        self.assertIn('冰雾+', rows['MomentaryGrace'][1])
        self.assertIn('惩戒圣言+', rows['Gospel'][1])

    def test_mutated_punctuation_and_values_fail_design_oracle(self):
        for name, base, _ in self.rows:
            title = self.loc['MAIDEN_SUCCUBUS_CARD_' + audit.screaming_snake(name) + '.title']
            design = design_sentence(title, self.lines, False)
            self.assertNotEqual(base.replace('\n', '').replace('。', '，', 1), design)
            if re.search(r'\d', base):
                mutant = re.sub(r'\d+', lambda m: str(int(m[0]) + 1), base, count=1)
                self.assertNotEqual(mutant.replace('\n', ''), design)


if __name__ == '__main__':
    unittest.main()
