"""DS27-02M source wiring regressions, not claims of engine execution."""
import re
import unittest
from TestDesignSyncNeutral20260927 import model, read


class AllCurseBiteContracts(unittest.TestCase):
    def test_design_full_entry(self):
        text = re.sub(r"\s", "", read("DesignDoc.md"))
        self.assertIn("万咒之噬攻击牌稀有3/2费造成1点伤害。本场战斗中，每当有一张攻击牌被消耗时，将它的伤害数值增加到这张牌上。", text)
        self.assertIn("不论这张牌的位置是抽牌堆、弃牌堆、手牌、消耗堆甚至结算区，只要攻击牌被消耗都会计算", text)

    def test_calculated_damage_evaluates_growth_not_initial_base(self):
        code = read("src/Core/Powers/ExhaustDamageSnapshot.cs")
        self.assertIn("card.DynamicVars.CalculatedDamage.Calculate(null)", code)
        self.assertNotIn("CalculatedDamage.IntValue", code)
        self.assertIn('card.DynamicVars.ContainsKey("Damage")', code)
        self.assertIn('card.DynamicVars.ContainsKey("CalculatedDamage")', code)
        self.assertIn("if (card.Type != CardType.Attack) return 0", code)
        self.assertNotIn("PreviewValue", code)
        self.assertNotIn("ModifyDamage", code)

    def test_recipient_remains_same_owner_and_combat_only(self):
        code = model("AllCurseBite")
        for text in ("card.Owner != Owner", "Pile?.IsCombatPile != true", "CombatState == null",
                     "card.CombatState != CombatState", "ExhaustDamageSnapshot.Get(card)"):
            self.assertIn(text, code)
        self.assertNotIn("DeckVersion", code)

    def test_snapshot_before_listener_and_cleanup_after_async_completion(self):
        code = read("src/Core/Powers/ExhaustDamageSnapshot.cs")
        for text in ("nameof(Hook.AfterCardExhausted)", "private static void Prefix",
                     "ExhaustDamageSnapshot.Capture(__2)", "private static void Postfix",
                     "await original", "finally { scope.Dispose(); }", "Safe.Run",
                     "private static void Finalizer"):
            self.assertIn(text, code)

    def test_values_weak_identity_scoped_and_idempotent(self):
        code = read("src/Util/WeakInstanceValueScope.cs")
        for text in ("ConditionalWeakTable<TKey, LinkedList<TValue>>", "values.AddLast(value)",
                     "values.Last", "if (node.List != null) values.Remove(node)"):
            self.assertIn(text, code)
        self.assertNotIn("static", code)

    def test_saved_value_and_clone_not_removed(self):
        code = model("AllCurseBite")
        self.assertIn("[SavedProperty]", code)
        self.assertIn("DynamicVars.ExtraDamage.BaseValue = value", code)
        self.assertIn("new CalculationBaseVar(1)", code)

    def test_real_commands_cover_all_piles_and_calculated_self_exhaust(self):
        code = read("src/Debugging/CardEffects/DesignSyncAllCurseBiteContract.cs")
        for text in ("PileType.Draw, PileType.Discard, PileType.Hand, PileType.Exhaust, PileType.Play",
                     "await CardCmd.Exhaust", "receiver absorbs calculated forty",
                     "self-exhaust absorbs same forty", "new copy absorbs same forty",
                     "CardModel.FromSerializable", "ctx.Combat.CloneCard(deck)",
                     "faulted task releases snapshot", "grown real attack"):
            self.assertIn(text, code)
        self.assertIn("CustomVariants<AllCurseBite>(DesignSyncAllCurseBiteContract.Run, 28)",
                      read("src/Debugging/CardEffects/CardEffectTestCatalog.cs"))

    def test_pure_tests_compile_production_scope(self):
        self.assertIn("../../src/Util/WeakInstanceValueScope.cs",
                      read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        code = read("tests/DesignSyncContracts/Program.cs")
        for text in ("nested survives await", "equal-value keys remain isolated",
                     "exception cleans value scope", "active child survives early parent cleanup"):
            self.assertIn(text, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
