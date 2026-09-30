"""Guard the native-arrow isolation and replay-border layout regressions."""

from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]


def source(relative):
    return (ROOT / relative).read_text(encoding="utf-8")


class UiVisualRegression(unittest.TestCase):
    def test_relic_arrow_has_independent_native_state(self):
        code = source("src/UI/StarterRelicArrowLayout.cs")
        self.assertIn("source.Duplicate((int)Node.DuplicateFlags.Scripts)", code)
        self.assertNotIn("Node.DuplicateFlags.UseInstantiation", code)
        self.assertNotIn("Node.DuplicateFlags.Signals", code)
        self.assertIn("canvas.Material.Duplicate(true)", code)
        self.assertIn("NativeUiClone.RestoreOwners(button)", code)

    def test_relic_arrows_use_panel_local_anchors(self):
        code = source("src/UI/StarterRelicArrowLayout.cs")
        self.assertIn("panel.AddChild(Previous)", code)
        self.assertIn("panel.AddChild(Next)", code)
        self.assertIn("button.AnchorLeft = button.AnchorRight = right ? 1f : 0f", code)
        self.assertIn("icon.ItemRectChanged += PlaceArrows", code)
        self.assertNotIn("GetGlobalRect", code)
        selector = source("src/UI/StarterRelicSelector.cs")
        self.assertNotIn("PlaceArrows", selector)

    def test_replay_border_has_explicit_card_extent(self):
        overlay = source("src/UI/BattleReplayCardVisuals.cs")
        capability = source("src/Core/Replay/BattleReplayOriginCapability.cs")
        self.assertIn("Size = NCard.defaultSize", overlay)
        self.assertIn("Position = -NCard.defaultSize / 2f", overlay)
        self.assertNotIn("root.SetAnchorsAndOffsetsPreset", overlay)
        self.assertIn("fullRect: false", capability)
        self.assertIn("ZIndex = 1", overlay)


if __name__ == "__main__":
    unittest.main()
