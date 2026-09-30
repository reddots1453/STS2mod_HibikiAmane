"""Native loot integration contracts, backed by production rule and engine tests."""
import re
import struct
import unittest
from pathlib import Path
from TestDesignSyncNeutral20260927 import read

class RewardContracts(unittest.TestCase):
    def test_installed_native_loot_scene(self):
        pack=Path(__file__).resolve().parents[3].parent/'SlayTheSpire2.pck'
        if not pack.is_file(): self.skipTest('Game PCK unavailable')
        with pack.open('rb') as stream:
            header=stream.read(40)
            self.assertEqual(header[:4],b'GDPC')
            base,directory=struct.unpack_from('<QQ',header,24)
            stream.seek(directory)
            count=struct.unpack('<I',stream.read(4))[0]
            files={}
            for _ in range(count):
                length=struct.unpack('<I',stream.read(4))[0]
                name=stream.read(length).rstrip(b'\0').decode()
                offset,size=struct.unpack('<QQ',stream.read(16));stream.read(20)
                files[name]=(offset,size)
            offset,size=files['scenes/screens/rewards_screen.tscn']
            stream.seek(base+offset);scene=stream.read(size).decode()
            self.assertIn('NRewardsScreen.cs',scene)
            for dependency in re.findall(r'path="res://([^"]+)"',scene):
                self.assertTrue(dependency in files or dependency + '.import' in files or dependency + '.remap' in files, 'Missing native dependency: ' + dependency)

    def test_trial_uses_native_loot_and_one_claim(self):
        code=read('src/UI/FourthRouteRewardScreen.cs')
        self.assertIn('FourthRouteTrialRelicReward : RelicReward',code)
        self.assertIn('WithCustomRewards([reward]).WithSkippingDisallowed().Offer()',code)
        self.assertIn('await FourthRouteRewardFlow.Claim(Player, _offer)',code)
        self.assertIn('nameof(ClaimedRelic)',code)
        self.assertNotIn('RelicCmd.Obtain',code)
        self.assertNotIn('NModalContainer',code)
        self.assertNotIn('NTreasureRoomRelicHolder',code)

    def test_current_stage_preview_and_stale_receipt(self):
        code=read('src/UI/FourthRouteRewardScreen.cs')
        self.assertIn('CreateRelicPreview(offer.Quest, offer.Stage)',code)
        self.assertIn('!_isCurrent()',code)
        self.assertIn('SuccessfullySelected',code)
        self.assertNotIn('relic.Owner =',code)
        flow=read('src/Acts/FourthRouteRewardFlow.cs')
        self.assertIn('Pending(run) == offer',flow)
        self.assertIn('await FourthRouteRewardScreen.Show(player, offer, Current)',flow)

    def test_event_handlers_finish_before_reward_opens(self):
        code=read('src/Acts/FourthRouteRewardFlow.cs')
        self.assertIn('room.LocalMutableEvent.IsFinished',code)
        self.assertIn('AwaitPendingOptionTasks().IsCompletedSuccessfully',code)
        self.assertIn('EventSettled(run)',code)
        self.assertNotIn('SetEventFinished(',code)
        self.assertNotIn('SetEventState(',code)

    def test_victory_wrapper_and_map_restoration(self):
        code=read('src/Patches/FourthRouteRewardPatch.cs')
        self.assertIn('ref Task __result',code)
        self.assertLess(code.index('await original;'),code.index('await FourthRouteRewardFlow.Show'))
        self.assertIn('victoryBoundary: true',code)
        flow=read('src/Acts/FourthRouteRewardFlow.cs')
        for token in ('finally','gate.Busy = false','map.SetTravelEnabled(true)','LocalContext.IsMe(player)','NetGameType.Replay'):
            self.assertIn(token,flow)

    def test_idle_watcher_failure_does_not_loop(self):
        code=read('src/UI/FourthRouteRewardWatcher.cs')
        for token in ('_failedOffer == offer','_failedRoom, run.CurrentRoom','finally { _busy = false; }'):
            self.assertIn(token,code)

if __name__=='__main__':unittest.main(verbosity=2)
