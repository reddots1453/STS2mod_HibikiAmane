"""DS27 incremental static contracts, not substitutes for in-game effect tests.

Run: python scripts/TestDesignSync20260927.py
Runtime counterparts: CardEffectTestCatalog (Shift+F10) and ms_test_events confirm.
Descriptions are compared exactly, including punctuation; no line-number parsing.
"""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


def card_class(path, name):
    source = read(path)
    # Stop at the next class rather than matching a keyword elsewhere in the file.
    start = source.index(f"public sealed class {name} :")
    tail = source[start:]
    next_class = re.search(r"\n(?:\[RegisterCard[^\n]*\]\s*)?public sealed class ", tail[1:])
    return tail[:next_class.start() + 1] if next_class else tail


class DesignSyncBatchOne(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.cards = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        cls.powers = json.loads(read("MaidenSuccubus/localization/zhs/powers.json"))

    def test_recollection_exact_card_text(self):
        self.assertEqual(self.cards["MAIDEN_SUCCUBUS_CARD_RECOLLECTION_ROOM.description"],
                         "回合开始时额外抽1张牌，并优先从[gold]消耗牌堆[/gold]抽牌。")

    def test_recollection_power_stacking_text(self):
        expected = ("回合开始时额外抽{Amount}张牌，并优先从[gold]消耗牌堆[/gold]抽牌；"
                    "不足部分按正常规则从[gold]抽牌堆[/gold]补足。")
        for suffix in ("description", "smartDescription"):
            self.assertEqual(self.powers[f"MAIDEN_SUCCUBUS_POWER_RECOLLECTION_ROOM_POWER.{suffix}"],
                             expected.replace("{Amount}", "1") if suffix == "description" else expected)

    def test_recollection_metadata(self):
        source = card_class("src/Cards/Iteration1CorruptCards.cs", "RecollectionRoom")
        self.assertIn("context, Owner.Creature, 1, Owner.Creature, this", source)
        self.assertIn("AddKeyword(CardKeyword.Innate)", source)
        self.assertNotIn("CardKeyword.Retain", source)

    def test_conversion_no_self_exhaust(self):
        source = card_class("src/Cards/Iteration1CorruptCards.cs", "MiasmaConversion")
        self.assertNotIn("CardKeyword.Exhaust", source)
        self.assertIn("base(1, CardType.Skill, CardRarity.Uncommon", source)
        self.assertIn("EnergyCost.UpgradeBy(-1)", source)

    def test_soul_choice_allows_skip(self):
        source = card_class("src/Cards/Iteration2ExpansionCards.cs", "SoulFuenika")
        self.assertIn("canSkip: true", source)
        self.assertIn("if (selected != null)", source)
        self.assertLess(source.index("PendingPostCombatCards.Clear()"), source.index("foreach (SerializableCard"))

    def test_soul_exact_text(self):
        self.assertEqual(self.cards["MAIDEN_SUCCUBUS_CARD_SOUL_FUENIKA.description"],
                         "从3张[gold]圣洁[/gold]牌中选择一张加入[gold]手牌[/gold]。\n战斗结束后，将那张牌的复制加入牌组。")

    def test_familiar_upgrade_before_visual_and_enchantment(self):
        source = card_class("src/Cards/Iteration2ExpansionCards.cs", "FamiliarContract")
        self.assertIn("if (IsUpgraded)", source)
        self.assertLess(source.index("CardCmd.Upgrade(card)"), source.index("AddGeneratedCardToCombat"))
        self.assertLess(source.index("AddGeneratedCardToCombat"), source.index("Apply<FamiliarEnchantment>"))
        self.assertEqual(self.cards["MAIDEN_SUCCUBUS_CARD_FAMILIAR_CONTRACT.description"],
                         "将X张{IfUpgraded:show:升级过的|}随机[gold]圣洁[/gold]牌加入[gold]手牌[/gold]。\n为这些牌[gold]附魔[/gold]：[purple]使魔[/purple]。")

    def test_library_identity_and_cost(self):
        self.assertEqual(self.cards["MAIDEN_SUCCUBUS_CARD_INSATIABLE_GREED.title"], "娅露丝的书库")
        self.assertEqual(self.cards["MAIDEN_SUCCUBUS_CARD_INSATIABLE_GREED.description"],
                         "你不能抽牌。\n回合开始时，选择一个牌堆，随机打出其中的10张牌。")
        source = card_class("src/Cards/Iteration1CorruptCards.cs", "InsatiableGreed")
        self.assertIn("base(2, CardType.Power, CardRarity.Ancient", source)
        self.assertIn("AddKeyword(CardKeyword.Retain)", source)
        self.assertNotIn("SecondaryCosts", source)

    def test_ancient_integrations_registered(self):
        self.assertIn("RegisterDustyTomeCard(typeof(MaidenSuccubus.Characters.MaidenSuccubusCharacter))",
                      read("src/Cards/Iteration1CorruptCards.cs"))
        self.assertIn("RegisterArchaicToothTranscendence(typeof(DarkOrigin))", read("src/Cards/MvpBasicCards.cs"))
        self.assertIn("base(CardRarity.Ancient, 8, 4)", card_class("src/Cards/MvpBasicCards.cs", "DarkOrigin"))
        self.assertEqual(self.cards["MAIDEN_SUCCUBUS_CARD_DARK_ORIGIN.description"],
                         self.cards["MAIDEN_SUCCUBUS_CARD_DARK_ELEMENT.description"])

    def test_library_pile_choices_and_draw_ban(self):
        source = read("src/Powers/YarusLibraryPower.cs")
        self.assertIn("PileType.Draw, PileType.Discard, PileType.Exhaust", source)
        self.assertIn("cards.StableShuffle(player.RunState.Rng.CombatCardGeneration)", source)
        self.assertIn("cards.Take(10)", source)
        self.assertIn("ShouldDraw(Player player, bool fromHandDraw) => !IsActive || player.Creature != Owner", source)

    def test_library_choice_title_uses_dynamic_contributor(self):
        source = read("src/Cards/LibraryPileChoice.cs")
        self.assertIn("ICardTitleContributor", source)
        self.assertIn("CardTitleFragmentPlacement.ReplaceBase", source)
        self.assertEqual(self.cards["MAIDEN_SUCCUBUS_CARD_LIBRARY_PILE_CHOICE.title"], "选择牌堆")
        self.assertEqual(self.cards["MAIDEN_SUCCUBUS_CARD_LIBRARY_PILE_CHOICE.pileTitle"], "{PileName}")

    def test_event_hp_cost_text(self):
        events = json.loads(read("MaidenSuccubus/localization/zhs/events.json"))
        self.assertEqual(events["MAIDEN_SUCCUBUS_EVENT_UNDEAD_GATHERING.pages.INITIAL.options.PRAY.description"],
                         "失去{HpLoss}点生命值。选择2张牌，将其变化为随机[gold]圣洁[/gold]牌。失去1点[purple]堕落值[/purple]。")
        source = read("src/Events/UndeadGathering.cs")
        self.assertIn("DynamicVars.AddTo(option.Description)", source)
        self.assertIn("DynamicVars.HpLoss.IntValue, ValueProp.Unblockable | ValueProp.Unpowered", source)
        self.assertNotIn("CreatureCmd.LoseMaxHp(", source)

    def test_event_scope_and_unlock(self):
        source = read("src/Events/UndeadGathering.cs")
        self.assertIn("runState.CurrentActIndex is 1 or 2", source)
        self.assertIn("runState.Players.All(player => player.Character is MaidenSuccubusCharacter)", source)
        self.assertIn("Corruption >= 3 && canEnchant ? Learn : null", source)
        self.assertIn("[RegisterSharedEvent]", source)

    def test_event_runtime_tests_require_confirmation(self):
        source = read("src/ConsoleCommands/DesignEventTestConsoleCmd.cs")
        self.assertIn('args[0] != "confirm"', source)
        self.assertIn("CombatManager.Instance.IsInProgress", source)
        self.assertIn("prayer.Chosen()", source)
        self.assertIn("maximum hp unchanged", source)


if __name__ == "__main__":
    unittest.main(verbosity=2)
