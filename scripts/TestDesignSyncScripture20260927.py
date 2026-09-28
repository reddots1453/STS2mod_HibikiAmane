"""Static scripture design/wiring checks. Native command scenarios require the game."""
import json
import re
import unittest
import AuditCardLocalization as audit
from TestDesignSyncNeutral20260927 import read


class ScriptureContracts(unittest.TestCase):
    def test_nimble_resonance_timing_is_explicit_design(self):
        design = read('DesignDoc.md')
        self.assertIn('特别地，轻灵圣言临时敏捷的触发时机视为回合结束时，因为临时敏捷回合结束时减少1回合生效时间', design)
        source = read('src/Powers/Scriptures/ScripturePowerTemplate.cs')
        applied = source.split('public override async Task AfterApplied(')[1].split('public override async Task AfterPlayerTurnStart(')[0]
        self.assertIn('await TriggerEffect(new ThrowingPlayerChoiceContext());', applied)
        self.assertNotIn('await TriggerAndPublish(', applied)
        end = source.split('else if (Timing == ScriptureTriggerTiming.OnApply)')[1].split('private async Task TriggerPublishAndTick')[0]
        self.assertIn('await ScriptureCmd.Publish(new ScriptureTriggered(this, Owner));', end)
        self.assertLess(end.index('await ScriptureCmd.Publish'), end.index('await Tick'))
        self.assertNotIn('TriggerEffect(', end, 'do not grant another two Dexterity every turn')

    def test_active_owner_side_and_last_tick_guards(self):
        source = read('src/Powers/Scriptures/ScripturePowerTemplate.cs')
        for token in ('IsMutable && Amount > 0 && Owner.IsAlive', 'Owner.Powers.Contains(this)',
                      'IsActive && Timing == ScriptureTriggerTiming.TurnStart', 'player.Creature == Owner',
                      '!IsActive || side != Owner.Side || !participants.Contains(Owner)'):
            self.assertIn(token, source)
        tick = source.split('private async Task Tick(')[1]
        self.assertIn('if (IsActive)', tick)
        self.assertIn('await PowerCmd.ModifyAmount(', tick)
        order = source.split('private async Task TriggerPublishAndTick(')[1].split('private async Task TriggerAndPublish')[0]
        self.assertLess(order.index('await TriggerAndPublish'), order.index('await Tick'))

    def test_other_five_timings_and_independent_instances_preserved(self):
        source = read('src/Powers/Scriptures/ScripturePowers.cs')
        for name, timing in [('Guardian', 'TurnEnd'), ('Nimble', 'OnApply'), ('Punishment', 'TurnEnd'),
                             ('Wisdom', 'TurnStart'), ('Vitality', 'TurnStart'), ('Bliss', 'TurnStart')]:
            self.assertRegex(source, 'class ' + name + r'ScripturePower[^{}]*\{\s*(?:private const int DexterityAmount = 2;\s*)?protected override ScriptureTriggerTiming Timing =>\s*ScriptureTriggerTiming\.' + timing)
        template = read('src/Powers/Scriptures/ScripturePowerTemplate.cs')
        self.assertIn('PowerInstanceType.Instanced', template)
        self.assertIn('PowerType.Buff', template)
        self.assertIn('public override Task AfterRemoved(Creature oldOwner)', source)
        self.assertIn('-DexterityAmount', source)

    def test_exact_six_card_templates_and_duration_variants(self):
        loc = json.loads(read('MaidenSuccubus/localization/zhs/cards.json'))
        design = read('DesignDoc.md').splitlines()
        rows = [
            ('GUARDIAN', '守护圣言', '每回合结束时获得3点[gold]格挡[/gold]。'),
            ('NIMBLE', '轻灵圣言', '获得2层临时[gold]敏捷[/gold]。'),
            ('PUNISHMENT', '惩戒圣言', '每回合结束时对随机敌人给予1层[gold]断罪[/gold]。'),
            ('WISDOM', '睿智圣言', '每回合开始时获得抽{Cards:diff()}张牌。'),
            ('VITALITY', '活力圣言', '回合开始时获得{Energy:maidenEnergyIcons()}。'),
            ('BLISS', '极乐圣言', '回合开始时失去{energyPrefix:maidenDesireIcons(1)}。\n若{energyPrefix:maidenDesireIcons(0)}，获得{Energy:maidenEnergyIcons()}并抽{Cards:diff()}张牌。'),
        ]
        for stem, title, body in rows:
            key = 'MAIDEN_SUCCUBUS_CARD_' + stem + '_SCRIPTURE'
            self.assertEqual(loc[key + '.title'], title)
            self.assertEqual(loc[key + '.description'], '持续{Duration:diff()}回合。\n' + body)
            expected = re.sub(r'\[[^\]]*\]', '', body).replace('\n', '')
            expected = expected.replace('{Cards:diff()}', '1').replace('{Energy:maidenEnergyIcons()}', '1费')
            expected = expected.replace('{energyPrefix:maidenDesireIcons(1)}', '1点欲望').replace('{energyPrefix:maidenDesireIcons(0)}', '欲望为0')
            self.assertEqual(audit.design_effect(title, design).splitlines()[0], '持续2/3回合。' + expected)
        source = read('src/Cards/Scriptures/ScriptureCardTemplate.cs')
        self.assertIn('new IntVar("Duration", 2)', source)
        self.assertIn('DynamicVars["Duration"].UpgradeValueBy(1)', source)

    def test_guardian_display_and_real_block_use_hooks_not_cached_dexterity(self):
        source = read('src/Powers/Scriptures/GuardianScriptureBlockVar.cs')
        self.assertIn('BaseBlock = 3', source)
        self.assertIn('base(BaseBlock, ValueProp.Move)', source)
        self.assertIn('return Hook.ModifyBlock(', source)
        self.assertIn('CreatureCmd.GainBlock(Owner, DynamicVars.Block, null)', read('src/Powers/Scriptures/ScripturePowers.cs'))
        loc = json.loads(read('MaidenSuccubus/localization/zhs/powers.json'))
        for suffix in ('description', 'smartDescription'):
            self.assertIn('{Block}', loc['MAIDEN_SUCCUBUS_POWER_GUARDIAN_SCRIPTURE_POWER.' + suffix])

    def test_all_six_icons_have_distinct_formal_mapping(self):
        source = read('src/UI/PowerIconAssets.cs')
        values = []
        for name in ('Guardian', 'Nimble', 'Punishment', 'Wisdom', 'Vitality', 'Bliss'):
            match = re.search(r'\["' + name + r'ScripturePower"\] = "([^"]+)"', source)
            self.assertIsNotNone(match)
            values.append(match[1])
        self.assertEqual(len(set(values)), 6)

    def test_game_scenarios_measure_each_tick_resonance_and_cleanup(self):
        source = read('src/Debugging/CardEffects/DesignSyncScriptureContract.cs')
        for token in ('await ctx.Play(card)', 'await ctx.ApplyPower<HolyResonancePower>(ctx.Self, 2)',
                      'ScriptureCmd.Triggered += Observe', 'finally { ScriptureCmd.Triggered -= Observe; }',
                      'for (int turn = 1; turn <= duration; turn++)',
                      'events[^1].Remaining == duration - turn + 1',
                      'PowerLayerQuery.CountBuffLayers(ctx.Self)', 'card is GuardianScripture ? 7 : card is NimbleScripture ? 5 : 3',
                      'turn < duration, ctx.Self.Powers.Contains(power)', 'Math.Max(0, 2 - turn)',
                      'card is BlissScripture && turn >= 2', 'beforeWrongPhase, Snapshot()',
                      'afterExpiry, Snapshot()', 'await PowerCmd.Remove(removed)',
                      'two applications each add two', 'early removal cleans its own bonus'):
            self.assertIn(token, source)

    def test_guarded_selector_requires_all_six_and_real_effect_assertions(self):
        runner = read('src/Debugging/CardEffects/CardEffectTestRunner.cs')
        self.assertIn('"ds27-scriptures"', runner)
        self.assertIn('DesignSyncScriptureContract.Types.Contains(spec.CardType)', runner)
        self.assertIn('if (batch.Length != 6)', runner)
        catalog = read('src/Debugging/CardEffects/CardEffectTestCatalog.cs')
        self.assertIn('CustomVariants<T>(DesignSyncScriptureContract.Run, 20)', catalog)
        for name in ('Guardian', 'Nimble', 'Punishment', 'Wisdom', 'Vitality', 'Bliss'):
            self.assertIn('Scripture<' + name + 'Scripture>();', catalog)


if __name__ == '__main__':
    unittest.main()
