"""Greed room replacement, inventory isolation and native test entry contracts."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


def relic():
    return read("src/Relics/FourthRouteRelics.cs").split("public sealed class GreedRouteRelic", 1)[1].split("[RegisterRelic", 1)[0]


class GreedContracts(unittest.TestCase):
    def test_stage_text_exact_design(self):
        design = read("DesignDoc.md").split("#### 贪婪\n", 1)[1].split("####", 1)[0]
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        prefix = "MAIDEN_SUCCUBUS_RELIC_GREED_ROUTE_RELIC.descriptionStage"
        for stage, label in ((1, "残缺"), (2, "完整"), (4, "觉醒")):
            self.assertEqual(re.search(rf"^{label}：(.*)$", design, re.M).group(1), loc[prefix + str(stage)])
        self.assertEqual(loc[prefix + "3"], loc[prefix + "4"])

    def test_real_room_patches_safe_and_reserved_first(self):
        code = read("src/Patches/GreedShopPatches.cs")
        for text in ('"RollRoomTypeFor"', '"CreateRoom"', 'nameof(Hook.ModifyMerchantPrice)',
                     "reserved: !__runOriginal", "Priority.Last", "Priority.First", "RoomType.Shop", "Safe.Run"):
            self.assertIn(text, code)
        self.assertIn("GreedShopService.Cancel(run)", code)
        self.assertIn("GreedShopService.Created(run, mapPointType, __result)", code)

    def test_inventory_creation_cannot_consume_qualification(self):
        self.assertNotIn("OnMerchantCreated", read("src/Patches/FourthRouteMerchantPatch.cs"))
        self.assertNotIn("OnMerchantCreated", relic())
        self.assertNotIn("CreateForNormalMerchant", read("src/Acts/GreedShopService.cs"))

    def test_service_commits_after_matching_unknown_merchant_created(self):
        code = read("src/Acts/GreedShopService.cs")
        for text in ("ConditionalWeakTable<RunState, Selection>", "MapPointType.Unknown || reserved",
                     "player.IsActiveForHooks", "FourthRouteLifecycle.IsEligible(player)",
                     "room is not MerchantRoom", "selection.Location != Location(run)",
                     "relic.Owner.Relics.Contains(relic)", "relic.BindFreeShop(selection.Location, merchant)"):
            self.assertIn(text, code)
        selecting = code.split("internal static bool Select", 1)[1].split("internal static void Cancel", 1)[0]
        self.assertNotIn("BindFreeShop", selecting)

    def test_saved_receipt_room_scope_and_no_replay(self):
        code = relic()
        for text in ("[SavedProperty] public bool FreeShopPending", "[SavedProperty] public bool FreeShopActive",
                     "[SavedProperty] public string FreeShopLocation", "[SavedProperty] public bool PickupEffectGranted",
                     "Stage == 0 || PickupEffectGranted", "ReferenceEquals(Owner.RunState.CurrentRoom, _freeShopRoom)",
                     "player == Owner", "_shop.Leave()", "DeepCloneFields"):
            self.assertIn(text, code)
        self.assertLess(code.index("PickupEffectGranted = true"), code.index("await PlayerCmd.GainGold"))

    def test_production_rules_are_executed(self):
        self.assertIn("../../src/Core/Relics/GreedShopState.cs", read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        code = read("tests/DesignSyncContracts/Program.cs")
        for text in ("greed later location needs unspent entitlement", "failed creation keeps pending", "reserved event wins",
                     "deferred next unknown still eligible", "other player not free", "same floor other act not free",
                     "legacy active without location cannot grant arbitrary shop", "one entitlement never creates two shops"):
            self.assertIn(text, code)

    def test_native_command_checks_hooks_prices_and_reservation(self):
        code = read("src/ConsoleCommands/DesignGreedTestConsoleCmd.cs")
        for text in ("RelicCmd.Obtain", "SavedProperties.From(relic)!.Fill(saved)", "Harmony.GetPatchInfo(roll)",
                     "roll.Invoke(RunManager.Instance", "create.Invoke(RunManager.Instance", "reservation.Patch(roll",
                     "MerchantInventory.CreateForNormalMerchant", "inventory.AllEntries.All(entry => entry.Cost == 0)",
                     "Hook.ModifyMerchantPrice", "ordinary inventory remains paid", "loaded model must bind actual room before pricing",
                     "higher priority reservation wins actual patched roll", "different shop instance at same floor is not free"):
            self.assertIn(text, code)

    def test_destructive_entry_guard_and_cleanup(self):
        code = read("src/ConsoleCommands/DesignGreedTestConsoleCmd.cs")
        for text in ("#if DEBUG", 'args[0] != "confirm"', "issuingPlayer.RunState.Players.Count != 1",
                     "CombatManager.Instance.IsInProgress", "_running", "TestMode.IsOn = previousTestMode",
                     "reservation.UnpatchAll(reservation.Id)", "run.ActFloor = floorBefore", "GreedShopService.Cancel(run)"):
            self.assertIn(text, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
