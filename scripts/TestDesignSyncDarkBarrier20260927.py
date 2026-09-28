"""Dark barrier uses current design and native Colossus timing; engine execution is separate."""
import json
import unittest
from TestDesignSync20260927 import read, card_class


class DarkBarrierContracts(unittest.TestCase):
    def test_cost_base_block_and_overdraft_extend_duration(self):
        code = card_class('src/Cards/Iteration1CorruptCards.cs', 'DarkFlameBarrier')
        for token in ('base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)',
                      'SecondaryCosts().Set(DesireResource.Id, 1)', 'new BlockVar(6, ValueProp.Move)',
                      'new DynamicVar("Turns", 1)', 'CardKeyword.Exhaust', 'DynamicVars.Block.UpgradeValueBy(3)'):
            self.assertIn(token, code)
        self.assertEqual(code.count('CreatureCmd.GainBlock'), 1)
        self.assertLess(code.index('CreatureCmd.GainBlock'), code.index('PowerCmd.Apply<DarkFlameBarrierPower>'))
        self.assertLess(code.index('PowerCmd.Apply<DarkFlameBarrierPower>'), code.index('TransformationCmd.PayOverdraft'))
        tail = code.split('if (await TransformationCmd.PayOverdraft', 1)[1]
        self.assertIn('PowerCmd.Apply<DarkFlameBarrierPower>(context, Owner.Creature, 1', tail)
        self.assertNotIn('GainBlock', tail)

    def test_damage_and_lifetime_match_native_reference_boundaries(self):
        code = card_class('src/Powers/Iteration1CorruptPowers.cs', 'DarkFlameBarrierPower')
        for token in ('IsMutable && Amount > 0 && Owner.IsAlive && Owner.Powers.Contains(this)',
                      'target == Owner && props.IsPoweredAttack()', 'dealer.Side != Owner.Side',
                      'dealer.HasPower<BurningPower>()', '? 0.5m', ': 1m',
                      'AfterSideTurnEnd', 'IsActive && side == CombatSide.Enemy', 'PowerCmd.Decrement(this)'):
            self.assertIn(token, code)
        self.assertNotIn('AfterPlayerTurnStart', code)

    def test_real_payment_block_damage_and_expiry_are_all_observed(self):
        code = read('src/Debugging/CardEffects/DesignSyncDarkBarrierContract.cs')
        for token in ('await card.SpendResources()', 'ctx.Player.PlayerCombatState!.Energy',
                      '1, Data.Desire.Get(ctx.Player)', 'await ctx.Play(card, selectedIndices:',
                      'ctx.Self.Block', 'ctx.AssertPower("barrier duration',
                      'CreatureCmd.Damage(context, ctx.Self, 10, ValueProp.Move',
                      'CreatureCmd.Damage(context, ctx.Self, 10, ValueProp.Unpowered',
                      'burning attack halves actual damage', 'nonburning attack unaffected',
                      'friendly source unaffected', 'missing source unaffected',
                      'AfterPlayerTurnStart(context, ctx.Player)', 'AfterSideTurnEnd(context, CombatSide.Enemy',
                      'expired reference cannot reduce damage', 'repeated casts add duration not mitigation'):
            self.assertIn(token, code)
        self.assertIn('CustomVariants<DarkFlameBarrier>(DesignSyncDarkBarrierContract.Run, 100)',
                      read('src/Debugging/CardEffects/CardEffectTestCatalog.cs'))

    def test_seven_resource_cases_cover_zero_decline_and_precedence(self):
        code = read('src/Debugging/CardEffects/DesignSyncDarkBarrierContract.cs')
        for row in ('(-1, 0, false, 1, 0, 0)', '(0, 0, false, 1, 0, 0)',
                    '(1, 0, true, 2, 0, 0)', '(3, 0, false, 1, 3, 0)',
                    '(-1, 1, true, 2, 0, 0)', '(-1, 2, true, 2, 0, 1)', '(3, 1, true, 2, 3, 0)'):
            self.assertIn(row, code)

    def test_power_hover_displays_remaining_turns_and_unchanged_identity(self):
        loc = json.loads(read('MaidenSuccubus/localization/zhs/powers.json'))
        key = 'MAIDEN_SUCCUBUS_POWER_DARK_FLAME_BARRIER_POWER.'
        self.assertEqual(loc[key + 'title'], '暗焰壁障')
        expected = '持续{Amount}回合，[gold]燃烧[/gold]的敌人对你造成的攻击伤害降低50%。'
        self.assertEqual(loc[key + 'description'],
                         '[gold]燃烧[/gold]的敌人对你造成的攻击伤害降低50%。每个敌方回合结束时减少1层。')
        self.assertNotIn('{Amount}', loc[key + 'description'])
        self.assertEqual(loc[key + 'smartDescription'], expected)


if __name__ == '__main__':
    unittest.main()
