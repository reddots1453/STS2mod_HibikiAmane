"""Source-wiring checks only; native runtime behavior requires ms_test_humility_runtime."""
import json
import unittest
from TestDesignSyncNeutral20260927 import read


class HumilityRuntime(unittest.TestCase):
    def test_instance_boundary_and_no_enchantment_reapplication(self):
        code = read('src/Core/Cards/HumilityRewriteCapability.cs')
        for text in ('card.Pile?.IsCombatPile != true', 'CardType.Attack or CardType.Skill',
                     'HumilityNativeEffects.Validate(program)', 'card.AddCapability(capability, allowMerge: false)',
                     'LoadAdditionalState', 'OnOwnerCardUpgraded', 'card.BaseReplayCount = 0'):
            self.assertIn(text, code)
        self.assertNotIn('.ModifyCard()', code)
        self.assertNotIn('DeckVersion.', code)
        self.assertIn('LayeredEnchantment layered', code)

    def test_native_wrapper_not_replaced(self):
        code = read('src/Core/Cards/HumilityRewriteCapability.cs')
        self.assertIn('CardPlayCapability', code)
        self.assertIn('await _program.Execute', code)
        self.assertIn('return true;', code)
        self.assertNotIn('OnPlayWrapper', read('src/Patches/HumilityRewritePatches.cs'))

    def test_native_modifiers_and_grouped_hit_count(self):
        code = read('src/Core/Cards/HumilityNativeEffects.cs')
        for text in ('ResolveEnergyXValue()', 'ResolveStarXValue()',
                     'play.SecondaryResources().Value(DesireResource.Id)',
                     'FromCard(play.Card, play).WithHitCount(hits)',
                     'TargetingRandomOpponents(_combat!)',
                     'CreatureCmd.GainBlock(creature, baseAmount, ValueProp.Move, play)'):
            self.assertIn(text, code)
        self.assertNotIn('new Random', code)

    def test_nested_hook_stream_and_direct_result_override(self):
        code = read('src/Patches/HumilityRewritePatches.cs')
        for text in ('HarmonyPriority(Priority.Last)', '_readingRunListeners > 0 ? input : Filter(input)',
                     'finally { _readingRunListeners--; }', 'model is CardModel card',
                     'OnTurnEndInHandWrapper', 'CardMethods("GetResultLocationForCardPlay")',
                     '__instance.IsDupe', '__instance.ExhaustOnNextPlay'):
            self.assertIn(text, code)

    def test_description_native_preview_and_localization(self):
        code = read('src/Core/Cards/HumilityRewritePresentation.cs')
        for text in ('new DamageVar(amount, ValueProp.Move)', 'new BlockVar(amount, ValueProp.Move)',
                     'variable.UpdateCardPreview', 'ToHighlightedString', 'repeats == 1 ? ""'):
            self.assertIn(text, code)
        data = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        self.assertEqual('{Effects}', data['MAIDEN_HUMILITY_REWRITE.description'])
        self.assertEqual('{Target}造成{Amount}点伤害{Repeats}。', data['MAIDEN_HUMILITY_REWRITE.damage'])
        self.assertEqual('{Target}获得{Amount}点[gold]格挡[/gold]{Repeats}。', data['MAIDEN_HUMILITY_REWRITE.block'])

    def test_game_scenarios_use_real_play_not_direct_card_callback(self):
        code = read('src/ConsoleCommands/DesignHumilityRuntimeTestConsoleCmd.cs')
        for text in ('card.SpendResources()', 'card.OnPlayWrapper', 'await ctx.Play', 'CardPileCmd.Draw',
                     'CardCmd.Enchant<Swift>', 'CardCmd.Enchant<Glam>', 'CardCmd.Enchant<Steady>',
                     'KinglyKick', 'ShiningStrike', 'DoubleDefense', 'Whirlwind', 'AllHopeLost',
                     'RelicCmd.Obtain(ModelDb.Relic<ChemicalX>()', 'MutableClone()', '.SaveState()',
                     'probe.BeforeCount', 'runListeners.Count'):
            self.assertIn(text, code)
        self.assertIn('args[0] != "confirm"', code)
        self.assertIn('finally { TestMode.IsOn = previousTestMode; _running = false; }', code)
        self.assertNotIn('await pommel.OnPlay(', code)


if __name__ == '__main__':
    unittest.main()
