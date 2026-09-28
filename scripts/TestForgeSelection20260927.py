"""Source integration contracts; actual card-selection scenarios require the game."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding='utf-8-sig')


class ForgeSelectionTests(unittest.TestCase):
    def setUp(self):
        self.card = read('src/Cards/MvpAdvancedCards.cs').split('public sealed class BeyondReasonForge :')[1]
        self.test = read('src/Debugging/CardEffects/DesignSyncForgeContract.cs')

    def test_nine_design_options(self):
        options = re.findall(r'(?:Vanilla|Custom)<(\w+)>\("([^"]+)", "([^"]+)"\)', self.card)
        self.assertEqual(options, [
            ('Swift', 'swift', '迅捷：4'), ('ChargeEnchantment', 'charge', '充能：3'),
            ('Glam', 'glam', '华彩'), ('TezcatarasEmber', 'ember', '特兹卡塔拉的余烬'),
            ('Instinct', 'instinct', '本能'), ('ProliferationEnchantment', 'proliferation', '增殖'),
            ('IronWallEnchantment', 'iron_wall', '铁壁'), ('Nimble', 'nimble', '灵巧：8'),
            ('Sharp', 'sharp', '锋利：8')])

    def test_precise_application_amounts(self):
        for id_, type_, amount in [('swift', 'Swift', 4), ('charge', 'ChargeEnchantment', 3),
                                  ('glam', 'Glam', 1), ('ember', 'TezcatarasEmber', 1),
                                  ('instinct', 'Instinct', 1), ('proliferation', 'ProliferationEnchantment', 1),
                                  ('iron_wall', 'IronWallEnchantment', 1), ('nimble', 'Nimble', 8), ('sharp', 'Sharp', 8)]:
            self.assertRegex(self.card, rf'case "{id_}": CombatEnchantmentCmd\.Apply\w*<{type_}>\(card, {amount}\);')
            self.assertIn(f'new("{id_}", typeof({type_}), {amount},', self.test)

    def test_original_random_stream_and_legal_filter(self):
        self.assertIn('CreateOptions().Where(option => hand.Any(option.CanApply))', self.card)
        self.assertIn('UnstableShuffle(Owner.RunState.Rng.CombatCardSelection).Take(3)', self.card)
        self.assertNotIn('Rng.Chaotic', self.card)

    def test_option_identity_and_combat_checked_after_await(self):
        option_await = self.card.index('await CardSelectCmd.FromChooseACardScreen')
        guard = self.card.index('!choiceCards.Contains(selectedOption)')
        hand_await = self.card.index('await CardSelectCmd.FromHand')
        self.assertLess(option_await, guard)
        self.assertLess(guard, hand_await)
        self.assertIn('CombatState == combat && Owner.Creature.IsAlive', self.card)
        self.assertIn('!CombatManager.Instance.IsOverOrEnding', self.card)

    def test_target_checked_before_and_after_await(self):
        self.assertIn('card => IsEligibleTarget(card, optionDef)', self.card)
        self.assertIn('if (CanContinue() && target != null && IsEligibleTarget(target, optionDef))', self.card)
        for fragment in ('card.Owner == Owner', 'card.CombatState == CombatState',
                         'card.Pile == PileType.Hand.GetPile(Owner)',
                         'LayeredEnchantments.HasOpenSlot(card) && option.CanApply(card)'):
            self.assertIn(fragment, self.card)

    def test_runtime_coverage_and_rng_restore(self):
        for fragment in ('foreach (Expected expected in All)', 'CheckSkillOnly', 'CheckEmpty',
                         '"moved", "enchanted", "all_moved", "foreign_option"',
                         'CheckPermanentAndLayered', 'exact legal hand target set',
                         'only expected selection RNG consumed', 'permanent original not enchanted'):
            self.assertIn(fragment, self.test)
        self.assertIn('finally { ctx.Player.RunState.Rng.CombatCardSelection.LoadFromSerializable(selectionState); }', self.test)
        self.assertIn('CustomVariants<BeyondReasonForge>(DesignSyncForgeContract.Run, 120)',
                      read('src/Debugging/CardEffects/CardEffectTestCatalog.cs'))

    def test_selection_description_oracle_includes_original_punctuation(self):
        self.assertIn('"选择附魔：" + All.Single(x => x.Id == choice.ChoiceId).Display + "。"', self.test)
        self.assertIn('从3个强大的附魔中选择一项，附加给1张手牌。\\n消耗。', self.test)


if __name__ == '__main__':
    unittest.main()
