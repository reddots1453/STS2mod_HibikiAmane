"""Payment forecast contracts. Native command and preview tests require the game."""
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model


class MagicBurstContracts(unittest.TestCase):
    def test_design_payment_order_and_card_formula(self):
        design = re.sub(r'\s', '', read('DesignDoc.md'))
        self.assertIn('选择魔力解放时先失去1层魔装耐久，再结算魔力解放效果', design)
        self.assertIn('造成7点伤害。魔力解放：每拥有一层增益效果，额外造成2/3点伤害。（造成？点伤害）', design)
        self.assertIn('魔装耐久降至0时保持变身', design)

    def test_forecast_counts_after_armour_payment_but_not_after_deferred_amplification(self):
        source = model('MagicBurst')
        for token in ('static (card, _) => PreviewReleasedLayers(card)',
                      'PowerLayerQuery.CountBuffLayers(creature)', '.CanPreviewOverdraft(card) == true',
                      'return layers;', 'TransformationCmd.IsTransformed(creature)',
                      'armor is not { Amount: > 0 }', 'armor.IsVisible',
                      'armor.TypeForCurrentAmount == MegaCrit.Sts2.Core.Entities.Powers.PowerType.Buff',
                      'Math.Max(0, layers - (counted ? 1 : 0))'):
            self.assertIn(token, source)
        self.assertLess(source.index('.CanPreviewOverdraft(card)'), source.index('var armor ='))

    def test_actual_effect_keeps_original_order_and_damage(self):
        source = model('MagicBurst')
        play = source[source.index('protected override async Task OnPlay'):]
        self.assertLess(play.index('await TransformationCmd.PayOverdraft'), play.index('PowerLayerQuery.CountBuffLayers'))
        for token in ('decimal damage = DynamicVars.CalculationBase.BaseValue;', 'if (overdrafted)',
                      '* DynamicVars.ExtraDamage.BaseValue', 'DamageCmd.Attack(damage)', 'UpgradeValueBy(1)'):
            self.assertIn(token, play)
        self.assertNotIn('PreviewReleasedLayers', play)

    def test_availability_query_is_read_only_and_owner_scope_aware(self):
        source = read('src/Powers/TransformationPowers.cs')
        query = source.split('internal bool CanPreviewOverdraft(CardModel card) =>', 1)[1].split(';', 1)[0]
        for token in ('Amount > 0', 'card.Owner?.Creature == Owner', 'CardType.Attack or CardType.Skill',
                      '!AmplificationConsumptionScope.IsExempt(card)', '_cardToAmplify == null',
                      'ReferenceEquals(card, _cardToAmplify)', '_reservedForOverdraft < Amount'):
            self.assertIn(token, query)
        for forbidden in ('++', '--', 'TryReserve', 'PowerCmd', 'await ', 'Publish', 'Flash('):
            self.assertNotIn(forbidden, query)

    def test_independent_case_values_cover_armour_amp_decline_and_negative_stats(self):
        source = read('src/Debugging/CardEffects/DesignSyncMagicBurstContract.cs')
        rows = re.findall(r'new\("([^"]+)", ([^\n]+)\),', source)
        self.assertEqual(len(rows), 11)
        by_name = {name: [part.strip() for part in values.split(',')] for name, values in rows}
        # Armor, amplification, dexterity, strength, accept, previews, actuals, remaining resources.
        expected = {
            'last armour': '1, 0, 2, 0, true, 13, 16, 13, 16, 0, 0',
            'three armour': '3, 0, 2, 0, true, 17, 22, 17, 22, 2, 0',
            'five armour': '5, 0, 2, 0, true, 21, 28, 21, 28, 4, 0',
            'declined armour': '1, 0, 2, 0, false, 13, 16, 7, 7, 1, 0',
            'one amplification': '-1, 1, 2, 0, true, 19, 24, 19, 24, 0, 0',
            'two amplification': '-1, 2, 2, 0, true, 22, 28, 22, 28, 0, 1',
            'amplification before armour': '1, 1, 2, 0, true, 25, 33, 25, 33, 1, 0',
            'negative dexterity': '1, 0, -2, 0, true, 9, 10, 9, 10, 0, 0',
            'negative strength': '1, 0, 2, -2, true, 11, 14, 11, 14, 0, 0',
        }
        for name, values in expected.items():
            self.assertEqual(by_name[name], values.split(', '))
        self.assertEqual(by_name['zero armour'][5:9], ['7'] * 4)
        self.assertEqual(by_name['no resource'][5:9], ['7'] * 4)

    def test_game_contract_executes_real_damage_and_preserves_preview_resources(self):
        source = read('src/Debugging/CardEffects/DesignSyncMagicBurstContract.cs')
        for token in ('await ctx.Reset()', 'await ctx.SetUpArmour(entry.Armor)',
                      'await ctx.ApplyPower<AmbergrisPower>(ctx.Self, 3)', 'query < 3',
                      'DesignSyncCombatTextContract.AssertText(', 'ctx.AssertDamage(entry.Name + " actual damage"',
                      'entry.ActualUpgraded : entry.ActualBase', 'entry.RemainingArmor',
                      'entry.RemainingAmplification', 'entry.Accept ? 0 : 1',
                      '"one energy cost"', 'TransformationCmd.IsTransformed(ctx.Self)'):
            self.assertIn(token, source)
        catalog = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs')
        self.assertIn('CustomVariants<MagicBurst>(DesignSyncMagicBurstContract.Run, 100)', catalog)

    def test_reserved_other_card_and_exempt_scope_have_explicit_cases(self):
        source = read('src/Debugging/CardEffects/DesignSyncMagicBurstContract.cs')
        for token in ('using (AmplificationConsumptionScope.Enter(ownerCard))',
                      'await amplification.BeforeCardPlayed(receipt)',
                      '!amplification.CanPreviewOverdraft(otherCard)',
                      'amplification.TryReserveForOverdraft(ownerCard)',
                      '!amplification.CanPreviewOverdraft(ownerCard)',
                      'await amplification.AfterCardPlayed(new BlockingPlayerChoiceContext(), receipt)',
                      '"reservation remains deferred", 1, amplification.Amount'):
            self.assertIn(token, source)


if __name__ == '__main__':
    unittest.main()
