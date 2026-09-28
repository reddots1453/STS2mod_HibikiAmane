"""Source-wiring checks only; native runtime behavior requires ms_test_humility_runtime."""
import json
import unittest
from TestDesignSyncNeutral20260927 import read


class HumilityRuntime(unittest.TestCase):
    def test_intrinsic_amplification_marker_has_shared_runtime_gate(self):
        rules = read('src/Core/Transformation/IDoubleMagicAmplification.cs')
        self.assertIn('card is IDoubleMagicAmplification && HumilityRewriteCapability.Find(card) == null', rules)
        immediate = read('src/Powers/TransformationPowers.cs')
        delayed = read('src/Core/Transformation/TransformationCmd.cs')
        self.assertIn('MagicAmplificationCardRules.HasIntrinsicDouble(card)', immediate)
        self.assertIn('MagicAmplificationCardRules.HasIntrinsicDouble(source)', delayed)
        self.assertIn('Owner.HasPower<TacticalCorePower>()', immediate)
        self.assertIn('creature.HasPower<TacticalCorePower>()', delayed)

    def test_mixed_and_exhaust_scenarios_use_native_commands(self):
        code = read('src/ConsoleCommands/DesignHumilityRuntimeTestConsoleCmd.cs')
        for text in ('ctx.Add<LightArrow>', 'DelayedAmplificationSample',
                     'ApplyAmplificationToDelayedValue(Owner.Owner.Creature, Owner, 10)',
                     'CardCmd.Exhaust(choice, barrier)', 'CardCmd.Exhaust(choice, scythe)',
                     'scythe.CurrentDamage == 18', 'ctx.Add<ShiningSword>', 'swordProbe.BeforeCount == 1'):
            self.assertIn(text, code)

    def test_intrinsic_properties_do_not_replace_native_can_play(self):
        code = read('src/Patches/HumilityRewritePatches.cs')
        for text in ('CardMethods("get_IsPlayable")', 'CardMethods("get_HasTurnEndInHandEffect")',
                     '__originalMethod.Name == "get_IsPlayable"', '"Humility.IntrinsicFlags"'):
            self.assertIn(text, code)
        self.assertNotIn('CardMethods("CanPlay")', code)
        self.assertNotIn('nameof(CardModel.CanPlay)', code)

    def test_holy_profiles_have_native_rule_boundary_scenarios(self):
        code = read('src/ConsoleCommands/DesignHumilityRuntimeTestConsoleCmd.cs')
        for text in ('Hook.BeforeFlush(combat, player)', 'HumilityCardProfiles.ApplyKnown(rest)',
                     'UnplayableReason.BlockedByCardLogic', 'ReferenceEquals(preventer, sloth)',
                     'Check(!costed.CanPlay()', 'ctx.Add<DragonflyTouch>', 'ctx.Add<ForgeNimble>',
                     '!flare.HasTurnEndInHandEffect && ordinaryFlare.HasTurnEndInHandEffect'):
            self.assertIn(text, code)

    def test_profiles_bind_exact_types_and_fail_closed(self):
        code = read('src/Core/Cards/HumilityCardProfiles.cs')
        for text in ('IReadOnlyDictionary<Type, string>', 'Bindings.TryGetValue(card.GetType()',
                     'bindings.Count != HumilityProfileDefinitions.All.Count', 'throw new NotSupportedException',
                     'typeof(MaidenSuccubus.Cards.Fusion)', 'typeof(Whirlwind)'):
            self.assertIn(text, code)
        self.assertNotIn('DynamicVars', code)
        self.assertNotIn('GetFormattedText', code)
        definitions = read('src/Core/Cards/HumilityProfileDefinitions.cs')
        self.assertIn('new ReadOnlyDictionary', definitions)
        self.assertNotIn('MegaCrit', definitions)
        project = read('tests/HumilityEffectContracts/HumilityEffectContracts.csproj')
        self.assertIn('../../src/Core/Cards/HumilityProfileDefinitions.cs', project)

    def test_game_suite_uses_production_profiles(self):
        code = read('src/ConsoleCommands/DesignHumilityRuntimeTestConsoleCmd.cs')
        for text in ('HumilityCardProfiles.SupportedTypes', 'ctx.Create(type, upgraded)',
                     'HumilityNativeEffects.ResolveValue(card, name, ctx.PrimaryEnemy)',
                     'HumilityCardProfiles.ApplyKnown(dualX)', 'HumilityCardProfiles.ApplyKnown(whirlwind)',
                     'HumilityCardProfiles.ApplyKnown(empty)', 'flare.OnTurnEndInHandWrapper(choice)',
                     'HumilityCardProfiles.ApplyKnown(explosive)'):
            self.assertIn(text, code)
        self.assertNotIn('new([Damage(', code)
        self.assertNotIn('new([Block(', code)

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
