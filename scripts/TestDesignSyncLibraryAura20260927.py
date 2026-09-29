"""New library/temperance wiring; native scenarios are compiled, not executed here."""
import json
import unittest
from TestDesignSyncNeutral20260927 import read


class LibraryAuraContracts(unittest.TestCase):
    def test_formal_new_descriptions_and_choice_limit(self):
        loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        for model, title in [('TEMPERANCE_SIGNET', '节制之戒'), ('TEMPERANCE_CIRCLET', '节制之环')]:
            self.assertEqual(title, loc[f'MAIDEN_SUCCUBUS_CARD_{model}.title'])
        self.assertEqual('本回合你不能再抽牌。\n选择一个牌堆，随机打出其中的{Cards:diff()}张牌。',
                         loc['MAIDEN_SUCCUBUS_CARD_TEMPERANCE_SIGNET.description'])
        self.assertEqual('随机打出[gold]{PileName}[/gold]中的{Count:diff()}张牌。',
                         loc['MAIDEN_SUCCUBUS_CARD_LIBRARY_PILE_CHOICE.description'])
        self.assertIn('choice.Configure(pile, count)', read('src/Commands/TemperancePileCmd.cs'))
        self.assertIn('HoverTipFactory.Static(StaticHoverTip.ReplayDynamic, new DynamicVar("Times", 1))',
                      read('src/Cards/Iteration1CorruptCards.cs'))

    def test_live_owner_and_combat_guard_no_permanent_mutation(self):
        code = read('src/Core/Cards/LibraryHandAura.cs')
        for token in ('IsMutable: true', 'is not MaidenSuccubusCharacter', 'CombatState == null',
                      'PileType.Hand', 'ReferenceEquals(hand[i], card)', 'neighbour.Owner == card.Owner',
                      '!neighbour.HasBeenRemovedFromState', 'LibraryNeighbourRules.Evaluate'):
            self.assertIn(token, code)
        for token in ('AddKeyword', 'RemoveKeyword', 'Enchant', 'SavedProperty'):
            self.assertNotIn(token, code)
        rules = read('src/Core/Cards/LibraryNeighbourRules.cs')
        self.assertIn('index + 1 < hand.Count', rules)
        self.assertIn('index > 0', rules)

    def test_per_play_snapshot_async_finally_and_nested_scope(self):
        code = read('src/Core/Cards/LibraryHandAura.cs')
        for token in ('WeakInstanceValueScope<CardModel, LibraryAuraEffect>', 'PileType.Play',
                      'Plays.Enter(card, InHand(card))', 'try { await original; }', 'finally { scope.Dispose(); }'):
            self.assertIn(token, code)
        patches = read('src/Patches/LibraryHandAuraPatches.cs')
        for token in ('nameof(CardModel.OnPlayWrapper)', 'Prefix(CardModel __instance',
                      'ref Task __result', 'LibraryHandAura.Complete(original, __state)', 'Finalizer',
                      'Safe.Run', 'original.ToHashSet()', 'if (add) __result++',
                      'nameof(CardModel.GetKeywordsWithSources)', '!__0.HasFlag(KeywordSources.Global)'):
            self.assertIn(token, patches)

    def test_overlay_native_coordinates_input_passthrough_and_pool_reuse(self):
        code = read('src/UI/LibraryAuraOverlay.cs')
        for token in ('"CardContainer"', '-NCard.defaultSize / 2', 'MouseFilterEnum.Ignore',
                      'CardPreviewMode.Normal', 'if (next == _effect) return', 'PileType.Hand',
                      'LibraryAuraEffect.Exhaust', 'LibraryAuraEffect.Replay', 'ShadowSize = 7'):
            self.assertIn(token, code)
        self.assertNotIn('_red.Dispose()', code)
        self.assertNotIn('_card = null', code)

    def test_ancient_generated_cards_are_not_normal_rewards(self):
        code = read('src/Cards/TemperanceCards.cs')
        self.assertEqual(2, code.count('[RegisterCard(typeof(MSGeneratedCardPool))]'))
        self.assertEqual(2, code.count('CanBeGeneratedInCombat => false'))
        self.assertEqual(2, code.count('CanBeGeneratedByModifiers => false'))
        for token in ('base(1, CardType.Skill, CardRarity.Ancient', 'base(2, CardType.Power, CardRarity.Ancient',
                      'PowerCmd.Apply<NoDrawPower>', 'DynamicVars["Cards"].UpgradeValueBy(1)',
                      'PowerCmd.Apply<YarusLibraryPower>', 'AddKeyword(CardKeyword.Retain)'):
            self.assertIn(token, code)
        pools = json.loads(read('docs/content_contract_20260824.json'))['cards']
        for model in ('TemperanceSignet', 'TemperanceCirclet'):
            self.assertEqual(['MSGeneratedCardPool'], [pool for pool, cards in pools.items() if model in cards])

    def test_actual_new_aura_test_covers_text_tome_clone_play_and_cleanup(self):
        code = read('src/Debugging/CardEffects/DesignSyncLibraryAuraContract.cs')
        for token in ('GetUnlockedCards(ctx.Player.UnlockState',
                      '!ArchaicTooth.TranscendenceCards.Contains(candidate)',
                      'tomePool.Any(candidate => candidate is InsatiableGreed',
                      'ctx.Player.RunState.CreateCard<InsatiableGreed>', 'PileType.Deck',
                      'instance.GetDescriptionForPile(pile)', 'ctx.Combat.CloneCard(left)',
                      'CardModel.FromSerializable(left.ToSerializable())', 'await ctx.Play(middle)',
                      '15m, ctx.Self.Block', 'PileType.Exhaust, middle.Pile!.Type',
                      'TaskCanceledException', 'nested non-hand play does not inherit',
                      'failure/cancellation clears captured replay', 'native exhaust is not removed',
                      'Player.CreateForNewRun<Ironclad>'):
            self.assertIn(token, code)
        self.assertIn('CustomVariants<InsatiableGreed>(DesignSyncLibraryAuraContract.Run, 20)',
                      read('src/Debugging/CardEffects/CardEffectTestCatalog.cs'))

    def test_actual_signet_cases_use_real_rng_draw_and_turn_end(self):
        code = read('src/Debugging/CardEffects/DesignSyncTemperanceSignetContract.cs')
        for token in ('upgraded ? 4 : 3', 'new[] { 0, 1, 3, 4, 6 }', 'PileType.Exhaust, 2',
                      'new Rng(', 'rng.NextInt(i + 1)', 'actual.SequenceEqual(played)',
                      'actual.Distinct().Count()', 'await ctx.Play(signet, selectedIndices: [option])',
                      'CardPileCmd.Draw(context, 1, ctx.Player)', 'AfterSideTurnEnd(context',
                      'signet native draw resumes', 'signet full rendered text'):
            self.assertIn(token, code)
        self.assertNotIn('.StableShuffle(', code)

    def test_production_neighbour_rules_are_executed_not_test_copy(self):
        self.assertIn('../../src/Core/Cards/LibraryNeighbourRules.cs',
                      read('tests/DesignSyncContracts/DesignSyncContracts.csproj'))
        code = read('tests/DesignSyncContracts/Program.cs')
        for token in ('LibraryNeighbourRules.Evaluate', 'leave hand revokes', 'reorder recomputes',
                      'int.MaxValue, LibraryAuraEffect.None', 'int.MinValue, LibraryAuraEffect.None'):
            self.assertIn(token, code)


if __name__ == '__main__':
    unittest.main()
