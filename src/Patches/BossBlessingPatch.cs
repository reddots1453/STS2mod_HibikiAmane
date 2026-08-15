using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Data;
using MaidenSuccubus.Rewards;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(RewardsCmd), nameof(RewardsCmd.OfferForRoomEnd))]
public static class BossBlessingPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        Player player,
        AbstractRoom room,
        ref Task __result)
    {
        int defeatedActIndex = player.RunState is RunState runState
            ? runState.CurrentActIndex
            : -1;
        Task original = __result;
        Task replacement = original;
        Safe.Run(
            () => replacement = OfferAfterOriginal(
                original,
                player,
                room,
                defeatedActIndex),
            "BossBlessing.OfferForRoomEnd");
        __result = replacement;
    }

    private static async Task OfferAfterOriginal(
        Task original,
        Player player,
        AbstractRoom room,
        int defeatedActIndex)
    {
        await original;
        if (room.RoomType != RoomType.Boss
            || defeatedActIndex < 0
            || player.Character is not MaidenSuccubusCharacter
            || player.RunState is not RunState runState)
        {
            return;
        }

        M5ProgressState state = M5Progress.Handle.Get(runState);
        if (state.BlessingOfferedActs.Contains(defeatedActIndex))
        {
            return;
        }
        M5Progress.Handle.Modify(
            runState,
            data => data.BlessingOfferedActs.Add(defeatedActIndex));
        MaidenSuccubusMod.Logger.Info(
            $"Offering goddess blessing after boss act {defeatedActIndex + 1}.");
        await GoddessBlessingService.Offer(player);
    }
}
