"""Focused dual-X source contracts. Engine scenarios require a disposable run."""
import json
import unittest
from TestDesignSyncNeutral20260927 import read


class AllHopeLostContracts(unittest.TestCase):
    def test_current_design_axes_and_exact_template(self):
        self.assertIn('造成6Y点伤害X/X+1次。', read('DesignDoc.md'))
        loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        self.assertEqual('造成{InCombat:{Damage:diff()}|6Y}点伤害{InCombat:{Hits:diff()}|{IfUpgraded:show:X+1|X}}次。',
                         loc['MAIDEN_SUCCUBUS_CARD_ALL_HOPE_LOST.description'])

    def test_payment_ledger_replaces_single_use_cache(self):
        code = read('src/Cards/MvpDesireCards.cs').split('public sealed class AllHopeLost', 1)[1].split('public sealed class LegendaryMiner', 1)[0]
        self.assertIn('play.SecondaryResources().Value(DesireResource.Id)', code)
        self.assertIn('SecondaryResourcePaymentResolver.Plan(card).Lines', code)
        self.assertIn('.Sum(line => line.Value)', code)
        self.assertIn('ResolveEnergyXValue() + (IsUpgraded ? 1 : 0)', code)
        for stale in ('_desireSpent', 'AfterSecondaryResourceSpent', 'Desire.Get(card.Owner)'):
            self.assertNotIn(stale, code)

    def test_real_payment_replay_and_new_play_scenarios(self):
        code = read('src/Debugging/CardEffects/DesignSyncAllHopeLostContract.cs')
        for token in ('ctx.Player.RunState.CreateCard<AllHopeLost>', 'PileType.Deck', 'PileType.Hand',
                      'await played.SpendResources()', 'await played.OnPlayWrapper', 'CardCmd.Enchant<Glam>',
                      'new(0, 2, false, false)', 'new(3, 0, false, false)', 'DesirePaidWithHpPower',
                      'dual X later play recaptures resources', 'OfType<DamageReceivedEntry>()'):
            self.assertIn(token, code)
        self.assertNotIn('AfterSecondaryResourceSpent', code)
        self.assertIn('CustomVariants<AllHopeLost>(DesignSyncAllHopeLostContract.Run, 40)',
                      read('src/Debugging/CardEffects/CardEffectTestCatalog.cs'))


if __name__ == '__main__':
    unittest.main()
