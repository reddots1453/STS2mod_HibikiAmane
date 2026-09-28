"""Only the two numeric discrepancies found in the DesignDoc change scope."""
import json
import unittest
from TestDesignSyncNeutral20260927 import read


class RemainingNumberContracts(unittest.TestCase):
    def test_gate_uses_five_then_four_and_current_resource(self):
        self.assertIn('欲望大于等于5/4时才能打出。', read('DesignDoc.md'))
        code = read('src/Cards/MvpNeutralCardsBatch3.cs').split('public sealed class ChangePanties', 1)[1]
        self.assertIn('new DynamicVar("Threshold", 5)', code)
        self.assertIn('DynamicVars["Threshold"].UpgradeValueBy(-1)', code)
        self.assertIn('Data.Desire.Get(Owner) >= DynamicVars["Threshold"].IntValue', code)
        test = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs').split('private static void ChangePantiesProbe()', 1)[1].split('private static void CoronationProbe()', 1)[0]
        for token in ('upgraded ? 4 : 5', 'threshold - 1', 'threshold + 1', 'playable at threshold', 'await ctx.Play(card)'):
            self.assertIn(token, test)

    def test_damage_is_five_for_both_versions(self):
        design = read('DesignDoc.md').split('欲望鞭挞\n', 1)[1].split('\n\n', 1)[0]
        self.assertIn('造成5点伤害。', design)
        code = read('src/Cards/Iteration1CorruptCards.cs').split('public sealed class DesireWhip', 1)[1].split('public sealed class PleasureGarden', 1)[0]
        self.assertIn('new DamageVar(5, ValueProp.Move)', code)
        self.assertIn('OnUpgrade() => EnergyCost.UpgradeBy(-1)', code)
        self.assertNotIn('Damage.UpgradeValueBy', code)
        test = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs').split('private static void DesireWhipProbe()', 1)[1].split('private static void BlizzardProbe()', 1)[0]
        self.assertIn('ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, 5)', test)
        self.assertIn('upgraded ? 0 : 1', test)

    def test_text_uses_changed_variables_not_old_constants(self):
        loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        self.assertIn('[pink]欲望[/pink]大于等于{Threshold:diff()}时才能打出。', loc['MAIDEN_SUCCUBUS_CARD_CHANGE_PANTIES.description'])
        self.assertIn('{Damage:diff()}', loc['MAIDEN_SUCCUBUS_CARD_DESIRE_WHIP.description'])


if __name__ == '__main__':
    unittest.main()
