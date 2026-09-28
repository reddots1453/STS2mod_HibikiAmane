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
        self.assertEqual(len(self.cards), 227)
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

    def test_actual_forwarded_status_keywords_match_independent_design_expectations(self):
        expectations = {
            "BarbedHookStatus": ("倒刺钩", ["Exhaust"]),
            "ClothingBurnStatus": ("衣物燃烧", ["Ethereal", "Exhaust"]),
            "BitingPaperStatus": ("咬衣纸片", ["Exhaust"]),
            "DissolvingFluidStatus": ("溶解液", ["Exhaust", "Retain"]),
        }
        for name, (title, keywords) in expectations.items():
            with self.subTest(name=name):
                data = audit.source_metadata(name, self.classes)
                design = audit.design_metadata(title, self.lines)
                self.assertEqual(data["keywords"], keywords)
                self.assertEqual(design["baseKeywords"], keywords)
                self.assertEqual((data["cost"], data["upgradedCost"], data["type"], data["rarity"], data["target"]),
                                 (1, 1, "Status", "Status", "Self"))
                self.assertEqual(data["unresolvedFields"], [])

    def test_no_write_and_strict_review_refuses_false_completion(self):
        # Running the real audit must not touch another agent's report. A strict
        # review cannot go green merely because a similarity score is high.
        output = io.StringIO()
        with patch.object(Path, "write_text", side_effect=AssertionError("unexpected write")), \
                contextlib.redirect_stdout(output):
            self.assertEqual(audit.main(["--no-write", "--strict-review"]), 1)
        self.assertIn("full review incomplete", output.getvalue())
        self.assertIn("audited=227", output.getvalue())


class ForwardedKeywordTests(unittest.TestCase):
    BASE = """{
        protected Base(IEnumerable<CardKeyword> keywords)
            : base(1, CardType.Status, CardRarity.Status, TargetType.Self)
        { _keywords = keywords.ToArray(); }
        private readonly IReadOnlyList<CardKeyword> _keywords;
        public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;
    }"""

    def metadata(self, expression, base=None, extra="", leaf_body=""):
        leaf = "{ public Leaf() : base(" + expression + ") { " + leaf_body + " } " + extra + " }"
        return SourceTests().source(leaf, self.BASE if base is None else base)

    def test_single_multiple_and_empty_literal_collections(self):
        for expression, expected in (("[CardKeyword.Exhaust]", ["Exhaust"]),
                                     ("[CardKeyword.Retain, CardKeyword.Exhaust,]", ["Exhaust", "Retain"]),
                                     ("[]", [])):
            with self.subTest(expression=expression):
                result = self.metadata(expression)
                self.assertEqual(result["keywords"], expected)
                self.assertEqual(result["unresolvedFields"], [])

    def test_argument_separator_preserves_collection_and_nested_commas(self):
        self.assertEqual(audit.split_source_arguments('[A, B], Call(1, 2), new[] {3, 4}, "x,y"'),
                         ['[A, B]', 'Call(1, 2)', 'new[] {3, 4}', '"x,y"'])
        for text in ('[A, B', 'A], B', '[A), B]'):
            self.assertIsNone(audit.split_source_arguments(text))

    def test_literal_parser_rejects_dynamic_spread_and_strings_instead_of_partial_success(self):
        for expression in ('[CardKeyword.Exhaust, Extra]', '[..OtherKeywords]',
                           '[CardKeyword.Exhaust, Compute()]', '["CardKeyword.Exhaust"]',
                           '[CardKeyword.Exhaust, Flag ? CardKeyword.Retain : CardKeyword.Ethereal]',
                           '[CardKeyword.Exhaust].Concat([CardKeyword.Retain])'):
            with self.subTest(expression=expression):
                self.assertIsNone(audit.literal_keywords(expression))
                result = self.metadata(expression)
                self.assertIn("keywords", result["unresolvedFields"])
        self.assertEqual(audit.literal_keywords('[PortableKeyword.Value, SinkingKeyword.Value, CardKeyword.Exhaust]'),
                         ['Exhaust', 'PortableKeyword', 'SinkingKeyword'])

    def test_direct_getter_dynamic_values_also_remain_unknown(self):
        for expression in ('[CardKeyword.Exhaust, Extra]', '["x"]', 'Flag ? [CardKeyword.Exhaust] : []'):
            base = self.BASE.replace('CanonicalKeywords => _keywords;', 'CanonicalKeywords => ' + expression + ';')
            self.assertIn("keywords", self.metadata('[]', base)["unresolvedFields"])

    def test_known_fully_qualified_enums_and_keyword_values(self):
        result = audit.literal_keywords('[MegaCrit.Sts2.Core.Entities.Cards.CardKeyword.Exhaust, MaidenSuccubus.Keywords.SinkingKeyword.Value]')
        self.assertEqual(result, ['Exhaust', 'SinkingKeyword'])
        self.assertIsNone(audit.literal_keywords('[Unknown.CardKeyword.Exhaust]'))
        self.assertIsNone(audit.literal_keywords('[Unknown.SinkingKeyword.Value]'))

    def test_conditional_assignment_mutability_and_field_escape_are_not_certified(self):
        changes = (
            ('{ _keywords = keywords.ToArray(); }', '{ if (Flag) _keywords = keywords.ToArray(); }'),
            ('private readonly', 'private'),
            ('private readonly', 'public readonly'),
            ('_keywords = keywords.ToArray();', '_keywords = Transform(keywords);'),
            ('_keywords = keywords.ToArray();', '_keywords = keywords.ToArray(); _keywords = [];'),
            ('CanonicalKeywords => _keywords;', 'CanonicalKeywords => _keywords; void Mutate() { _keywords[0] = CardKeyword.Retain; }'),
            ('CanonicalKeywords => _keywords;', 'CanonicalKeywords => _keywords; void Escape() { Use(_keywords); }'),
        )
        for old, new in changes:
            with self.subTest(new=new):
                self.assertIn("keywords", self.metadata('[CardKeyword.Exhaust]', self.BASE.replace(old, new))["unresolvedFields"])

    def test_forwarder_side_effects_and_overloaded_constructor_remain_unknown(self):
        result = self.metadata('[CardKeyword.Exhaust]', leaf_body='Configure();')
        self.assertIn("keywords", result["unresolvedFields"])
        result = self.metadata('[CardKeyword.Exhaust]', extra='public Leaf(int x) : base([]) {}')
        self.assertIn("keywords", result["unresolvedFields"])

    def test_local_getter_override_takes_precedence(self):
        result = self.metadata('[CardKeyword.Exhaust]', extra='public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];')
        self.assertEqual(result["keywords"], ["Retain"])
        self.assertEqual(result["unresolvedFields"], [])

    def test_multilevel_forwarding_and_comments_do_not_create_fake_keywords(self):
        path = audit.ROOT / 'src/Cards/AuditFixture.cs'
        classes = {
            'Leaf': ('{ public Leaf() : base([CardKeyword.Exhaust, CardKeyword.Retain]) {} }', 'Middle', path),
            'Middle': ('{ protected Middle(IEnumerable<CardKeyword> ks) : base(ks) {} /* CanonicalKeywords => [CardKeyword.Innate]; */ }', 'Base', path),
            'Base': (self.BASE, None, path),
        }
        result = audit.source_metadata('Leaf', classes)
        self.assertEqual(result['keywords'], ['Exhaust', 'Retain'])
        self.assertEqual(result['unresolvedFields'], [])


if __name__ == "__main__":
    unittest.main(verbosity=2)
