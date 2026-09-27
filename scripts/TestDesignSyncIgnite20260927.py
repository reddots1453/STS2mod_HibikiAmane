"""Ignite identity regression wiring; real execution requires ms_test_cards confirm Ignite."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model


def power():
    code = read("src/Powers/MvpExhaustPowers2.cs")
    return code[code.index("public sealed class IgnitePower"):code.index("public sealed class ChainDestructionPower")]


class IgniteContracts(unittest.TestCase):
    def test_full_design_entry_and_formatted_template(self):
        design = re.sub(r"\s", "", read("DesignDoc.md"))
        self.assertIn("引燃技能牌罕见1/0费消耗1张牌。下回合开始时将其打出。", design)
        loc = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        self.assertEqual("[gold]消耗[/gold]1张牌。\n下回合开始时将其打出。",
                         loc["MAIDEN_SUCCUBUS_CARD_IGNITE.description"])

    def test_existing_metadata_remains_correct(self):
        code = model("Ignite")
        self.assertIn("base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)", code)
        self.assertIn("EnergyCost.UpgradeBy(-1)", code)
        self.assertIn("card => card != this", code)
        self.assertIn("if (selected == null) return", code)

    def test_schedule_real_selected_instance_after_actual_exhaust(self):
        code = model("Ignite")
        self.assertLess(code.index("await CardCmd.Exhaust(context, selected)"), code.index("power?.SetSelectedCard(selected)"))
        self.assertNotIn("power.CardId =", code)

    def test_each_application_is_instanced_like_vanilla_nightmare(self):
        code = power()
        self.assertIn("public override PowerInstanceType InstanceType => PowerInstanceType.Instanced", code)
        self.assertIn("protected override object InitInternalData() => new Data()", code)
        self.assertIn("GetInternalData<Data>().SelectedCard = card", code)
        self.assertNotIn("CreateClone", code)
        self.assertNotIn("FirstOrDefault", code)
        self.assertNotIn("candidate.Id", code)

    def test_consumed_before_await_and_duplicate_callback_noop(self):
        code = power()
        self.assertIn("if (data.Resolved) return", code)
        self.assertLess(code.index("data.Resolved = true"), code.index("await PowerCmd.Remove"))
        self.assertLess(code.index("data.SelectedCard = null"), code.index("await PowerCmd.Remove"))
        self.assertLess(code.index("await PowerCmd.Remove"), code.index("await CardCmd.AutoPlay"))

    def test_owner_and_lifecycle_guards_keep_native_free_autoplay(self):
        code = power()
        for part in ("player.Creature != Owner", "card.HasBeenRemovedFromState", "card.Owner != player",
                     "card.CombatState != Owner.CombatState", "card.Pile?.IsCombatPile != true",
                     "CombatManager.Instance.IsOverOrEnding", "await CardCmd.AutoPlay(context, card, null)"):
            self.assertIn(part, code)
        self.assertNotIn("PileType.Exhaust.GetPile", code)
        self.assertNotIn("LoseEnergy", code)

    def test_engine_contract_checks_identity_duplicate_casts_and_movement(self):
        code = read("src/Debugging/CardEffects/DesignSyncIgniteContract.cs")
        for part in ("selected.DynamicVars.Damage.BaseValue = 17", "selected seventeen damage not same-id decoy",
                     "two casts produce two pending powers", "both selected cards autoplay exactly once",
                     "PileType.Draw, PileType.Hand, PileType.Discard", "CardCmd.Upgrade(moving)",
                     "await CardPileCmd.RemoveFromCombat(removed", "removed target is not replaced with same-id decoy",
                     "other player turn does not trigger", "duplicate callback cannot replay",
                     "no selectable card creates no pending power", "native unplayable handling completes"):
            self.assertIn(part, code)
        self.assertIn("CustomVariants<Ignite>(DesignSyncIgniteContract.Run, 35)",
                      read("src/Debugging/CardEffects/CardEffectTestCatalog.cs"))

    def test_legacy_metadata_is_not_a_fallback_lookup(self):
        code = power()
        self.assertIn("[SavedProperty] public string CardId", code)
        self.assertIn("[SavedProperty] public bool WasUpgraded", code)
        after = code[code.index("public override async Task AfterPlayerTurnStart"):]
        self.assertNotIn("CardId", after)
        self.assertNotIn("WasUpgraded", after)
        self.assertIn("missing reference does not guess by metadata",
                      read("src/Debugging/CardEffects/DesignSyncIgniteContract.cs"))


if __name__ == "__main__":
    unittest.main(verbosity=2)
