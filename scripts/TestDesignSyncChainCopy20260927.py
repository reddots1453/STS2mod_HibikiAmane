"""DS27-02R design/text drift and executable game-regression wiring.

Static checks do not execute Godot or prove natural turn/VFX behavior.
"""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model


class ChainCopyContracts(unittest.TestCase):
    def test_exact_design_entries(self):
        design = re.sub(r"\s", "", read("DesignDoc.md"))
        for expected in (
            "唤雷攻击牌普通1费造成7/9点伤害。斩杀时和魔力解放：对生命值最低的敌人造成7/9点伤害。",
            "终极耀斑攻击牌罕见4费对所有敌人造成40/52点伤害。回合结束时，如果这张牌在你的手牌中，本场战斗中耗能降低1。",
            "瞬闪刺攻击牌罕见0费造成5/7点伤害。将一张复制加入抽牌堆。",
        ):
            self.assertIn(expected, design)

    def test_full_localization_templates(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        expected = {
            "SUMMON_THUNDER": "造成{Damage:diff()}点伤害。\n[gold]斩杀时[/gold]和[gold]魔力解放[/gold]：对生命值最低的敌人造成{Damage:diff()}点伤害。",
            "ULTIMATE_FLARE": "对所有敌人造成{Damage:diff()}点伤害。\n回合结束时，如果这张牌在你的[gold]手牌[/gold]中，本场战斗中耗能降低1。",
            "FLASH_STAB": "造成{Damage:diff()}点伤害。\n将一张复制加入[gold]抽牌堆[/gold]。",
        }
        for name, text in expected.items():
            self.assertEqual(loc["MAIDEN_SUCCUBUS_CARD_" + name + ".description"], text)

    def test_thunder_uses_native_fatal_eligibility_and_pending_hits(self):
        source = model("SummonThunder")
        self.assertEqual(source.count("ShouldOwnerDeathTriggerFatal()"), 2)
        for token in ("TransformationCmd.PayOverdraft", "released ? 1 : 0", "pending-- > 0",
                      "OrderBy(enemy => enemy.CurrentHp)", "pending +=", "WasTargetKilled"):
            self.assertIn(token, source)
        self.assertLess(source.index("bool initialFatalEligible"), source.index("var initial = await"))
        self.assertLess(source.index("bool fatalEligible"), source.index("var followUp = await"))

    def test_flare_and_flash_retain_native_lifecycle(self):
        flare = model("UltimateFlare")
        for token in ("HasTurnEndInHandEffect => true", "OnTurnEndInHand", "EnergyCost.AddThisCombat(-1)",
                      "new DamageVar(40", "UpgradeValueBy(12)", "TargetingAllOpponents"):
            self.assertIn(token, flare)
        # The native dispatcher moves the selected hand card to Play before callback.
        self.assertNotIn("Pile?.Type == PileType.Hand", flare)
        flash = model("FlashStab")
        for token in ("CardModel copy = CreateClone()", "CardCmd.PreviewCardPileAdd(",
                      "await CardPileCmd.AddGeneratedCardToCombat(", "PileType.Draw, Owner, CardPilePosition.Random", "2.2f"):
            self.assertIn(token, flash)

    def test_suite_is_wired_to_existing_catalog(self):
        catalog = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        for card, method, count in (("SummonThunder", "Thunder", 33), ("UltimateFlare", "Flare", 25), ("FlashStab", "Flash", 17)):
            self.assertIn(f"CustomVariants<{card}>(DesignSyncChainCopyContract.{method}, {count})", catalog)
        runner = read("src/Debugging/CardEffects/CardEffectTestRunner.cs")
        for token in ('"ds27-chain-copy"', "batch.Length != 3", "DesignSyncChainCopyContract.Validate(context, card, scenario.Upgraded)"):
            self.assertIn(token, runner)
        gate = read("scripts/ValidateCardEffectTests.ps1")
        for token in ("DesignSyncChainCopyContract.cs", "$chainCopyContract -notmatch", "combined triggers and native minion exclusions"):
            self.assertIn(token, gate)

    def test_thunder_real_command_scenarios(self):
        source = read("src/Debugging/CardEffects/DesignSyncChainCopyContract.cs")
        for token in ('"plain", "release", "decline", "chain", "chain+release", "initial-minion", "followup-minion"',
                      "CreatureCmd.Add<Byrdonis>", "CreatureCmd.SetMaxAndCurrentHp", "ctx.Play(ctx.Create<SummonThunder>(upgraded)",
                      "ApplyPower<MinionPower>(initial", "ApplyPower<MinionPower>(minion", "damage * sinkHits", "finally", "CreatureCmd.Escape"):
            self.assertIn(token, source)

    def test_flare_and_flash_real_command_scenarios(self):
        source = read("src/Debugging/CardEffects/DesignSyncChainCopyContract.cs")
        for token in ("GetDescriptionForPile(PileType.Deck)", "GetDescriptionForPile(PileType.Hand)",
                      "turn <= 5", "combat.OnTurnEndInHandWrapper(choice)", "combat.EnergyCost.EndOfTurnCleanup()",
                      "Math.Max(0, 4 - turn)", "freshCombatCopy", "RemoveFromDeck(deck",
                      "ApplyVanilla<Sharp>(card, 2)", "ApplyPower<StrengthPower>(ctx.Self, 3)",
                      "card.ExhaustOnNextPlay = true", "!copy.ExhaustOnNextPlay", "ReferenceEquals(copy.CloneOf, card)",
                      "!ReferenceEquals(copy.Enchantment, card.Enchantment)", "ReferenceEquals(descendant.CloneOf, copy)",
                      "ctx.Play(copy, ctx.PrimaryEnemy)", "combat copying does not add permanent cards"):
            self.assertIn(token, source)


if __name__ == "__main__":
    unittest.main()
