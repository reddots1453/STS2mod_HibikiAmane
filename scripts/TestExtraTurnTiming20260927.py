"""Extra-turn source contracts; native runtime counterpart is separately compiled."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding='utf-8-sig')


class ExtraTurnTimingTests(unittest.TestCase):
    def setUp(self):
        self.power = read('src/Powers/MvpTurnPowers.cs')
        self.contract = read('src/Debugging/CardEffects/DesignSyncExtraTurnContract.cs')

    def test_query_is_read_only(self):
        query = self.power.split('public override bool ShouldTakeExtraTurn')[1].split(';')[0]
        self.assertIn('=>', query)
        self.assertIn('IsActive && player.Creature == Owner && !DelayOneTurn', query)
        self.assertNotRegex(query, r'DelayOneTurn\s*=|\+\+|--')

    def test_maturity_is_owner_turn_start_not_query(self):
        start = self.power.split('public override Task AfterPlayerTurnStart')[1].split('return Task.CompletedTask')[0]
        self.assertIn('player.Creature == Owner', start)
        self.assertIn('TurnNumber > DelayAppliedOnTurn', start)
        self.assertIn('DelayOneTurn = false', start)

    def test_consume_only_mature_owned_power(self):
        after = self.power.split('public override Task AfterTakingExtraTurn')[1]
        self.assertIn('if (IsActive && player.Creature == Owner && !DelayOneTurn)', after)
        self.assertIn('return PowerCmd.Remove(this)', after)

    def test_schedule_preserves_legacy_save_field(self):
        self.assertRegex(self.power, r'\[SavedProperty\]\s*public bool DelayOneTurn')
        self.assertRegex(self.power, r'\[SavedProperty\]\s*public int DelayAppliedOnTurn.*= -1;')
        self.assertIn('DelayAppliedOnTurn = Owner.Player!.PlayerCombatState!.TurnNumber', self.power)
        self.assertIn('power.Schedule(!await OverdraftCmd.Offer(context, this, 1))', read('src/Cards/MvpHolyCardsBatch3.cs'))
        self.assertIn('PowerStackType.Single', self.power)

    def test_stale_and_canonical_reference_guards(self):
        self.assertIn('IsMutable && Amount > 0 && Owner.IsAlive', self.power)
        self.assertIn('Owner.CombatState != null && Owner.Powers.Contains(this)', self.power)

    def test_real_commands_and_broadcast_in_contract(self):
        for fragment in ('await ctx.Play(card)', 'ctx.ApplyPower<AmbergrisPower>',
                         'Hook.ShouldTakeExtraTurn(ctx.Combat, ctx.Player)',
                         'Hook.AfterTakingExtraTurn(ctx.Combat, ctx.Player)',
                         'IncrementTurnNumber()', 'selectedIndices: [0]',
                         'legacy absent turn stamp', 'same-turn start callback',
                         'foreign callbacks preserve pending grant', 'canonical power is inert'):
            self.assertIn(fragment, self.contract)

    def test_runtime_test_registration_and_cleanup(self):
        self.assertIn('CustomVariants<MultipleReproduction>(DesignSyncExtraTurnContract.Run, 40)',
                      read('src/Debugging/CardEffects/CardEffectTestCatalog.cs'))
        self.assertIn('finally { delayed.PowerExtraIconAmountLabelsInvalidated -= Changed; }', self.contract)
        self.assertIn('ctx.Create<MultipleReproduction>(upgraded)', self.contract)


if __name__ == '__main__':
    unittest.main()
