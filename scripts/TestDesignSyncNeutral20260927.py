"""DS27-02C static drift tests; real effects require ms_test_cards confirm ds27-neutral.

No generated audit report is overwritten. Text comparisons preserve punctuation,
line breaks and rich text. Design snippets are independent approved expectations.
"""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


def model(name):
    # Named declarations, including abstract/generic bases; never fixed line ranges.
    for path in (ROOT / "src/Cards").rglob("*.cs"):
        source = path.read_text(encoding="utf-8-sig")
        declarations = list(re.finditer(r"public (?:sealed |abstract )?class (\w+)", source))
        for index, match in enumerate(declarations):
            if match[1] == name:
                end = declarations[index + 1].start() if index + 1 < len(declarations) else len(source)
                return source[match.start():end]
    raise AssertionError("No card model " + name)


class NeutralDesignSync(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.cards = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))

    def test_design_snippets_have_not_drifted(self):
        # Strip whitespace ONLY: a changed comma, full stop, number or rank fails.
        design = re.sub(r"\s", "", read("DesignDoc.md"))
        approved = [
            "借力打击（是打击）攻击牌普通1费造成9/10点伤害。如果目标敌人的意图为攻击，获得1/2费。",
            "锻成·打击（是打击）攻击牌罕见1费造成6/9点伤害。选择1张打击附魔：本能。",
            "唤雷攻击牌普通1费造成7/9点伤害。斩杀时和魔力解放：对生命值最低的敌人造成7/9点伤害。",
            "妨碍射击攻击牌稀有2/1费造成3点伤害。如果目标不为攻击意图，将其击晕。消耗。",
            "梦幻之雾技能牌普通0费给予*所有人*2/3层虚弱。消耗。",
            "火焰绽放攻击牌普通1费造成8/11点伤害。给予1层燃烧。魔力解放：给予2层燃烧。",
            "碎冰斩攻击牌普通1费造成7点伤害。将1/2张冰晶碎片加入手牌。",
            "反伤屏障能力牌稀有1/0费获得2荆棘。获得2覆甲。将1张功性魔防壁II放入弃牌堆。沉底。",
            "功性魔防壁II能力牌稀有1/0费获得3荆棘。获得3覆甲。将1张功性魔防壁III放入弃牌堆。",
            "功性魔防壁III能力牌稀有1/0费获得5荆棘。获得5覆甲。将1张功性魔防壁IV放入弃牌堆。",
            "功性魔防壁IV能力牌稀有2/1费获得30荆棘。获得30覆甲。",
            "拖延技能牌普通0费选择一张牌，将其置于抽牌堆底部。抽2/3张牌。消耗。",
            "泡澡技能牌罕见1费获得2费。",
            "子守歌能力牌稀有2/1费回合结束时，将困了加入手牌。回合结束时，你每有1张手牌，获得2点格挡。",
        ]
        for text in approved:
            with self.subTest(text=text):
                self.assertIn(text, design)

    def test_exact_changed_card_text(self):
        expected = {
            "DREAM_MIST": "给予所有人{WeakPower:diff()}层[gold]虚弱[/gold]。",
            "FORGE_STRIKE": "造成{Damage:diff()}点伤害。\n选择1张[gold]打击[/gold][gold]附魔[/gold]：[purple]本能[/purple]。",
            "ICE_BREAKING_SLASH": "造成{Damage:diff()}点伤害。\n将{Cards:diff()}张[gold]冰晶碎片[/gold]加入[gold]手牌[/gold]。",
            "FLAME_BLOOM": "造成{Damage:diff()}点伤害。\n给予{BurningPower:diff()}层[gold]燃烧[/gold]。\n[gold]魔力解放[/gold]：给予2层[gold]燃烧[/gold]。",
            "OBSTRUCTING_SHOT": "造成{Damage:diff()}点伤害。\n如果目标不为攻击意图，将其[gold]击晕[/gold]。",
            "LULLABY": "回合结束时，将[gold]困了[/gold]加入[gold]手牌[/gold]。\n回合结束时，你每有1张[gold]手牌[/gold]，获得{BlockPerCard:diff()}点[gold]格挡[/gold]。",
        }
        for key, text in expected.items():
            with self.subTest(card=key):
                self.assertEqual(self.cards[f"MAIDEN_SUCCUBUS_CARD_{key}.description"], text)
        for suffix, next_stage in [("", "II"), ("_II", "III"), ("_III", "IV"), ("_IV", None)]:
            text = "获得{ThornsPower:diff()}[gold]荆棘[/gold]。\n获得{PlatingPower:diff()}[gold]覆甲[/gold]。"
            if next_stage:
                text += f"\n将1张[gold]功性魔防壁{next_stage}[/gold]放入[gold]弃牌堆[/gold]。"
            self.assertEqual(self.cards[f"MAIDEN_SUCCUBUS_CARD_COUNTER_BARRIER{suffix}.description"], text)

    def test_borrowed_force_upgrades_both_values(self):
        source = model("BorrowedForceStrike")
        self.assertIn("Damage.UpgradeValueBy(1)", source)
        self.assertIn("Energy.UpgradeValueBy(1)", source)

    def test_instinct_legality_and_hover(self):
        source = model("ForgeStrike")
        for fragment in ["CardRarity.Uncommon", "FromEnchantment<Instinct>", "Enchantment<Instinct>().CanEnchant(card)", "ApplyVanilla<Instinct>", "card.Tags.Contains(CardTag.Strike)"]:
            self.assertIn(fragment, source)
        self.assertNotIn("Glam", source)

    def test_dream_mist_affects_all_living_creatures_without_vulnerable(self):
        source = model("DreamMist")
        self.assertIn("base(0, CardType.Skill, CardRarity.Common", source)
        self.assertIn("CombatState.Creatures.Where(creature => creature.IsAlive)", source)
        self.assertNotIn("VulnerablePower", source)

    def test_shards_use_quantity_upgrade(self):
        source = model("IceBreakingSlash")
        self.assertIn("new CardsVar(1)", source)
        self.assertIn("i < DynamicVars.Cards.IntValue", source)
        self.assertIn("Cards.UpgradeValueBy(1)", source)
        self.assertNotIn("CardCmd.Upgrade(shard)", source)
        self.assertNotIn("FromCard<IceShard>(IsUpgraded)", source)

    def test_barrier_chain_cost_and_plating(self):
        for name in ["CounterBarrier", "CounterBarrierToken", "CounterBarrierIV"]:
            source = model(name)
            self.assertIn("PowerCmd.Apply<PlatingPower>", source)
            self.assertIn("EnergyCost.UpgradeBy(-1)", source)
        self.assertIn("new PowerVar<ThornsPower>(30)", model("CounterBarrierIV"))
        self.assertIn("new PowerVar<PlatingPower>(30)", model("CounterBarrierIV"))

    def test_runtime_contract_is_wired_not_only_registered(self):
        contract = read("src/Debugging/CardEffects/DesignSyncNeutralContract.cs")
        self.assertEqual(len(re.findall(r"new\(typeof\(", contract)), 14)
        runner = read("src/Debugging/CardEffects/CardEffectTestRunner.cs")
        self.assertIn("DesignSyncNeutralContract.Validate(context, card, scenario.Upgraded)", runner)
        self.assertIn('"ds27-neutral"', runner)
        catalog = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        for assertion in ["no legal strike finishes without applying enchantment", "release adds two more burning", "generated shards are not upgraded", "fourth stage cumulative plating"]:
            self.assertIn(assertion, catalog)


if __name__ == "__main__":
    unittest.main(verbosity=2)
