"""Static design/wiring checks; ms_test_forgotten_soul executes native exhaust in game."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


class ForgottenSoulContracts(unittest.TestCase):
    def test_exact_design_sentences_and_both_localized_variations(self):
        design = read('DesignDoc.md').split('#### 遗忘之魂')[1].split('### 4.')[0]
        loc = json.loads(read('MaidenSuccubus/localization/zhs/relics.json'))
        for suffix, amount, comparator in (('description', 1, '≥'), ('descriptionCorrupt', 2, '＜')):
            raw = loc['MAIDEN_SUCCUBUS_FORGOTTEN_SOUL.' + suffix]
            plain = re.sub(r'\[[^\]]*\]', '', raw).replace('{Damage}', str(amount))
            expected = f'每当你消耗1张牌，对随机敌人造成{amount}点伤害。堕落值{comparator}4：变奏。'
            self.assertEqual(plain, expected)
            if amount == 1:
                self.assertIn(expected, design)
            else:
                self.assertIn('当堕落值达到4或更高时，变为：' + expected, design)
            self.assertIn('[gold]消耗[/gold]', raw)
            self.assertIn('[purple]堕落值[/purple]', raw)
            self.assertIn('[gold]变奏[/gold]', raw)
        # Never overwrite vanilla's localization key for all characters.
        self.assertNotIn('FORGOTTEN_SOUL.description', loc)

    def test_exact_entity_and_scope_guards_precede_run_query(self):
        source = read('src/Core/Relics/ForgottenSoulVariation.cs')
        for token in ('relic is not ForgottenSoul', '!relic.IsMutable',
                      'relic.Owner?.Character is not MaidenSuccubusCharacter',
                      'relic.Owner.RunState is not RunState run',
                      'CorruptionQuery.Get(run) >= 4 ? 2 : 1'):
            self.assertIn(token, source)
        self.assertLess(source.index('!relic.IsMutable'), source.index('relic.Owner?'))
        self.assertLess(source.index('return false'), source.index('CorruptionQuery.Get'))
        for forbidden in ('LocalContext', 'CharonsAshes', 'static RunState', 'static Player'):
            self.assertNotIn(forbidden, source)

    def test_two_safe_getter_patches_preserve_original_exhaust_and_rng(self):
        source = read('src/Patches/ForgottenSoulVariationPatch.cs')
        self.assertEqual(source.count('Safe.Run('), 2)
        self.assertEqual(source.count('ForgottenSoulVariation.TryGetDamage(__instance, out int damage)'), 2)
        for token in ('HarmonyPatch(typeof(RelicModel), "get_DynamicVars")',
                      'HarmonyPatch(typeof(RelicModel), "get_Description")',
                      '__result.Damage.BaseValue = damage', 'ref LocString __result',
                      '__result = result;'):
            self.assertIn(token, source)
        code = re.sub(r'//[^\n]*', '', source)
        for forbidden in ('Prefix(', 'async ', 'await ', 'CreatureCmd', 'DamageCmd', 'AfterCardExhausted',
                          'NextItem(', '.Rng', 'new DamageVar', 'LocalContext', 'HarmonyTranspiler'):
            self.assertNotIn(forbidden, code)
        self.assertNotIn('__instance.DynamicVars', source, 'getter patch must not recurse')

    def test_runtime_command_has_confirmation_and_is_never_automatic(self):
        source = read('src/ConsoleCommands/DesignForgottenSoulTestConsoleCmd.cs')
        self.assertTrue(source.startswith('#if DEBUG'))
        for token in ('CmdName => "ms_test_forgotten_soul"', 'IsNetworked => false',
                      'args.Length != 1 || args[0] != "confirm"', 'player.RunState.Players.Count != 1',
                      'combat.HittableEnemies.Count < 2', 'CombatManager.Instance.IsEnding',
                      'TestMode.IsOn = previousTestMode; _running = false;'):
            self.assertIn(token, source)
        self.assertNotIn('Input.', source)

    def test_real_exhaust_target_and_full_rng_state_compared(self):
        source = read('src/ConsoleCommands/DesignForgottenSoulTestConsoleCmd.cs')
        for token in ('await RelicCmd.Obtain<ForgottenSoul>(player)',
                      'new Rng(run.Rng.CombatTargets.ToSerializable())',
                      'expectedRng.NextItem(combat.HittableEnemies)',
                      'await CardCmd.Exhaust(choice, card, skipVisuals: true)',
                      'hp[enemy] - enemy.CurrentHp == (enemy == expectedTarget ? expected : 0)',
                      'run.Rng.CombatTargets.ToSerializable().ToString() == expectedRng.ToSerializable().ToString()',
                      'PowerCmd.Apply<StrengthPower>', 'PowerCmd.Apply<VulnerablePower>',
                      'soul.DynamicVars.Damage.Props == ValueProp.Unpowered',
                      'await RelicCmd.Remove(soul)'):
            self.assertIn(token, source)
        self.assertNotIn('soul.AfterCardExhausted(choice, card', source)

    def test_live_boundary_reads_and_save_clone_are_not_pickup_only(self):
        source = read('src/ConsoleCommands/DesignForgottenSoulTestConsoleCmd.cs')
        for token in ('Enumerable.Range(-5, 11).Concat(new[] { 3, 4, 3, 5, -5 })',
                      'soul.DynamicDescription.GetFormattedText()',
                      'canonical.DynamicDescription.GetFormattedText() == originalText',
                      'RelicModel.FromSerializable(soul.ToSerializable())',
                      'soul.ClonePreservingMutability()', 'soul.FloorAddedToDeck == floor',
                      'run.Rng.CombatTargets.ToSerializable().ToString() == rngBeforeReading'):
            self.assertIn(token, source)

    def test_other_players_and_wrong_relic_are_explicitly_tested(self):
        source = read('src/ConsoleCommands/DesignForgottenSoulTestConsoleCmd.cs')
        for token in ('Player.CreateForNewRun<Ironclad>', 'player.NetId + 2000',
                      'foreignSoul.DynamicVars.Damage.BaseValue == 1',
                      'remoteSoul.DynamicVars.Damage.BaseValue == expected',
                      'canonical.DynamicVars.Damage.BaseValue == 1',
                      'ashes.DynamicVars.Damage.BaseValue == 3',
                      'await soul.AfterCardExhausted(choice, foreignCard, false)',
                      'rngBeforeForeign', 'ForgottenSoulVariation.TryGetDamage(ownerless, out _)'):
            self.assertIn(token, source)


if __name__ == '__main__':
    unittest.main()
