"""Exact text and independent boundary cases. Native commands require a disposable game run."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model


class CalmMindContracts(unittest.TestCase):
    def test_design_full_sentence_and_exclusion_of_playing_card(self):
        design = re.sub(r'\s', '', read('DesignDoc.md'))
        self.assertIn('0费保留。如果你的手牌有6张或更多，则抽2/3张牌并获得2/3费。', design)
        self.assertIn('6张手牌在打出时计算，不包括正在结算的“心神宁静”本身。', design)

    def test_localization_preserves_then_punctuation_and_icons(self):
        cards = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        self.assertEqual('如果你的[gold]手牌[/gold]有6张或更多，则抽{Cards:diff()}张牌并获得{Energy:maidenEnergyIcons()}。',
                         cards['MAIDEN_SUCCUBUS_CARD_CALM_MIND.description'])

    def test_two_full_rendered_oracles_use_real_run_and_combat_instances(self):
        source = read('src/Debugging/CardEffects/DesignSyncCalmMindContract.cs')
        for token in ('保留。\\n如果你的手牌有6张或更多，则抽2张牌并获得〈能量〉〈能量〉。',
                      '保留。\\n如果你的手牌有6张或更多，则抽3张牌并获得〈能量〉〈能量〉〈能量〉。',
                      'RunState.CreateCard(', 'CardCmd.Upgrade(outside)',
                      'AssertText(ctx, outside, PileType.Deck, expected',
                      'AssertText(ctx, card, PileType.Hand, expected'):
            self.assertIn(token, source)

    def test_all_thirteen_independent_boundary_rows(self):
        source = read('src/Debugging/CardEffects/DesignSyncCalmMindContract.cs')
        rows = re.findall(r'new\((\d+), (\d+), (false|true), (\d+), (\d+), (\d+), (\d+)\)', source)
        self.assertEqual(len(rows), 13)
        actual = {(int(h), int(d), blocked): tuple(map(int, values)) for h, d, blocked, *values in rows}
        expected = {
            (0, 0, 'false'): (0, 0, 0, 0), (0, 1, 'false'): (0, 0, 0, 0), (0, 10, 'false'): (0, 0, 0, 0),
            (5, 0, 'false'): (0, 0, 0, 0), (5, 1, 'false'): (0, 0, 0, 0), (5, 10, 'false'): (0, 0, 0, 0),
            (6, 0, 'false'): (0, 0, 2, 3), (6, 1, 'false'): (1, 1, 2, 3), (6, 10, 'false'): (2, 3, 2, 3),
            (7, 0, 'false'): (0, 0, 2, 3), (7, 1, 'false'): (1, 1, 2, 3), (7, 10, 'false'): (2, 3, 2, 3),
            (6, 10, 'true'): (0, 0, 2, 3),
        }
        self.assertEqual(actual, expected)

    def test_game_cases_use_real_piles_and_play_not_direct_onplay(self):
        source = read('src/Debugging/CardEffects/DesignSyncCalmMindContract.cs')
        for token in ('await ctx.Reset()', 'await ctx.Add<CalmMind>(PileType.Hand, upgraded)',
                      'await ctx.ApplyPower<NoDrawPower>', 'await ctx.Play(source)',
                      'entry.OtherHand + 1', 'entry.OtherHand + drawn', 'energyBefore',
                      'held.All(c => c.Pile?.Type == PileType.Hand)',
                      'draw.Count(c => c.Pile?.Type == PileType.Hand)', 'PileType.Discard, source.Pile?.Type'):
            self.assertIn(token, source)
        self.assertNotIn('.OnPlay(', source)
        self.assertNotIn('DynamicVars.', source)
        self.assertIn('CustomVariants<CalmMind>(DesignSyncCalmMindContract.Run, 90)',
                      read('src/Debugging/CardEffects/CardEffectTestCatalog.cs'))

    def test_production_condition_and_values_are_unchanged(self):
        source = model('CalmMind')
        for token in ('CanonicalKeywords => [CardKeyword.Retain]', 'new CardsVar(2), new EnergyVar(2)',
                      'base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)',
                      'if (PileType.Hand.GetPile(Owner).Cards.Count < 6) return;',
                      'await CardPileCmd.Draw(', 'await PlayerCmd.GainEnergy(',
                      'DynamicVars.Cards.UpgradeValueBy(1)', 'DynamicVars.Energy.UpgradeValueBy(1)'):
            self.assertIn(token, source)


if __name__ == '__main__':
    unittest.main()
