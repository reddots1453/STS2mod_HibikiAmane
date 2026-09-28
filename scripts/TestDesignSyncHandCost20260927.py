"""Independent cost/text expectations; engine pile-transition probes are compiled, not run here."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model


class HandCostContracts(unittest.TestCase):
    def test_approved_design_own_cost_and_surcharge(self):
        design = re.sub(r'\s', '', read('DesignDoc.md'))
        self.assertIn('口球1费如果这张牌在你的手牌中，你的技能牌额外耗能1点。', design)

    def test_cost_is_one_and_existing_identity_is_preserved(self):
        source = model('GagCurse')
        self.assertIn('class GagCurse : MSEventCurseTemplate', source)
        self.assertIn('public GagCurse() : base(1)', source)
        base = model('MSEventCurseTemplate')
        self.assertIn('MaxUpgradeLevel => 0', base)
        self.assertIn('CardType.Curse, CardRarity.Curse, TargetType.None', base)
        self.assertIn('ModelDb.CardPool<MSGeneratedCardPool>()', base)

    def test_effect_remains_owner_hand_and_skill_scoped(self):
        source = model('GagCurse')
        for token in ('card.Owner != Owner', 'Pile?.Type != PileType.Hand',
                      'card.Type != CardType.Skill', 'modifiedCost = originalCost;',
                      'modifiedCost = originalCost + 1;', 'return false;', 'return true;'):
            self.assertIn(token, source)

    def test_complete_localized_text_preserves_icon_and_punctuation(self):
        cards = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        self.assertEqual('口球', cards['MAIDEN_SUCCUBUS_CARD_GAG_CURSE.title'])
        self.assertEqual('如果这张牌在你的[gold]手牌[/gold]中，你的技能牌额外耗能{energyPrefix:maidenEnergyIcons(1)}。',
                         cards['MAIDEN_SUCCUBUS_CARD_GAG_CURSE.description'])

    def test_real_model_contract_covers_exit_reentry_and_non_skill(self):
        catalog = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs')
        self.assertIn('HandCostRestriction<GagCurse>(CardType.Skill, 1, expectedOwnCost: 1)', catalog)
        body = catalog.split('private static void HandCostRestriction<T>', 1)[1].split('private static void HandPlayRestriction<T>', 1)[0]
        for token in ('card.EnergyCost.Canonical', 'card.MaxUpgradeLevel', 'card.TargetType',
                      'ctx.Add<DefendIronclad>', 'ctx.Add<StrikeIronclad>',
                      'fixture.EnergyCost.GetWithModifiers(CostModifiers.All)',
                      'card.EnergyCost.GetWithModifiers(CostModifiers.All)',
                      'PileType.Draw, PileType.Discard, PileType.Exhaust',
                      '!card.TryModifyEnergyCostInCombat(fixture, 1, out decimal unchanged)',
                      '1m, unchanged', 'CardPileCmd.Add(card, pile, skipVisuals: true)',
                      'return from {pile} restores surcharge once',
                      '如果这张牌在你的手牌中，你的技能牌额外耗能〈能量〉。', '}, 21)'):
            self.assertIn(token, body)


if __name__ == '__main__':
    unittest.main()
