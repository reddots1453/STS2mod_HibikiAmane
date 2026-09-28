"""Q12/Q13/Q19 targeted wiring checks; game scenarios are separately compiled."""
import json
import unittest
from TestDesignSyncNeutral20260927 import read


class ConfirmedStacks(unittest.TestCase):
    def test_chain_instances_match_orbit_and_queue_consumes_one(self):
        code = read('src/Powers/MvpExhaustPowers2.cs')
        chain = code.split('public sealed class ChainDestructionPower', 1)[1].split('public sealed class ChainDestructionReplayPower', 1)[0]
        self.assertIn('PowerInstanceType.Instanced', chain)
        replay = code.split('public sealed class ChainDestructionReplayPower', 1)[1].split('public sealed class CurseCorridorPower', 1)[0]
        self.assertIn('playCount + 1', replay)
        self.assertIn('await PowerCmd.Decrement(this)', replay)
        self.assertNotIn('playCount + Amount', replay)

    def test_lullaby_generation_finishes_before_final_hand_count(self):
        code = read('src/Powers/MvpNeutralUtilityPowers.cs').split('public sealed class LullabyPower', 1)[1].split('public sealed class TenaciousResistancePower', 1)[0]
        for token in ('int layers = (int)Amount;', 'i < layers', 'handSize * layers * 2'):
            self.assertIn(token, code)
        self.assertLess(code.index('AddGeneratedCardToCombat'), code.index('int handSize'))
        self.assertIn('PowerCmd.Apply<LullabyPower>(context, Owner.Creature, 1,', read('src/Cards/MvpNeutralCardsBatch2.cs'))
        loc = json.loads(read('MaidenSuccubus/localization/zhs/powers.json'))
        self.assertIn('{Amount}张', loc['MAIDEN_SUCCUBUS_POWER_LULLABY_POWER.smartDescription'])
        self.assertIn('{Amount}张牌', loc['MAIDEN_SUCCUBUS_POWER_CHAIN_DESTRUCTION_REPLAY_POWER.smartDescription'])

    def test_native_purification_timing_is_preserved(self):
        code = read('src/Powers/MvpHolyUtilityPowers.cs').split('public sealed class SoulPurificationPower', 1)[1].split('public sealed class MemoryImprintPower', 1)[0]
        self.assertIn('ModifyCardPlayResultLocation', code)
        self.assertIn('AfterCardPlayed', code)
        self.assertNotIn('CardCmd.Exhaust', code)

    def test_engine_scenarios_use_real_hooks_history_and_separate_instances(self):
        code = read('src/Debugging/CardEffects/DesignSyncConfirmedStackContract.cs')
        for token in ('instances.Length', 'queue buffs successive cards', 'Hook.BeforeFlush(ctx.Combat, ctx.Player)',
                      'new[] { 1, 2, 3 }', 'events.Take(exhaustedAt)', 'CardCmd.Enchant<Glam>',
                      'OfType<CardExhaustedEntry>()', 'OfType<CardDrawnEntry>()'):
            self.assertIn(token, code)
        catalog = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs')
        for method in ('Chain', 'Lullaby', 'Purification'):
            self.assertIn('DesignSyncConfirmedStackContract.' + method, catalog)


if __name__ == '__main__':
    unittest.main()
