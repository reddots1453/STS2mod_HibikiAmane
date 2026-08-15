using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters.Starts;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
public static class StartProfilePatch
{
    [HarmonyPostfix]
    public static void Postfix(RunState __result)
    {
        Safe.Run(
            () => Apply(__result),
            "StartProfile.CreateForNewRun");
    }

    private static void Apply(RunState runState)
    {
        var profile = runState.Players
            .Select(player => player.Character)
            .OfType<IMaidenSuccubusStartProfile>()
            .FirstOrDefault();
        if (profile == null)
        {
            return;
        }

        CorruptionCmd.Set(
            runState,
            profile.InitialCorruption,
            new CorruptionChangeSource(
                $"start.{profile.StartProfileId.ToString().ToLowerInvariant()}"));
        M5Progress.Handle.Modify(
            runState,
            data =>
            {
                data.StartProfileApplied = true;
                data.StartProfileId = profile.StartProfileId.ToString();
            });
        MaidenSuccubusMod.Logger.Info(
            $"Applied start profile {profile.StartProfileId}; "
            + $"initial corruption={profile.InitialCorruption}.");
    }
}
