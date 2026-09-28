"""Offering integration source contracts; engine scenarios are compiled separately."""
import unittest
from TestDesignSyncNeutral20260927 import read


class Offering(unittest.TestCase):
    def test_wrap_scope_and_native_allocation_boundary(self):
        code = read('src/Rewards/GenerosityOffering.cs')
        self.assertIn('set.Room is not CombatRoom', code)
        self.assertIn('set.Rewards[i].GetType() == typeof(RelicReward)', code)
        self.assertIn('player.Character is not MaidenSuccubusCharacter', code)
        patches = read('src/Patches/GenerosityOfferingPatches.cs')
        self.assertIn('"AnimateRelicAwards"', patches)
        self.assertNotIn('typeof(RelicCmd), nameof(RelicCmd.Obtain))]', patches)
        self.assertIn('await original;', patches)

    def test_group_mutex_native_removal_and_deterministic_save(self):
        code = read('src/Rewards/GenerosityOffering.cs')
        for token in ('if (_busy || Resolved) return false;', 'Resolved = true;',
                      'FromDeckForRemoval', 'Cancelable = false', '.Distinct().Take(2)',
                      'ToSerializable() => RelicChoice.ToSerializable()', '_skipRecorded',
                      'nameof(SuccessfullySelected)', 'finally { PendingTreasure.Remove(relic); }'):
            self.assertIn(token, code)
        skipped = code.split('public override void OnSkipped()', 1)[1].split('public override SerializableReward', 1)[0]
        self.assertNotIn('AddProgress', skipped)

    def test_native_network_index_adapter_and_completed_message_guard(self):
        code = read('src/Patches/GenerosityOfferingPatches.cs')
        for token in ('"SelectLocalReward"', 'GenerosityOfferingRules.Encode(parent, child)',
                      'GenerosityOfferingRules.TryDecode', 'set.Player != player',
                      'group.Choices[child]', 'IsRewardsSetCompleted(player, __0.setId)',
                      'Expected exactly one native call'):
            self.assertIn(token, code)
        self.assertNotIn('PickRelicLocally', code)

    def test_ui_eligibility_and_pending_obtain_animation(self):
        code = read('src/Patches/GenerosityOfferingPatches.cs') + read('src/UI/GenerosityOfferingVisibility.cs')
        for token in ('button.Visible = available', '"%ChainContainer"',
                      'GenerosityOffering.IsPendingTreasure', 'GenerosityOffering.CanOffer(group.Player)'):
            self.assertIn(token, code)

    def test_real_engine_command_is_wired(self):
        self.assertIn('GenerosityOfferingContract.Run(player, Check)', read('src/ConsoleCommands/DesignGenerosityTestConsoleCmd.cs'))
        code = read('src/Debugging/GenerosityOfferingContract.cs')
        for token in ('Enum.GetValues<FourthTrialPhase>()', 'Reward.FromSerializable', 'SetupForAsyncCardSelection',
                      'SelectLocalReward(group.Choices[1])', 'GenerosityRemoteRewardIndexPatch.Prefix',
                      'GenerosityOffering.ObtainAllocated(allocated, player)', 'RewardsSet.testSelector = previousSelector'):
            self.assertIn(token, code)


if __name__ == '__main__':
    unittest.main()
