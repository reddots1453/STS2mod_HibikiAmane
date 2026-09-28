"""Attack-only shatter and random-target regression wiring, not engine execution."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read, model


class ShatterRandomContracts(unittest.TestCase):
    def test_design_scope_and_enemy_player_symmetry(self):
        design = re.sub(r"\s|\*", "", read("DesignDoc.md"))
        for text in (
            "破碎：受到额外{破碎层数}点攻击伤害，每回合减少1层。",
            "适用于敌人和玩家的负面状态。敌人与玩家使用完全相同的结算规则。",
            "气旋破裂攻击牌普通0费造成5点伤害。给予1/2层破碎。",
            "爆发式冲击攻击牌罕见1费随机造成3/5点伤害2次。给予受到伤害的敌人2层破碎。",
            "闪电踢击攻击牌普通2费造成10/12点伤害。给予4/5层破碎。",
            "渎神黄昏攻击牌稀有1费4欲望随机对敌人造成5/6点伤害5/6次。",
        ):
            self.assertIn(text, design)

    def test_shatter_filters_attack_type_in_additive_hook(self):
        source = read("src/Powers/MvpDebuffPowers.cs").split("public sealed class ShatterPower", 1)[1].split("[RegisterPower]", 1)[0]
        additive = source.split("public override decimal ModifyDamageAdditive", 1)[1].split("public override async Task AfterSideTurnEnd", 1)[0]
        for token in ("target != Owner", "dealer is null", "dealer.Side == Owner.Side", "!props.IsPoweredAttack()", "return Amount;"):
            self.assertIn(token, additive)
        self.assertLess(additive.index("!props.IsPoweredAttack()"), additive.index("return Amount;"))
        self.assertIn("side == Owner.Side", source)
        self.assertIn("await PowerCmd.TickDownDuration(this)", source)

    def test_exact_card_text_and_upgrade_variable(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/cards.json"))
        expected = {
            "CYCLONE_RUPTURE": "造成{Damage:diff()}点伤害。\n给予{ShatterPower:diff()}层[gold]破碎[/gold]。",
            "LIGHTNING_KICK": "造成{Damage:diff()}点伤害。\n给予{ShatterPower:diff()}层[gold]破碎[/gold]。",
            "EXPLOSIVE_IMPACT": "随机造成{Damage:diff()}点伤害2次。\n给予受到伤害的敌人2层[gold]破碎[/gold]。",
            "BLASPHEMOUS_TWILIGHT": "随机对敌人造成{Damage:diff()}点伤害{Hits:diff()}次。",
        }
        for name, text in expected.items():
            self.assertEqual(loc["MAIDEN_SUCCUBUS_CARD_" + name + ".description"], text)
        self.assertIn('DynamicVars["ShatterPower"].UpgradeValueBy(1)', model("CycloneRupture"))

    def test_production_random_attacks_keep_native_targeting(self):
        for name in ("ExplosiveImpact", "BlasphemousTwilight"):
            source = model(name)
            self.assertIn("TargetType.RandomEnemy", source)
            self.assertIn("TargetingRandomOpponents(CombatState)", source)
            self.assertIn("WithHitCount(", source)
        source = model("ExplosiveImpact")
        self.assertIn(".Distinct().Where(enemy => enemy.IsAlive)", source)

    def test_real_four_card_suite_is_connected(self):
        catalog = read("src/Debugging/CardEffects/CardEffectTestCatalog.cs")
        for name, method, count in (("CycloneRupture", "Targeted", 26), ("LightningKick", "Targeted", 26),
                                    ("ExplosiveImpact", "Random", 14), ("BlasphemousTwilight", "Random", 14)):
            self.assertIn(f"CustomVariants<{name}>((ctx, card, upgraded) => DesignSyncShatterRandomContract.{method}(ctx, card, upgraded), {count})", catalog)
        self.assertNotIn("DamageTargetPower<ExplosiveImpact>", catalog)
        self.assertNotIn("Damage<BlasphemousTwilight>", catalog)
        runner = read("src/Debugging/CardEffects/CardEffectTestRunner.cs")
        self.assertIn('"ds27-shatter-random"', runner)
        self.assertIn("DesignSyncShatterRandomContract.Validate(context, card, scenario.Upgraded)", runner)

    def test_damage_type_and_side_cases_use_native_commands(self):
        source = read("src/Debugging/CardEffects/DesignSyncShatterRandomContract.cs")
        for token in ("new[] { ctx.Self, ctx.PrimaryEnemy }", "ApplyPower<ShatterPower>(target, 2)",
                      "ValueProp.Unpowered | ValueProp.Move, 3", "ValueProp.Move | ValueProp.Unblockable, 5",
                      "CreatureCmd.Damage(choice, target, 3, props, opponent)",
                      "DamageCmd.Attack(4).FromMonster", "WithHitCount(2)", "ctx.Play(ctx.Create<MaidenStrike>(), target)",
                      "power.AfterSideTurnEnd(choice, otherSide", "power.AfterSideTurnEnd(choice, target.Side",
                      "!target.HasPower<ShatterPower>()"):
            self.assertIn(token, source)

    def test_random_cases_never_assume_first_enemy_takes_every_hit(self):
        source = read("src/Debugging/CardEffects/DesignSyncShatterRandomContract.cs").split("internal static async Task Random", 1)[1]
        for token in ("new[] { 0, 2 }", "CreatureCmd.Add<Byrdonis>", "hp.Count >= 2", "await ctx.Play(card)",
                      "hp.Sum(pair => pair.Value - pair.Key.CurrentHp)", "lost % perHit == 0",
                      "observedHits += lost / perHit", "card is ExplosiveImpact && lost > 0 ? 2 : 0",
                      "finally", "CreatureCmd.Escape(extra)"):
            self.assertIn(token, source)
        self.assertNotIn("ctx.PrimaryEnemy", source)


if __name__ == "__main__":
    unittest.main()
