"""Reward presentation contracts; native PCK inspection is not a Godot render test."""
import json
import re
import struct
import unittest
from pathlib import Path
from TestDesignSyncNeutral20260927 import read


class RewardContracts(unittest.TestCase):
    def test_installed_native_holder_scene_and_dependencies(self):
        pack = Path(__file__).resolve().parents[3].parent / "SlayTheSpire2.pck"
        if not pack.is_file():
            self.skipTest("Installed game PCK unavailable; native scene not verified here")
        with pack.open("rb") as stream:
            header = stream.read(40)
            self.assertEqual(header[:4], b"GDPC")
            self.assertEqual(struct.unpack_from("<I", header, 4)[0], 3)
            base, directory = struct.unpack_from("<QQ", header, 24)
            stream.seek(directory)
            count = struct.unpack("<I", stream.read(4))[0]
            files = {}
            for _ in range(count):
                length = struct.unpack("<I", stream.read(4))[0]
                name = stream.read(length).rstrip(b"\0").decode()
                offset, size = struct.unpack("<QQ", stream.read(16))
                stream.read(20)
                files[name] = (offset, size)
            path = "scenes/ui/treasure_relic_holder.tscn"
            offset, size = files[path]
            stream.seek(base + offset)
            scene = stream.read(size).decode()
            for node in ("Relic", "MultiplayerVoteContainer", "UncommonGlow", "RareGlow"):
                self.assertIn('name="' + node + '"', scene)
            self.assertIn("NTreasureRoomRelicHolder.cs", scene)
            self.assertIn("Vector2(136, 136)", scene)
            for dependency in re.findall(r'path="res://([^"]+)"', scene):
                self.assertIn(dependency, files)

    def test_native_holder_no_shared_chest_commands(self):
        code = read("src/UI/FourthRouteRewardScreen.cs")
        self.assertIn('"res://scenes/ui/treasure_relic_holder.tscn"', code)
        self.assertIn("Instantiate<NTreasureRoomRelicHolder>()", code)
        self.assertIn("_holder.Relic.Model = relic", code)
        self.assertIn("NClickableControl.SignalName.Released", code)
        for forbidden in ("TreasureRoomRelicSynchronizer", "PickRelicLocally", "RelicCmd.Obtain", "RewardsCmd"):
            self.assertNotIn(forbidden, code)

    def test_only_completed_trial_and_current_effect_preview(self):
        code = read("src/UI/FourthRouteRewardScreen.cs")
        self.assertIn("QuestText(_offer.Quest, _offer.Trial)", code)
        self.assertIn("CreateRelicPreview(_offer.Quest, _offer.Stage)", code)
        self.assertIn("relic.DynamicDescription.GetFormattedText()", code)
        self.assertNotIn("relic.Owner =", code)
        self.assertNotIn("ProgressText(", code)
        loc = json.loads(read("MaidenSuccubus/localization/zhs/events.json"))
        self.assertEqual("试炼的奖赏", loc["MAIDEN_SUCCUBUS_TRIAL_REWARD.title"])

    def test_closes_modal_before_native_pickup_selectors(self):
        code = read("src/Acts/FourthRouteRewardFlow.cs")
        self.assertLess(code.index("await FourthRouteRewardScreen.Show"), code.index("await Claim(player, offer)"))
        self.assertIn("if (picked is null || !Current()) return false", code)
        self.assertIn("offer.Matches(quest, FourthRouteProgressService.Trial(run).Phase)", code)
        self.assertIn("await FourthRouteProgressService.ClaimInitialReward(player)", code)
        self.assertIn("RelicInventory.AnimateRelic", code)

    def test_victory_waits_entire_original_not_fire_and_forget(self):
        code = read("src/Patches/FourthRouteRewardPatch.cs")
        self.assertIn("nameof(Hook.AfterCombatVictory)", code)
        self.assertIn("ref Task __result", code)
        self.assertLess(code.index("await original;"), code.index("await FourthRouteRewardFlow.Show"))
        self.assertIn("victoryBoundary: true", code)
        self.assertIn("while (FourthRouteRewardFlow.Pending(run) != null", code)
        self.assertIn("SceneTree.SignalName.ProcessFrame", code)
        self.assertNotIn("EnterNextAct", code)
        self.assertNotIn("TaskHelper.RunSafely", code)

    def test_idle_watcher_does_not_retry_failure_forever(self):
        code = read("src/UI/FourthRouteRewardWatcher.cs")
        for token in ("_elapsed < .15", "_failedOffer == offer", "_failedRoom, run.CurrentRoom",
                      "FourthRouteRewardFlow.Ready", "finally { _busy = false; }"):
            self.assertIn(token, code)
        self.assertIn("await FourthRouteRewardFlow.Show(_owner)", read("src/Acts/FourthRouteLifecycle.cs"))

    def test_cleanup_and_map_restore(self):
        ui = read("src/UI/FourthRouteRewardScreen.cs")
        for token in ("RemoveBlockingScreen(this)", "NHoverTipSet.Remove(_holder)", "_completion.TrySetResult(null)",
                      "if (!_isCurrent()) Close(null)", "container.Clear()", "AutoSizeEnabled = false"):
            self.assertIn(token, ui)
        flow = read("src/Acts/FourthRouteRewardFlow.cs")
        for token in ("finally", "gate.Busy = false", "map.SetTravelEnabled(true)", "Transition.InTransition",
                      "run.Players.Count == 1", "TestMode.IsOn", "LocalContext.IsMe(player)", "NetGameType.Replay",
                      "NCapstoneContainer.Instance?.InUse", "LocalMutableEvent: Neow"):
            self.assertIn(token, flow)

    def test_actual_runtime_script_and_pure_rules_linked(self):
        self.assertIn("../../src/Acts/FourthRouteRewardOffer.cs", read("tests/DesignSyncContracts/DesignSyncContracts.csproj"))
        code = read("src/ConsoleCommands/DesignRouteRewardTestConsoleCmd.cs")
        for token in ("#if DEBUG", 'args[0] != "confirm"', "TestMode.IsOn = previous", "FourthRouteRewardFlow.Claim",
                      "TestCardSelector", "Harmony.GetPatchInfo", "AfterVictory"):
            self.assertIn(token, code)


if __name__ == "__main__":
    unittest.main(verbosity=2)
