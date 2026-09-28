"""Targeted source/asset contracts; does not claim execution in the game engine."""
import hashlib
import json
import pathlib
import unittest
from TestDesignSyncNeutral20260927 import read

ROOT = pathlib.Path(__file__).resolve().parents[1]


class RandomRelics(unittest.TestCase):
    def test_pool_is_flat_unique_stable_and_combat_healing_is_excluded(self):
        code = read('src/Core/Rewards/UnifiedRouteCardPool.cs')
        for token in ('MSNeutralCardPool', 'MSHolyCardPool', 'MSCorruptCardPool',
                      'SelectMany', 'DistinctBy(card => card.Id)', 'StringComparer.Ordinal',
                      'GetUnlockedCards', 'CardMultiplayerConstraint', 'card is not HealingArt',
                      'player.Character is not MaidenSuccubusCharacter'):
            self.assertIn(token, code)
        self.assertNotIn('Corruption', code)
        self.assertNotIn('NextItem', code)

    def test_tony_uses_native_removal_reward_rng_upgrade_and_saved_receipt(self):
        code = read('src/Relics/TonysCharm.cs')
        for token in ('RelicRarity.Shop', '[SavedProperty]', 'BeforeCardRemoved',
                      'FromDeckForRemoval', 'selected.Distinct().Take(2)', 'card.IsRemovable',
                      'Owner.Relics.Contains(this)', 'card.Owner != Owner', 'PileType.Deck',
                      'rareOnly: true', 'Owner.PlayerRng.Rewards.NextItem(candidates)',
                      'CardCmd.Upgrade(reward)', 'finally { _resolving.Remove(card); }'):
            self.assertIn(token, code)
        self.assertLess(code.index('PickupEffectGranted = true'), code.index('await CardSelectCmd'))
        self.assertLess(code.index('CardCmd.Upgrade(reward)'), code.index('CardPileCmd.Add(reward'))

    def test_branch_uses_native_generation_and_end_guard(self):
        code = read('src/Relics/MvpRelics.cs').split('public sealed class WitheredTreeSoul', 1)[1].split('public sealed class Vibrator', 1)[0]
        for token in ('AfterCardExhausted', 'card.Owner != Owner', 'IsOverOrEnding',
                      'UnifiedRouteCardPool.Get(Owner)', 'CardFactory.GetDistinctForCombat',
                      'Rng.CombatCardGeneration', 'AddGeneratedCardToCombat(generated, PileType.Hand, Owner)',
                      'RuntimeTextureAssets.PrepareResource', 'dead_branch_outline.png'):
            self.assertIn(token, code)
        self.assertNotIn('Owner.Character.CardPool', code)

    def test_exact_sts1_icon_bytes_and_png_format(self):
        expected = {
            'dead_branch.png': 'bebf6f98561129085e5bf97e3ac5be5e4c06765e77fe94d6b982b61401a20c60',
            'dead_branch_outline.png': '4cb289ac775c62d00a7b33f6a9e3804071909d43ca7153dff0867cadc868183e',
        }
        for filename, digest in expected.items():
            data = (ROOT / 'MaidenSuccubus/images/relics/sts1' / filename).read_bytes()
            self.assertEqual(data[:8], b'\x89PNG\r\n\x1a\n')
            self.assertEqual(hashlib.sha256(data).hexdigest(), digest)

    def test_registration_and_localization(self):
        self.assertIn('"TonysCharm"', read('docs/content_contract_20260824.json'))
        loc = json.loads(read('MaidenSuccubus/localization/zhs/relics.json'))
        self.assertEqual(loc['MAIDEN_SUCCUBUS_RELIC_TONYS_CHARM.description'],
                         '当你移除一张牌时，将1张升级过的随机稀有牌加入牌组。拾起时，移除2张牌。')

    def test_engine_test_uses_real_hooks_history_and_explicit_consent(self):
        code = read('src/ConsoleCommands/DesignRandomRelicTestConsoleCmd.cs')
        for token in ('args[0] != "confirm"', 'RunState.Players.Count != 1',
                      'new[] { 0, 1, 4 }', 'RelicCmd.Obtain(relic, player)',
                      'CardPileCmd.RemoveFromDeck(originals[2]', 'SavedProperties.From(relic)',
                      'new[] { 0, 10 }', 'CardCmd.Exhaust(choice, source)',
                      'OfType<CardGeneratedEntry>()', 'PileType.Discard : PileType.Hand',
                      'player.Deck.Cards.SequenceEqual(deckBefore)', 'TestMode.IsOn = previousTestMode'):
            self.assertIn(token, code)


if __name__ == '__main__':
    unittest.main()
