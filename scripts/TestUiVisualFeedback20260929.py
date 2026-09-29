"""Regression guards for the three visual feedback fixes."""

from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]


def source(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


class UiVisualFeedback(unittest.TestCase):
    def test_starter_uses_native_button_without_ascension_signal(self):
        code = source("src/UI/StarterRelicSelector.cs")
        self.assertIn("source.Duplicate((int)flags)", code)
        self.assertNotIn("Node.DuplicateFlags.Signals", code)
        self.assertIn("SignalName.Released", code)
        self.assertIn("button.TooltipText = string.Empty", code)
        self.assertNotIn("new Button", code)

    def test_replay_overlay_occupies_card_foreground(self):
        code = source("src/Core/Replay/BattleReplayOriginCapability.cs")
        self.assertIn("fullRect: true", code)
        self.assertIn("order: 1", code)
        self.assertIn("BattleReplayCardVisuals.CreateShadowOverlay()", code)

    def test_reveal_keeps_only_animated_enchantment_tab(self):
        code = source("src/Patches/EnchantmentRevealVisualPatch.cs")
        self.assertIn("NCardEnchantVfx", code)
        self.assertIn("nameof(NCard.UpdateVisuals)", code)
        self.assertIn("__instance.EnchantmentTab.Visible = false", code)
        self.assertIn("Safe.Run", code)


if __name__ == "__main__":
    unittest.main()
