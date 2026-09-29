"""Offline contract for starter UI, run visibility and stored invasion curses."""
import json
import unittest

from TestDesignSyncNeutral20260927 import read


class RunUiRelicContracts(unittest.TestCase):
    def test_starter_arrows_use_native_ascension_art_and_flank_relic(self):
        source = read("src/UI/StarterRelicSelector.cs")
        self.assertIn('GetNode<NAscensionPanel>("%AscensionPanel")', source)
        self.assertIn('GetNode<NButton>("HBoxContainer/LeftArrowContainer/LeftArrow")', source)
        self.assertIn('GetNode<NButton>("HBoxContainer/RightArrowContainer/RightArrow")', source)
        self.assertIn('bounds.Position.X - _previous.Size.X', source)
        self.assertIn('bounds.End.X + 8f', source)
        self.assertNotIn('Text = "‹"', source)

    def test_resource_ui_follows_map_and_top_bar_lifecycle(self):
        rail = read("src/UI/MaidenSidebarRail.cs")
        balance = read("src/UI/CorruptionMeter.cs")
        for source in (rail, balance):
            self.assertIn("CurrentMapCoord.HasValue", source)
            self.assertIn("_topBar.Position.Y", source)
        self.assertIn("RefreshVisibility();", rail)

    def test_alternate_starter_and_stored_curse_relic_registered(self):
        collection = read("src/Patches/StarterRelicCollectionPatch.cs")
        self.assertIn("ModelDb.Relic<HeroOrb>()", collection)
        self.assertIn("ModelDb.Relic<EternalOrb>()", collection)
        relic = read("src/Relics/InternalCondom.cs")
        self.assertIn("RelicRarity.Uncommon", relic)
        self.assertIn("override bool ShouldAddToDeck", relic)
        self.assertIn("AfterAddToDeckPrevented", relic)
        self.assertIn("[SavedProperty]", relic)
        merchant = read("src/Merchant/InvasionCurseMerchantService.cs")
        self.assertIn("condom.StoredCount = 0", merchant)
        self.assertIn("selected.Length + stored", merchant)
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        self.assertIn("内用套套", loc["MAIDEN_SUCCUBUS_RELIC_INTERNAL_CONDOM.title"])
        self.assertIn("获得3点最大生命值", loc["MAIDEN_SUCCUBUS_RELIC_INTERNAL_CONDOM.descriptionCorrupt"])
        self.assertEqual(json.loads(read("docs/content_contract_20260824.json"))["relics"].count("InternalCondom"), 1)


if __name__ == "__main__":
    unittest.main()
