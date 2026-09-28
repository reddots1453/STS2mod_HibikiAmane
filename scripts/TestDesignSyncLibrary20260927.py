"""Source wiring for library's engine scenarios; does not claim engine execution."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


class LibraryContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.power = read("src/Powers/YarusLibraryPower.cs")
        cls.command = read("src/Commands/TemperancePileCmd.cs")
        cls.runtime = read("src/Debugging/CardEffects/DesignSyncLibraryContract.cs")

    def test_active_power_and_draw_ban(self):
        for token in ("IsMutable && Amount > 0 && Owner.IsAlive && Owner.Powers.Contains(this)",
                      "ShouldDraw(Player player, bool fromHandDraw) => !IsActive || player.Creature != Owner",
                      "if (!IsActive || player.Creature != Owner || Owner.CombatState == null"):
            self.assertIn(token, self.power)

    def test_revalidate_choice_and_combat_before_rng(self):
        for token in ("var combat = player.Creature.CombatState", "player.Creature.CombatState == combat", "!choices.Contains(selected)",
                      "card.Owner == player && card.Pile == selected.SelectedPile.GetPile(player)"):
            self.assertIn(token, self.command)
        self.assertLess(self.command.index("!choices.Contains(selected)"), self.command.index("cards.StableShuffle"))
        self.assertIn("canSkip: false", self.command)

    def test_snapshot_native_play_and_ten_limit_unchanged(self):
        for token in ("selected.SelectedPile.GetPile(player).Cards.ToList()",
                      "cards.StableShuffle(player.RunState.Rng.CombatCardGeneration)",
                      "foreach (CardModel card in cards.Take(count))", "await CardCmd.AutoPlay(context, card, target: null)"):
            self.assertIn(token, self.command)
        self.assertIn("TemperancePileCmd.Play(context, player, 10, () => IsActive)", self.power)
        self.assertIn("2费   你不能抽牌。回合开始时，选择一个牌堆，随机打出其中的10张牌。升级后获得保留。", read("DesignDoc.md"))

    def test_three_piles_boundaries_and_independent_rng_identity(self):
        for token in ("[PileType.Draw, PileType.Discard, PileType.Exhaust]", "new[] { 0, 1, 9, 10, 12 }",
                      "new Rng(", "sorted.Sort()", "predictedRng.NextInt(i + 1)",
                      "actual.SequenceEqual(chosen)", "actual.Distinct().Count()", "predictedRng.ToSerializable().ToString()",
                      "OfType<CardPlayStartedEntry>()", "entry.CardPlay.Card"):
            self.assertIn(token, self.runtime)
        self.assertNotIn(".StableShuffle(", self.runtime)

    def test_actual_draw_ban_and_native_unplayable(self):
        for token in ("await CardPileCmd.Draw(context, 1, ctx.Player)", "OfType<CardDrawnEntry>().Count()",
                      "ctx.Add<Wound>(PileType.Exhaust)", "PlaysSince(history).SequenceEqual([defend])",
                      "native unplayable result is discard", "library free plays cost no energy"):
            self.assertIn(token, self.runtime)

    def test_other_piles_hand_and_permanent_deck_unchanged(self):
        for token in ("cards.Except(chosen)", "originals.All(c => c.Pile == other.GetPile(ctx.Player))",
                      "!actual.Intersect(originals).Any()", "Cards.Where(originals.Contains).SequenceEqual(originals)",
                      "library leaves hand untouched", "library does not change permanent deck",
                      "library does not consume deck shuffle RNG"):
            self.assertIn(token, self.runtime)

    def test_async_failure_removal_and_foreign_owner(self):
        for token in ("power.AfterPlayerTurnStart(context, foreign)", "library permits other player's draws",
                      "await Task.Yield()", "TaskCanceledException", "library selection failure propagates without settling",
                      "await PowerCmd.Remove(power)", "removed during choice plays nothing", "removed reference never prompts",
                      "native draw resumes after removal", "ModelDb.Power<YarusLibraryPower>().ShouldDraw"):
            self.assertIn(token, self.runtime)

    def test_registration_and_fulltext_keep_ancient_contract(self):
        self.assertIn("CustomVariants<TemperanceCirclet>(DesignSyncLibraryContract.Run, 200)",
                      read("src/Debugging/CardEffects/CardEffectTestCatalog.cs"))
        for token in ("CardRarity.Ancient", "CardKeyword.Retain",
                      "ctx.Player.RunState.CreateCard<TemperanceCirclet>", "instance.GetDescriptionForPile(pile, ctx.PrimaryEnemy)",
                      "节制之环+", "保留。\\n"):
            self.assertIn(token, self.runtime)


if __name__ == "__main__":
    unittest.main()
