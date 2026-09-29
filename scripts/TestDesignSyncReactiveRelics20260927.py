"""Mirror/stardust wiring and exact localization; game effects have a separate console suite."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


class ReactiveRelicContracts(unittest.TestCase):
    def test_exact_design_descriptions(self):
        design = read("DesignDoc.md")
        for heading in ("反咒镜 `[RELIC-CHAR-003 · READY]`", "神界星尘 `[RELIC-CHAR-006 · READY]`"):
            self.assertIn(heading, design)
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        for suffix, expected in (
            ("COUNTER_CURSE_MIRROR.description", "给予敌人负面状态时，额外给予1层燃烧。堕落值≤-3：变奏。"),
            ("COUNTER_CURSE_MIRROR.descriptionHoly", "给予敌人负面状态时，额外给予1层断罪。堕落值＞-3：变奏。"),
            ("DIVINE_STARDUST.description", "你每生成或打出7张附魔牌，获得2层魔力增幅。"),
        ):
            self.assertIn(expected, design)
            actual = loc["MAIDEN_SUCCUBUS_RELIC_" + suffix]
            self.assertEqual(re.sub(r"\[/?(?:gold|purple)\]", "", actual), expected)
            self.assertIn("[gold]", actual)

    def test_registration_rarity_hover_and_content_inventory(self):
        source = read("src/Relics/ReactiveMagicRelics.cs")
        contract = json.loads(read("docs/content_contract_20260824.json"))
        for name in ("CounterCurseMirror", "DivineStardust"):
            self.assertEqual(contract["relics"].count(name), 1)
            self.assertIn(f"[RegisterRelic(typeof(MSRelicPool))]\npublic sealed class {name}", source)
        self.assertEqual(len(contract["relics"]), 36)  # Includes the stored-curse relic.
        for token in ("RelicRarity.Rare", "RelicRarity.Uncommon", "FromPower<BurningPower>",
                      "FromPower<CondemnationPower>", "FromPower<MagicAmplificationPower>"):
            self.assertIn(token, source)

    def test_actual_debuff_change_and_shared_exception_safe_guard(self):
        source = read("src/Relics/ReactiveMagicRelics.cs").split("public sealed class DivineStardust", 1)[0]
        for token in ("AfterPowerAmountChanged", "ReferenceEquals(applier, self)", "target.Side != self.Side",
                      "ReferenceEquals(target.CombatState, self.CombatState)", "GetTypeForAmount(amount)",
                      "static readonly WeakInstanceScope<Creature>", "Reactions.Contains(self)",
                      "using (Reactions.Enter(self))", "await PowerCmd.Apply<BurningPower>",
                      "await PowerCmd.Apply<CondemnationPower>", "HasBeenRemovedFromState"):
            self.assertIn(token, source)
        self.assertNotIn("Task.Run", source)
        self.assertNotIn("_ =", source)

    def test_variation_reads_live_run_and_uses_safe_patch(self):
        source = read("src/Relics/ReactiveMagicRelics.cs")
        self.assertIn("ReactiveMagicRelicRules.MirrorVariation(CorruptionQuery.Get(run))", source)
        patch = read("src/Patches/TwinSoulChaliceDescriptionPatch.cs")
        for token in ("Safe.Run(", "CounterCurseMirror { HolyVariation: true }", 'mirror.Id.Entry + ".descriptionHoly"'):
            self.assertIn(token, patch)

    def test_generation_is_native_creator_attributed_not_pile_or_enchant_event(self):
        source = read("src/Relics/ReactiveMagicRelics.cs").split("public sealed class DivineStardust", 1)[1]
        for token in ("AfterCardGeneratedForCombat(CardModel card, Player? creator)", "ReferenceEquals(creator, Owner)",
                      "card.Enchantment == null", "ReferenceEquals(card.CombatState, Owner.Creature.CombatState)"):
            self.assertIn(token, source)
        for forbidden in ("AfterCardChangedPiles", "AfterCombatEnchantmentApplied", "AfterCardEnchanted", "IsLastInSeries"):
            self.assertNotIn(forbidden, source)

    def test_play_receipts_are_instance_local_consumed_once_and_cloned_independently(self):
        source = read("src/Relics/ReactiveMagicRelics.cs").split("public sealed class DivineStardust", 1)[1]
        for token in ("ConditionalWeakTable<CardPlay, PlayReceipt>", "BeforeCardPlayed(CardPlay cardPlay)",
                      "ReferenceEquals(play.Player, Owner)", "play.Card.Enchantment != null", "receipt.Eligible",
                      "receipt.Counted", "DeepCloneFields()", "base.DeepCloneFields()"):
            self.assertIn(token, source)
        self.assertLess(source.index("receipt.Counted = true"), source.index("return CountOne(context)"))
        end = source.split("public override Task AfterCombatEnd", 1)[1].split("public override Task AfterCardGenerated", 1)[0]
        self.assertIn("_plays = new()", end)
        self.assertNotIn("Progress =", end)
        for token in ("[SavedProperty]", "public int Progress", "ShowCounter => true", "DisplayAmount => Progress",
                      "Math.Clamp(value, 0, 6)", "InvokeDisplayAmountChanged()", "RelicStatus.Active"):
            self.assertIn(token, source)

    def test_pure_rules_are_linked_from_production(self):
        self.assertIn("../../src/Core/Relics/ReactiveMagicRelicRules.cs", read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        source = read("tests/DesignSyncContracts/Program.cs")
        for token in ("ReactiveMagicRelicRules.MirrorVariation", "ReactiveMagicRelicRules.ShouldReflect",
                      "ReactiveMagicRelicRules.NextStardust", "int[] stardustSequence = [1, 2, 3, 4, 5, 6, 0]"):
            self.assertIn(token, source)

    def test_real_game_suite_covers_effects_replay_generation_snapshot_and_save(self):
        source = read("src/ConsoleCommands/DesignReactiveRelicTestConsoleCmd.cs")
        for token in ('args[0] != "confirm"', "player.RunState.Players.Count != 1", "CombatManager.Instance.IsEnding",
                      "PowerCmd.Apply<ArtifactPower>", "PowerCmd.Apply<StrengthPower>", "PowerCmd.ModifyAmount",
                      "PowerCmd.Apply<BurningPower>", "PowerCmd.Apply<CondemnationPower>",
                      "CardPileCmd.AddGeneratedCardToCombat", "ApplyVanilla<Glam>", "ctx.Play(replay)",
                      "ctx.Create<LightWings>", "expiring.ClearEnchantmentInternal()", "Receipt(layered, foreign)",
                      "RelicModel.FromSerializable", "dust.AfterCombatEnd", "foreignMirror", "foreignDust",
                      "TestMode.IsOn = previousTestMode", "finally"):
            self.assertIn(token, source)


if __name__ == "__main__":
    unittest.main()
