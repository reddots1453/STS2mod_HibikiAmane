"""Native classification/visibility are preserved; signed magnitude is tested offline."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding='utf-8-sig')


class SignedPowerLayerTests(unittest.TestCase):
    def test_query_preserves_native_classification_and_visibility(self):
        source = read('src/Core/Powers/PowerLayerQuery.cs')
        self.assertIn('power.IsVisible', source)
        self.assertIn('power.TypeForCurrentAmount == type', source)
        self.assertNotIn('power.Amount > 0', source)
        self.assertIn('PowerLayerMath.Count(creature.Powers', source)
        self.assertIn('.Select(power => power.Amount)', source)

    def test_production_aggregation_is_linked_not_duplicated(self):
        self.assertIn('amounts.Sum(amount => Math.Abs(amount))', read('src/Core/Powers/PowerLayerMath.cs'))
        self.assertIn('../../src/Core/Powers/PowerLayerMath.cs', read('tests/DesignSyncContracts/DesignSyncContracts.csproj'))
        source = read('tests/DesignSyncContracts/Program.cs')
        self.assertIn('PowerLayerMath.Count(amounts)', source)
        for example in ('([-2, -3], 5)', '([-2, 3], 5)', '([0, -2, 3, 0], 5)', '([], 0)'):
            self.assertIn(example, source)

    def test_five_card_contracts_are_registered(self):
        catalog = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs')
        for method in ('Healing', 'Judgment', 'LastStand', 'Draw', 'Energy'):
            self.assertIn(f'await DesignSyncSignedLayerContract.{method}(ctx, upgraded);', catalog)

    def test_native_negative_classification_and_hidden_power(self):
        source = read('src/Debugging/CardEffects/DesignSyncSignedLayerContract.cs')
        for token in ('ctx.ApplyPower<StrengthPower>(ctx.Self, -2)',
                      'ctx.ApplyPower<DexterityPower>(ctx.PrimaryEnemy, -3)',
                      'ctx.ApplyPower<AmbergrisPower>(ctx.Self, 4)',
                      'ctx.Self.GetPower<StrengthPower>()!.TypeForCurrentAmount',
                      '!ctx.Self.GetPower<AmbergrisPower>()!.IsVisible',
                      'crossing zero leaves no debuff layers', 'crossing zero adds two buff layers'):
            self.assertIn(token, source)

    def test_previews_and_actual_settlement_both_checked(self):
        source = read('src/Debugging/CardEffects/DesignSyncSignedLayerContract.cs')
        for token in ('actual damage matches negative dexterity preview',
                      'self-debuff bonus plus native strength penalty',
                      'healing ignores negative and hidden states',
                      'three cards drawn for negative three strength',
                      'negative stat enables energy reward',
                      'permanent instance still omits combat total',
                      'DesignSyncTextBatchContract.Text(card, PileType.Hand'):
            self.assertIn(token, source)


if __name__ == '__main__':
    unittest.main()
