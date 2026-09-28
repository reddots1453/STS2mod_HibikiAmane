"""Production ledger wiring and independent current-design text. Engine tests are separate."""
import json
import unittest
from TestDesignSync20260927 import read, card_class


class BurningDesireContracts(unittest.TestCase):
    def test_current_design_and_own_payment_are_explicit(self):
        doc = read('DesignDoc.md')
        self.assertIn('1费1欲望    造成3/4点伤害。本场战斗中每消耗1点欲望，重复1次。', doc)
        self.assertIn('瘴雷自身消耗的1欲望也进入计数，即正常情况下至少造成伤害2次', doc)
        code = card_class('src/Cards/CorruptCardsExpanded.cs', 'BurningDesire')
        for token in ('new DamageVar(3, ValueProp.Move)', 'Damage.UpgradeValueBy(1)',
                      'base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)',
                      'SecondaryCosts().Set(DesireResource.Id, 1)',
                      '1 + DesireCombatSpending.Get(Owner)', '.WithHitCount(hits)'):
            self.assertIn(token, code)
        self.assertNotIn('Data.Desire.Get', code)
        self.assertNotIn('SecondaryResourceHistory', code)

    def test_only_successful_spend_hook_records_not_change_events(self):
        code = read('src/Core/Desire/DesireResourceRules.cs')
        spent = code.split('public Task AfterSecondaryResourceSpent', 1)[1].split(
            'public decimal ModifyMaxSecondaryResource', 1)[0]
        self.assertIn('DesireCombatSpending.Record(context)', spent)
        self.assertEqual(code.count('DesireCombatSpending.Record(context)'), 1)
        service = read('src/Core/Desire/DesireCombatSpending.cs')
        for token in ('context.Definition.Id != DesireResource.Id', 'context.Amount <= 0',
                      'context.Player.PlayerCombatState == null',
                      'ReferenceEquals(context.Player.Creature.CombatState, context.CombatState)',
                      'Ledger.Record(context.CombatState, context.Player, context.Amount)'):
            self.assertIn(token, service)
        self.assertIn('new DesireResourceRules()', read('src/Core/Desire/DesireResource.cs'))

    def test_ledger_is_weak_combat_scoped_and_linked_to_executable_tests(self):
        code = read('src/Core/Desire/CombatSpendLedger.cs')
        for token in ('ConditionalWeakTable<TCombat, Session>', 'ConditionalWeakTable<TPlayer, Counter>',
                      'if (amount <= 0) return;', 'if (session.Closed) return;',
                      'session.Closed = true;', '(long)count.Amount + amount'):
            self.assertIn(token, code)
        self.assertNotIn('RunSavedData', code)
        self.assertIn('src/Core/Desire/CombatSpendLedger.cs', read('tests/DesignSyncContracts/DesignSyncContracts.csproj'))
        self.assertIn('new CombatSpendLedger<object, string>()', read('tests/DesignSyncContracts/Program.cs'))
        lifecycle = read('src/Core/Desire/DesirePersistenceCoordinator.cs')
        self.assertIn('DesireCombatSpending.Close(evt.CombatState)', lifecycle.split(
            'private static void OnCombatEnded', 1)[1])
        self.assertNotIn('DesireCombatSpending.Close', lifecycle.split(
            'private static void OnCombatEnded', 1)[0])

    def test_exact_localized_sentence_and_independent_full_text(self):
        loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        expected = ('造成{Damage:diff()}点伤害。\n'
                    '本场战斗中每[gold]消耗[/gold]{energyPrefix:maidenDesireIcons(1)}，重复1次。')
        actual = loc['MAIDEN_SUCCUBUS_CARD_BURNING_DESIRE.description']
        self.assertEqual(actual, expected)
        for mutation in (expected.replace('本场战斗中每', '每拥有'), expected.replace('，', '。'),
                         expected.replace('重复1次', '重复2次')):
            self.assertNotEqual(actual, mutation)
        test = read('src/Debugging/CardEffects/DesignSyncBurningDesireContract.cs')
        for literal in ('造成3点伤害。\\n本场战斗中每消耗〈欲望〉，重复1次。',
                        '造成4点伤害。\\n本场战斗中每消耗〈欲望〉，重复1次。',
                        'RunState.CreateCard', 'PileType.Deck', 'PileType.Hand',
                        'GetDescriptionForPile', 'UpdateDynamicVarPreview'):
            self.assertIn(literal, test)

    def test_actual_payment_and_actual_damage_not_mirrored_math(self):
        code = read('src/Debugging/CardEffects/DesignSyncBurningDesireContract.cs')
        for token in ('await played.SpendResources()', 'await ctx.Play(played, ctx.PrimaryEnemy)',
                      'ctx.AssertDamage', 'SecondaryResourceCmd.Spend', 'SecondaryResourceCmd.Lose',
                      'OfType<DamageReceivedEntry>()', 'distinct damage segments match repeat count',
                      'new([2, 3], 7, null, true, 7)', 'new([], 7, null, false, 1)',
                      'new([], 4, -1, true, 5)', 'new([], 7, 0, true, 1)',
                      'DesirePaidWithHpPower', 'HP payment actually committed',
                      'repeat creates no second payment', 'round and side transition',
                      'closed combat rejects late payment notifications'):
            self.assertIn(token, code)
        catalog = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs')
        self.assertIn('CustomVariants<BurningDesire>(DesignSyncBurningDesireContract.Run, 100)', catalog)
        self.assertNotIn('three hits at two current desire', catalog)

    def test_fixture_reset_is_debug_only_not_runtime_turn_or_refresh(self):
        code = read('src/Core/Desire/DesireCombatSpending.cs')
        self.assertIn('#if DEBUG\n    internal static void ResetForTests', code)
        self.assertIn('DesireCombatSpending.ResetForTests(Combat)',
                      read('src/Debugging/CardEffects/CardEffectTestContext.cs'))
        for path in ('src/Data/Desire.cs', 'src/Core/Desire/DesirePersistenceCoordinator.cs'):
            self.assertNotIn('ResetForTests', read(path))


if __name__ == '__main__':
    unittest.main()
