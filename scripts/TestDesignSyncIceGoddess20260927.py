"""Ice Goddess source/text contracts; actual game effects use the existing card runner."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model


def power():
    return read("src/Powers/Iteration1NeutralPowers.cs").split("public sealed class GoddessOfIcePower", 1)[1].split("public interface IEscapeCard", 1)[0]


class IceGoddessContracts(unittest.TestCase):
    def test_design_and_card_metadata_unchanged(self):
        design = re.sub(r"\s", "", read("DesignDoc.md"))
        self.assertIn("冰界的女神能力牌稀有1费每当你打出附魔牌时，将1张冰晶碎片/冰晶碎片+加入手牌。", design)
        card = model("GoddessOfIce")
        self.assertIn("base(1, CardType.Power, CardRarity.Rare, TargetType.Self)", card)
        self.assertIn("IsUpgraded ? 2 : 1", card)
        self.assertIn("FromCard<IceShard>(IsUpgraded)", card)
        self.assertIn("PowerStackType.Single", power())

    def test_exact_base_upgrade_descriptions_and_gold_derivative_names(self):
        cards = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        self.assertEqual(cards["MAIDEN_SUCCUBUS_CARD_GODDESS_OF_ICE.description"],
                         "每当你打出[gold]附魔牌[/gold]时，将1张[gold]{IfUpgraded:show:冰晶碎片+|冰晶碎片}[/gold]加入[gold]手牌[/gold]。")
        loc = json.loads(read("MaidenSuccubus/localization/zhs/powers.json"))
        for upgraded in (False, True):
            expected = "每当你打出[gold]附魔牌[/gold]时，将1张[gold]冰晶碎片" + ("+" if upgraded else "") + "[/gold]加入[gold]手牌[/gold]。"
            for key in ("description", "smartDescription"):
                self.assertEqual(loc["MAIDEN_SUCCUBUS_POWER_GODDESS_OF_ICE_POWER." + key + ("Upgraded" if upgraded else "")], expected)
        for token in ('Amount >= 2 ? ".descriptionUpgraded"', 'Amount >= 2 ? ".smartDescriptionUpgraded"',
                      "AdditionalHoverTips", "FromCard<IceShard>(Amount >= 2)"):
            self.assertIn(token, power())

    def test_each_play_uses_actual_player_and_captured_enchantment(self):
        source = power()
        self.assertNotIn("IsLastInSeries", source)
        self.assertIn("ConditionalWeakTable<CardPlay, Receipt>", source)
        self.assertIn("ReferenceEquals(p.Player, Owner.Player)", source)
        self.assertIn("ReferenceEquals(p.Card.CombatState, Owner.CombatState)", source)
        self.assertIn("CapturePlay(cardPlay, cardPlay.Card.Enchantment != null)", source)
        after = source.split("public override async Task AfterCardPlayed", 1)[1]
        self.assertNotIn(".Enchantment", after)
        self.assertIn("!receipt.Eligible || receipt.Consumed", after)
        self.assertLess(after.index("receipt.Consumed = true"), after.index("await CardPileCmd.AddGeneratedCardToCombat"))

    def test_power_lifecycle_and_clone_receipt_isolation(self):
        source = power()
        for token in ("Owner is not { Player: not null, CombatState: not null, IsAlive: true }",
                      "ReferenceEquals(Owner.GetPower<GoddessOfIcePower>(), this)", "AfterRemoved(Creature oldOwner)",
                      "DeepCloneFields()", "base.DeepCloneFields()"):
            self.assertIn(token, source)
        self.assertEqual(source.count("_plays = new();"), 3)
        self.assertNotIn("static ConditionalWeakTable", source)

    def test_self_enchanted_power_card_not_lost_when_power_is_new(self):
        card = model("GoddessOfIce")
        self.assertLess(card.index("bool enchanted = Enchantment != null"), card.index("await PowerCmd.Apply<GoddessOfIcePower>"))
        self.assertIn("power?.CapturePlay(play, enchanted)", card)
        self.assertIn("_plays.GetValue(play", power())

    def test_native_creation_and_overflow_retained(self):
        source = power()
        for token in ("Owner.CombatState.CreateCard", "ModelDb.Card<IceShard>()", "if (Amount >= 2)",
                      "CardCmd.Upgrade(shard)", "shard, PileType.Hand, Owner.Player"):
            self.assertIn(token, source)
        self.assertEqual(source.count("AddGeneratedCardToCombat"), 1)
        for forbidden in ("Task.Run", "_ =", "CardPileCmd.Draw"):
            self.assertNotIn(forbidden, source)

    def test_game_contract_is_wired_and_uses_real_commands(self):
        self.assertIn("CustomVariants<GoddessOfIce>(DesignSyncIceGoddessContract.Run, 20)",
                      read("src/Debugging/CardEffects/CardEffectTestCatalog.cs"))
        source = read("src/Debugging/CardEffects/DesignSyncIceGoddessContract.cs")
        for token in ("RunState.CreateCard<GoddessOfIce>", "PileType.Deck", "GetDescriptionForPile(PileType.Hand)",
                      "power.Description.GetFormattedText()", "power.SmartDescription.GetFormattedText()",
                      "ApplyVanilla<Glam>", "ctx.Play(replay)", "ctx.Add<LightWings>",
                      "expiring.ClearEnchantmentInternal()", "Receipt(enchanted, foreign)",
                      "PowerCmd.Remove(power)", "ClonePreservingMutability()", "ctx.Play(selfEnchanted)",
                      "AddFillerCards(PileType.Hand, 10)", "Shards(PileType.Discard)"):
            self.assertIn(token, source)


if __name__ == "__main__":
    unittest.main()
