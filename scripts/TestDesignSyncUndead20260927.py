"""Static DesignDoc/text and runtime-test wiring contracts, not engine execution."""
import json
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


class UndeadContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.design = read("DesignDoc.md").split("#### 亡灵集会 `", 1)[1].split("#### 按摩店", 1)[0]
        cls.source = read("src/Events/UndeadGathering.cs")
        cls.runtime = read("src/ConsoleCommands/DesignUndeadEventContract.cs")
        cls.command = read("src/ConsoleCommands/DesignEventTestConsoleCmd.cs")
        cls.text = json.loads(read("MaidenSuccubus/localization/zhs/events.json"))
        cls.prefix = "MAIDEN_SUCCUBUS_EVENT_UNDEAD_GATHERING."

    def loc(self, key):
        return self.text[self.prefix + key]

    def test_initial_narrative_exact_design_paragraphs_and_colors(self):
        section = self.design.split("**初始页面描述：**", 1)[1].split("**选项：**", 1)[0]
        expected = "\n\n".join(re.findall(r"^> (.+)$", section, re.M))
        self.assertEqual(self.loc("title"), "亡灵集会")
        self.assertEqual(self.loc("pages.INITIAL.description"), expected)
        # Independent literal in runtime check must match exact design too.
        literal = re.search(r'InitialDescription.GetRawText\(\) == ("(?:[^"\\]|\\.)*")', self.runtime)
        self.assertEqual(json.loads(literal[1]), expected)

    def test_all_result_pages_exact_design_and_runtime_literals(self):
        expected = re.findall(r"  - 结算页面描述：(.+)", self.design)
        self.assertEqual(len(expected), 3)
        for key, value in zip(("LISTEN", "PRAY", "LEARN"), expected):
            value = value.replace(r"\n", "\n")
            self.assertEqual(self.loc(f"pages.{key}.description"), value)
            literal = re.search(rf'"{key}" => ("(?:[^"\\]|\\.)*")', self.runtime)
            self.assertEqual(json.loads(literal[1]), value)

    def test_all_option_words_and_locks(self):
        for key, name in (("LISTEN", "与灵魂对话"), ("PRAY", "祈祷"), ("LEARN", "窥探死灵秘术")):
            self.assertIn(f"- `{name}`", self.design)
            self.assertEqual(self.loc(f"pages.INITIAL.options.{key}.title"), name)
        effects = re.findall(r"  - 效果说明：(.+)", self.design)
        for key, effect in zip(("LISTEN", "PRAY", "LEARN"), effects):
            # Only established resource/template and enchantment-style adaptations.
            effect = effect.replace("“灵魂”", "灵魂").replace("[10%最大生命值]", "{HpLoss}")
            effect = effect.replace("附魔“死灵”", "附魔：死灵").replace("“凡庸”", "凡庸")
            localized = re.sub(r"\[/?(?:gold|purple)\]", "", self.loc(f"pages.INITIAL.options.{key}.description"))
            self.assertEqual(localized, effect)
        self.assertEqual(self.loc("pages.INITIAL.options.LEARN_LOCKED.title"),
                         re.search(r"锁定标题：`(.+?)`", self.design)[1])
        hint = re.search(r"锁定提示：(.+)", self.design)[1]
        self.assertEqual(self.loc("pages.INITIAL.options.LEARN_LOCKED.description"), hint)
        self.assertEqual(self.loc("pages.INITIAL.options.LEARN_NO_CARD.description"), hint)

    def test_production_keeps_native_selection_damage_and_scope(self):
        for token in ("runState.CurrentActIndex is 1 or 2",
                      "runState.Players.All(player => player.Character is MaidenSuccubusCharacter)",
                      "Math.Max(0, maxHp / 10)", "DynamicVars.AddTo(option.Description)",
                      "ValueProp.Unblockable | ValueProp.Unpowered", "if (Owner.Creature.IsDead) return;",
                      "CardSelectCmd.FromDeckForTransformation(", "CardSelectorPrefs.TransformSelectionPrompt, 2",
                      "CardSelectCmd.FromDeckForEnchantment(", "ModelDb.Enchantment<NecromancyEnchantment>().CanEnchant",
                      "Rng.NextItem(pool)", "CardPreviewStyle.EventLayout"):
            self.assertIn(token, self.source)
        self.assertNotIn("CreatureCmd.LoseMaxHp(", self.source)
        self.assertIn("扣除当前生命，不降低最大生命值", self.design)
        self.assertIn("只能选择符合附魔通用规则", self.design)

    def test_runtime_independent_expectations_and_real_commands(self):
        for token in ("maximum, expected", "(9, 0), (10, 1), (19, 1)",
                      "UndeadGathering.PrayerHpLoss(maximum) == expected",
                      "corruption = -5; corruption <= 5; corruption++", "new[] { 0, 1, 2, 4 }",
                      "await model.BeginEvent(player, null, false)", "await prayer.Chosen()",
                      "player.Creature.CurrentHp == 88 && player.Creature.MaxHp == 97",
                      "CorruptionQuery.Get(run) == -5", "CorruptionQuery.Get(run) == 5",
                      "originals.Except(selected)", "targets[1].Enchantment == null",
                      "OfType<Normality>().Count() == 1", "Finished(model, \"PRAY\")",
                      "Finished(listen, \"LISTEN\")", "Finished(learn, \"LEARN\")",
                      "model.CurrentOptions.Count == 0", "soul rewards have independent identities"):
            self.assertIn(token, self.runtime)

    def test_pending_choice_cleanup_and_rng_isolation(self):
        for token in ("selector.SetupForAsyncCardSelection()", "using (CardSelectCmd.UseSelector(selector))",
                      "Task action = prayer.Chosen()", "!action.IsCompleted && !model.IsFinished",
                      "duplicate click cannot transform while waiting", "new Rng(model.Rng.ToSerializable())",
                      "expectedRng.NextItem(pool)", "SequenceEqual(expectedIds)",
                      "model.Rng.ToSerializable().ToString() == expectedRng.ToSerializable().ToString()",
                      "player.PlayerRng.Rewards.ToSerializable().ToString() == rewardsBefore"):
            self.assertIn(token, self.runtime)
        self.assertRegex(self.runtime, r"finally\s*\{\s*pending.TrySetResult\(selected\);\s*await action;\s*selector.Cleanup\(\);")

    def test_guarded_command_and_test_mode_restoration(self):
        for token in ('args[0] != "confirm"', "CombatManager.Instance.IsInProgress",
                      "issuingPlayer.RunState.Players.Count != 1", "if (_running)",
                      "bool previousTestMode = TestMode.IsOn", "TestMode.IsOn = true",
                      "await DesignUndeadEventContract.Run(player, Check)",
                      "finally { TestMode.IsOn = previousTestMode; _running = false; }"):
            self.assertIn(token, self.command)
        self.assertTrue(self.runtime.startswith("#if DEBUG"))


if __name__ == "__main__":
    unittest.main()
