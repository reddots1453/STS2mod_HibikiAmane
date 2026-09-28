"""Five design-derived full-text contracts, including both starter variations."""
import json
import re
import subprocess
import unittest
from TestDesignSync20260927 import read


def rows(form):
    source = read('src/Debugging/CardEffects/DesignSyncVariationTextContract.cs')
    block = source.split(form + ' =', 1)[1].split('];', 1)[0]
    literal = r'"(?:[^"\\]|\\.)*"'
    return {name: (json.loads(base), json.loads(upgrade)) for name, base, upgrade in
            re.findall(r'new\(typeof\((\w+)\), (' + literal + r'), (' + literal + r')\)', block)}


class VariationTextContracts(unittest.TestCase):
    def test_full_design_sentences_not_generated_from_localization(self):
        lines = read('DesignDoc.md').splitlines()
        entries = rows('Entries')
        holy = rows('HolyEntries')
        names = {'DarkElement': '黑暗元素', 'DarkOrigin': '黑暗之源',
                 'LastStand': '背水一战', 'LegendaryMiner': '传奇矿工', 'RecollectionRoom': '回想房'}
        self.assertEqual(set(entries), set(names))
        self.assertEqual(set(holy), {'DarkElement', 'DarkOrigin'})
        for model, title in names.items():
            index = next(i for i, line in enumerate(lines) if line.strip() == title)
            for alternate, row in [(False, entries[model])] + ([(True, holy[model])] if model in holy else []):
                raw = lines[index + (6 if alternate else 2)].strip()
                self.assertRegex(raw, r'^\d+费\s+')
                for upgrade, expected in enumerate(row):
                    text = re.sub(r'^\d+费\s+', '', raw)
                    text = re.sub(r'(\d+)/(\d+)', lambda m: m[upgrade + 1], text)
                    if model == 'LastStand':
                        self.assertTrue(text.endswith('（造成？点伤害）'))
                        text = text.removesuffix('（造成？点伤害）')
                    if model == 'LegendaryMiner':
                        text = text.replace('1点欲望', '〈欲望〉')
                    if model == 'RecollectionRoom':
                        self.assertTrue(text.endswith('升级后获得固有。'))
                        text = ('固有。' if upgrade else '') + text.removesuffix('升级后获得固有。')
                    text = text.replace('。', '。\n').rstrip('\n')
                    self.assertEqual(text, expected, (model, alternate, upgrade))
                    self.assertNotEqual(text.replace('。', '，', 1), expected)

    def test_same_live_instances_cross_boundary_and_return(self):
        code = read('src/Debugging/CardEffects/DesignSyncVariationTextContract.cs')
        for token in ('(-5, true)', '(-3, true)', '(-2, false)', '(0, false)', '(2, false)',
                      '(3, false)', '(5, false)', 'CorruptionCmd.Set(run, test.Value)',
                      'finally { CorruptionCmd.Set(run, saved); }',
                      'RouteCardQuery.Get(instance)', 'instance.GainsBlock',
                      '!CombatSealQuery.IsSealed(run, instance)', 'RunState.CreateCard',
                      'PileType.Deck', 'PileType.Hand', 'GetDescriptionForPile'):
            self.assertIn(token, code)
        self.assertEqual(code.count('(-3, true)'), 2)
        self.assertEqual(code.count('(-5, true)'), 2)
        self.assertEqual(code.count('(-2, false)'), 2)

    def test_exact_metadata_keywords_and_global_selection(self):
        code = read('src/Debugging/CardEffects/DesignSyncVariationTextContract.cs')
        for token in ('CardRarity.Basic', 'CardRarity.Ancient', 'CardRarity.Common',
                      'CardRarity.Uncommon', 'CardRarity.Rare', 'card.Pool is MSCorruptCardPool',
                      'card.Keywords.SetEquals(card is RecollectionRoom && upgraded ? [CardKeyword.Innate] : [])'):
            self.assertIn(token, code)
        runner = read('src/Debugging/CardEffects/CardEffectTestRunner.cs')
        self.assertIn('DesignSyncVariationTextContract.Validate(context, card, scenario.Upgraded);', runner)
        self.assertIn('"ds27-variation-text"', runner)
        self.assertIn('batch.Length != 5 || batch.Length != DesignSyncVariationTextContract.Entries.Length', runner)

    def test_last_stand_single_conditional_newline_and_dynamic_full_assertion(self):
        loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        text = loc['MAIDEN_SUCCUBUS_CARD_LAST_STAND.description']
        self.assertEqual(text, '造成{CalculationBase:diff()}点伤害。\n每有1层负面状态，额外造成'
                         '{ExtraDamage:diff()}点伤害。{InCombat:\n（造成{CalculatedDamage:diff()}点伤害）|}')
        catalog = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs')
        self.assertIn('DesignSyncVariationTextContract.LastStandTotal(ctx, card, upgraded, upgraded ? 16 : 12);', catalog)
        signed = read('src/Debugging/CardEffects/DesignSyncSignedLayerContract.cs')
        own = signed.split('internal static async Task LastStand(', 1)[1].split('internal static async Task Draw(', 1)[0]
        self.assertIn('DesignSyncVariationTextContract.LastStandTotal(ctx, card, upgraded, upgraded ? 22 : 16);', own)
        self.assertNotIn('DesignSyncVariationTextContract.LastStandTotal', signed.split(
            'internal static async Task LastStand(', 1)[0])

    def test_production_formatter_does_not_restore_empty_line(self):
        source = read('scripts/FormatLocalizationStyle.ps1')
        pattern = next(line.strip() for line in source.splitlines() if '$sentencePattern =' in line)
        replacement = next(line.strip() for line in source.splitlines() if '$sentencePattern, $fullStop' in line)
        value = json.dumps(json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))[
            'MAIDEN_SUCCUBUS_CARD_LAST_STAND.description'], ensure_ascii=False)[1:-1]
        command = ("$ErrorActionPreference='Stop'; $fullStop=[string][char]0x3002; "
                   "$value=[Console]::In.ReadToEnd(); $expected=$value; " + pattern + '; ' + replacement +
                   "; if($value -cne $expected){throw 'formatter changed conditional newline'}; 'PASS'")
        result = subprocess.run(['pwsh', '-NoProfile', '-Command', command],
                                input=value, capture_output=True, text=True, timeout=30)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn('PASS', result.stdout)

    def test_miner_uses_actual_payment_not_direct_hook_call(self):
        code = read('src/Debugging/CardEffects/DesignSyncLegendaryMinerContract.cs')
        for token in ('SecondaryResourceCmd.Gain', 'SecondaryResourceCmd.Lose', 'SecondaryResourceCmd.Spend',
                      'insufficient payment rejected', 'ordinary loss gives no block',
                      'actual spending triggers once not twice', 'two miner cards stack amounts',
                      'removed listener no longer grants block', 'ctx.Self.Block', 'Data.Desire.Get(ctx.Player)'):
            self.assertIn(token, code)
        self.assertNotIn('AfterSecondaryResourceSpent(', code)
        self.assertIn('CustomVariants<LegendaryMiner>(DesignSyncLegendaryMinerContract.Run, 14)',
                      read('src/Debugging/CardEffects/CardEffectTestCatalog.cs'))
        catalog = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs')
        for token in ('origin released damage', 'holy variation magic release grants amplified block',
                      'damage with three self-debuff layers', 'manual draw leaves exhaust alone',
                      'recovery respects ten-card hand limit'):
            self.assertIn(token, catalog)


if __name__ == '__main__':
    unittest.main()
