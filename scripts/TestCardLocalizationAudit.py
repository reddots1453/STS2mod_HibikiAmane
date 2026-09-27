"""Regression tests for the audit itself, not proof of card gameplay correctness."""
import contextlib
import io
import re
import unittest
from pathlib import Path
from unittest.mock import patch

import AuditCardLocalization as audit


class CatalogueTests(unittest.TestCase):
    def test_shifted_catalogue_beats_prose(self):
        lines = ["测试", "这里只是提及。"] + [""] * 2100 + [
            "## 二、卡池 `[CARD-POOL-001 · READY]`", "测试", "攻击牌 普通",
            "1费 造成6点伤害。", "", "## 三、遗物"]
        self.assertEqual(audit.design_index("测试", lines), 2103)
        self.assertEqual(audit.design_metadata("测试", lines)["type"], "攻击牌")

    def test_wrapped_derivatives(self):
        for prefix in ("（衍生卡：", "(衍生牌: ", "衍生牌    "):
            lines = [prefix + "测试", "技能牌 稀有", "1/0费 获得3点格挡。", "）"]
            self.assertEqual(audit.design_metadata("测试", lines)["upgradedCost"], "0")

    def test_numbered_entries_do_not_bleed(self):
        lines = ["1. 第一圣言", "技能牌 普通", "持续2回合。", "2. 第二圣言",
                 "技能牌 普通", "获得1费。"]
        self.assertEqual(audit.design_effect("第一圣言", lines), "持续2回合。")
        self.assertIsNone(audit.design_metadata("第一圣言", lines)["baseCost"])

    def test_multiline_effect_and_notes(self):
        lines = ["测试", "技能牌 普通", "1费 获得3点格挡，", "抽1张牌。", "**说明**"]
        self.assertEqual(audit.design_effect("测试", lines), "1费 获得3点格挡，\n抽1张牌。")

    def test_inline_card_beats_route_heading(self):
        lines = ["#### 谦逊", "第一试炼：升级1张初始牌。", "（谦逊", "技能牌 先古",
                 "1/0费 保留。消耗。）", "第二试炼：升级1张初始牌。"]
        self.assertEqual(audit.design_index("谦逊", lines), 2)
        self.assertNotIn("第二试炼", audit.design_context("谦逊", lines))

    def test_event_heading(self):
        lines = ["##### 心神宁静（事件衍生卡） `[EVENT-CARD-001 · READY]`", "",
                 "技能牌 无色 罕见", "0费 保留。抽2张牌。"]
        self.assertEqual(audit.design_metadata("心神宁静", lines)["rarity"], "罕见")

    def test_mentions_are_not_type_or_cost(self):
        lines = ["测试", "1费 你的技能牌额外耗能1点。"]
        self.assertIsNone(audit.design_metadata("测试", lines)["type"])
        lines = ["测试", "技能牌 普通", "获得2费。"]
        self.assertIsNone(audit.design_metadata("测试", lines)["baseCost"])

    def test_punctuation_and_layout_are_not_erased(self):
        normalize = audit.normalize_for_comparison
        self.assertNotEqual(normalize("造成6点伤害。抽1张牌。"), normalize("造成6点伤害，抽1张牌。"))
        self.assertNotEqual(normalize("造成6点伤害。\n抽1张牌。"), normalize("造成6点伤害。抽1张牌。"))
        self.assertNotEqual(normalize("消耗。"), normalize(""))

    def test_reverse_coverage_keeps_new_design_not_variant_header(self):
        lines = ["新牌", "技能牌 普通", "1费 抽1张牌。", "", "变奏后改为：", "技能牌 普通", "0费 抽1张牌。"]
        self.assertEqual(audit.typed_design_entries(lines), {"新牌": 0})


class SourceTests(unittest.TestCase):
    def source(self, leaf, base):
        path = audit.ROOT / "src/Cards/AuditFixture.cs"
        classes = {"Leaf": (leaf, "Base", path), "Base": (base, None, path)}
        return audit.source_metadata("Leaf", classes)

    def test_constructor_forwarding_and_inherited_upgrade_keywords(self):
        data = self.source("{ public Leaf() : base(2) {} }", """{
            protected Base(int cost) : base(cost, CardType.Skill, CardRarity.Rare, TargetType.Self) {}
            public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
            protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); AddKeyword(CardKeyword.Retain); }
        }""")
        self.assertEqual((data["cost"], data["upgradedCost"]), (2, 1))
        self.assertEqual(data["keywords"], ["Exhaust"])
        self.assertEqual(data["addsKeywords"], ["CardKeyword.Retain"])
        self.assertEqual(data["unresolvedFields"], [])

    def test_override_does_not_inherit_upgrade_unless_called(self):
        base = """{ protected Base(int cost) : base(cost, CardType.Skill, CardRarity.Common, TargetType.Self) {}
            protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); }"""
        leaf = "{ public Leaf() : base(2) {} protected override void OnUpgrade() { %s } }"
        self.assertEqual(self.source(leaf % "", base)["upgradedCost"], 2)
        self.assertEqual(self.source(leaf % "base.OnUpgrade();", base)["upgradedCost"], 1)

    def test_default_constructor_parameters(self):
        data = self.source("{}", """{ protected Base(int cost = 1)
            : base(cost, CardType.Skill, CardRarity.Rare, TargetType.Self) {} }""")
        self.assertEqual(data["cost"], 1)

    def test_unresolved_not_guessed(self):
        data = self.source("{ public Leaf() : base(Calculate()) {} }", """{
            protected Base(int cost) : base(cost, CardType.Skill, CardRarity.Rare, TargetType.Self) {}
        }""")
        self.assertIn("cost", data["unresolvedFields"])
        self.assertIsNone(data["cost"])

    def test_conditional_cost_upgrade_is_unknown(self):
        data = self.source("{ public Leaf() : base(2) {} }", """{
            protected Base(int cost) : base(cost, CardType.Skill, CardRarity.Rare, TargetType.Self) {}
            protected override void OnUpgrade() { if (Flag) EnergyCost.UpgradeBy(-1); }
        }""")
        self.assertIsNone(data["upgradedCost"])
        self.assertIn("upgradedCost", data["unresolvedFields"])

    def test_braces_in_strings_and_comments(self):
        text = 'class A { string s = "}"; /* } */ // }\n void M() {} } class B {}'
        block = audit.class_block(text, re.search(r"class A", text))
        self.assertIn("void M()", block)
        self.assertNotIn("class B", block)
        self.assertEqual(audit.class_block("class A : B; class C {}", re.search("class A", "class A : B; class C {}")), "")

    def test_attributes_and_generic_arity(self):
        text = """[RegisterCard(typeof(Pool))] [Other(typeof(Value))] public sealed class Leaf : Base<int> {}
        public abstract class Base<T> : Base {}
        public abstract class Base : External {}"""
        path = audit.ROOT / "src/Cards/AuditFixture.cs"
        with patch.object(Path, "rglob", return_value=[path]), patch.object(Path, "read_text", return_value=text):
            cards, classes = audit.registered_cards()
        self.assertIn("Leaf", cards)
        self.assertEqual(classes["Leaf"][1], "Base`1")
        self.assertEqual(classes["Base`1"][1], "Base")


class RepositoryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.lines = audit.DESIGN_PATH.read_text(encoding="utf-8-sig").splitlines()
        cls.cards, cls.classes = audit.registered_cards()

    def test_actual_catalogue_boundary_cases(self):
        for title in ("冰雾", "功性魔防壁II", "功性魔防壁III", "功性魔防壁IV", "困了", "心神宁静", "黑暗之源", "娅露丝的书库"):
            with self.subTest(title=title):
                self.assertIsNotNone(audit.design_index(title, self.lines))
        self.assertNotIn("轻灵圣言", audit.design_context("守护圣言", self.lines))
        self.assertNotIn("抽1张", audit.design_effect("惩戒圣言", self.lines))

    def test_actual_source_inheritance(self):
        self.assertEqual(len(self.cards), 225)
        for name, rarity, cost, upgrade in (("DarkOrigin", "Ancient", 0, 0),
                                            ("DarkElement", "Basic", 0, 0),
                                            ("CounterBarrierII", "Rare", 1, 0),
                                            ("CounterBarrierIII", "Rare", 1, 0)):
            with self.subTest(name=name):
                data = audit.source_metadata(name, self.classes)
                self.assertEqual((data["rarity"], data["cost"], data["upgradedCost"]), (rarity, cost, upgrade))
                self.assertEqual(data["unresolvedFields"], [])
        self.assertIn("ScriptureCardTemplate`1", self.classes)
        self.assertIn("ScriptureCardTemplate", self.classes)

    def test_no_write_and_strict_review_refuses_false_completion(self):
        # Running the real audit must not touch another agent's report. A strict
        # review cannot go green merely because a similarity score is high.
        output = io.StringIO()
        with patch.object(Path, "write_text", side_effect=AssertionError("unexpected write")), \
                contextlib.redirect_stdout(output):
            self.assertEqual(audit.main(["--no-write", "--strict-review"]), 1)
        self.assertIn("full review incomplete", output.getvalue())
        self.assertIn("audited=225", output.getvalue())


if __name__ == "__main__":
    unittest.main(verbosity=2)
