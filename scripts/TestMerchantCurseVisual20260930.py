"""Static contract for the native-looking special shop removal slot.

This does not replace a game render/interaction check.
"""

from pathlib import Path
import json
import unittest


ROOT = Path(__file__).resolve().parents[1]
PATCH = (ROOT / "src/Patches/MerchantInvasionCursePatch.cs").read_text(encoding="utf-8")
SERVICE = (ROOT / "src/Merchant/InvasionCurseMerchantService.cs").read_text(encoding="utf-8")
HOVERS = json.loads((ROOT / "MaidenSuccubus/localization/zhs/static_hover_tips.json").read_text(encoding="utf-8"))


class MerchantCurseVisualContract(unittest.TestCase):
    def test_slot_uses_the_native_card_removal_control_and_art(self):
        self.assertIn('GetNodeOrNull<NMerchantCardRemoval>("%MerchantCardRemoval")', PATCH)
        self.assertIn("ordinary.Duplicate((int)Node.DuplicateFlags.Scripts)", PATCH)
        self.assertIn("is not NMerchantCardRemoval slot", PATCH)
        self.assertIn("parent.AddChild(slot)", PATCH)
        self.assertIn("PlaceBeside(slot, ordinary)", PATCH)
        self.assertIn('GetNode<Sprite2D>("%Visual")', PATCH)
        self.assertNotIn("new Button", PATCH)

    def test_special_purchase_is_independent_of_normal_removal(self):
        self.assertIn("new MerchantCardRemovalEntry(player)", PATCH)
        self.assertIn('HarmonyPatch(typeof(NMerchantCardRemoval), "OnTryPurchase")', PATCH)
        self.assertIn("__result = replacement", PATCH)
        self.assertIn("InvasionCurseMerchantService.SelectAndRemove(state.Player)", PATCH)
        self.assertIn("slot.OnCardRemovalUsed()", PATCH)
        self.assertNotIn("CardShopRemovalsUsed", PATCH)
        self.assertNotIn("OnTryPurchaseWrapper", PATCH)
        self.assertIn("CardPileCmd.RemoveFromDeck(curse)", SERVICE)
        self.assertIn("PlayerCmd.GainGold(refund, player)", SERVICE)

    def test_native_focus_and_dynamic_refund_hover_are_connected(self):
        self.assertIn('HarmonyPatch(typeof(NMerchantInventory), "UpdateNavigation")', PATCH)
        self.assertIn("ordinary.FocusNeighborRight = slot.GetPath()", PATCH)
        self.assertIn('HarmonyPatch(typeof(NMerchantCardRemoval), "CreateHoverTip")', PATCH)
        self.assertIn('label.SetTextAutoSize($"+{refund}")', PATCH)
        self.assertIn("HoverTip.GetHoverTipAlignment(__instance)", PATCH)
        self.assertIn("{Count}", HOVERS["MAIDENSUCCUBUS_SHOP_CURSE_REMOVAL.description"])
        self.assertIn("{Refund}", HOVERS["MAIDENSUCCUBUS_SHOP_CURSE_REMOVAL.description"])


if __name__ == "__main__":
    unittest.main()
