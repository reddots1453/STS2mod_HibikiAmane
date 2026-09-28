"""Seal presentation contracts, exact formal text and production test wiring."""
from pathlib import Path
import json
import re
import unittest

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


class SealPresentationContracts(unittest.TestCase):
    def test_exact_directional_descriptions(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/static_hover_tips.json"))
        for suffix, sign in (("holy", "≥3"), ("corrupt", "≤-3")):
            expected = f"由于堕落值{sign}，这张牌被封印了，战斗开始时不会进入抽牌堆。"
            actual = re.sub(r"\[/?(?:gold|purple)\]", "", loc[f"MAIDENSUCCUBUS_SEALED_CARD.{suffix}"])
            self.assertEqual(expected, actual)
            self.assertIn(expected, read("DesignDoc.md"))

    def test_sacrifice_no_longer_claims_direct_relic_growth(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/rest_site_ui.json"))
        text = loc["OPTION_MAIDEN_SUCCUBUS_SACRIFICE.description"]
        self.assertEqual(text, "移除所有封印区卡牌，不会占用本次火堆行动。")
        self.assertIn(text, read("DesignDoc.md"))
        self.assertNotIn("成长", text)

    def test_one_unchanged_sealing_rule(self):
        rule = read("src/Core/Seals/SealRules.cs")
        self.assertIn("CorruptionBand.Holy => route == RouteCardKind.Corrupt", rule)
        self.assertIn("CorruptionBand.Corrupt => route == RouteCardKind.Holy", rule)
        self.assertIn("SealRules.IsSealed(band, route)", read("src/Core/Seals/CombatSealQuery.cs"))
        self.assertIn("!maidenOwner || !permanentDeck || !IsSealed(band, route)", rule)

    def test_actual_owner_deck_and_original_preview_identity(self):
        code = read("src/Core/Seals/SealPresentation.cs")
        for token in ("card?.IsMutable != true", "card.Owner?.Character is not MaidenSuccubusCharacter",
                      "card.Owner.Deck.Cards.Contains(card)", "RouteCardQuery.TryGet(card", "CorruptionQuery.GetBand(run)"):
            self.assertIn(token, code)
        patch = read("src/Patches/DeckSealVisualPatch.cs")
        self.assertIn("SealPresentation.DescriptionKey(holder.CardModel)", patch)
        self.assertIn("SealPresentation.DescriptionKey(__instance.CardModel)", patch)
        self.assertIn("var tips = card.HoverTips.ToList()", patch)
        self.assertNotIn('"MAIDENSUCCUBUS_SEALED_CARD.description"', patch)

    def test_tint_recycling_and_display_scope(self):
        patch = read("src/Patches/DeckSealVisualPatch.cs")
        for token in ("ConditionalWeakTable<NGridCardHolder, OwnedVisualOverride<Color>>",
                      'typeof(NDeckViewScreen), "DisplayCards"', "nameof(NGridCardHolder.OnFreedToPool)",
                      "color => color * SealedColor", "tint.Restore(holder.Modulate)", "Tints.Remove(holder)",
                      "if (!IsInDeckView(holder)", "Safe.Run("):
            self.assertIn(token, patch)
        self.assertNotIn("Colors.White", patch)
        self.assertNotIn("_Process", patch)
        refresh = patch.split("private static void Refresh", 1)[1]
        self.assertLess(refresh.index("Restore(holder)"), refresh.index("if (!IsInDeckView(holder)"))

    def test_real_production_rules_execute_offline(self):
        project = read("tests/DesignSyncContracts/DesignSyncContracts.csproj")
        for path in ("src/Core/Seals/SealRules.cs", "src/Util/OwnedVisualOverride.cs"):
            self.assertIn("../../" + path, project)
        code = read("tests/DesignSyncContracts/Program.cs")
        for token in ("SealRules.DescriptionKey(", "SealRules.IsSealed(", "Enum.GetValues<CorruptionBand>()",
                      "Enum.GetValues<RouteCardKind>()", "flags < 4", "external color change wins",
                      "refresh does not multiply tint repeatedly", "recycled holder restores original not white"):
            self.assertIn(token, code)

    def test_read_only_runtime_inspection_not_fake_gameplay(self):
        code = read("src/ConsoleCommands/DesignSealViewTestConsoleCmd.cs")
        for token in ('"ms_test_seal_view"', "LocalContext.IsMe(player)", "Harmony.GetPatchInfo",
                      "DeckSealVisualPatch.HasSealTint(holder)", "holder.CardModel", "instance eligibility",
                      "card.MutableClone()", "card.CanonicalInstance", "read-only command preserves live state"):
            self.assertIn(token, code)
        for forbidden in ("CorruptionCmd.Set", "CardPileCmd.Add", "CardCmd.Upgrade", "PopulateCombatState("):
            self.assertNotIn(forbidden, code)


if __name__ == "__main__":
    unittest.main()
