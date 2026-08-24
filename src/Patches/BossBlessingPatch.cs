using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Features;
using MaidenSuccubus.Data;
using MaidenSuccubus.Rewards;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(RewardsCmd), nameof(RewardsCmd.OfferForRoomEnd))]
public static class BossBlessingPatch
{
    [HarmonyPrefix]
    public static bool Prefix(
        Player player,
        AbstractRoom room,
        ref Task __result)
    {
        bool shouldSuppress = false;
        Safe.Run(
            () => shouldSuppress = room is CombatRoom { RoomType: RoomType.Boss }
                && player.RunState is RunState runState
                && runState.CurrentActIndex == 2
                && runState.Acts.Any(act => act is MaidenSuccubusFourthAct)
                && M5Progress.Handle.Get(runState).FourthRouteThirdBossDefeated,
            "FourthRoute.ShouldSuppressActThreeBossRewards");
        if (!shouldSuppress)
        {
            return true;
        }

        bool suppressed = false;
        Task replacement = __result;
        Safe.Run(
            () =>
            {
                MaidenSuccubusMod.Logger.Info(
                    "Suppressing vanilla Act 3 boss rewards before entering the appended Fourth Act.");
                replacement = new RewardsSet(player).EmptyForRoom(room).Offer();
                suppressed = true;
            },
            "FourthRoute.SuppressActThreeBossRewards");
        if (suppressed)
        {
            __result = replacement;
        }
        return !suppressed;
    }

    [HarmonyPostfix]
    public static void Postfix(
        Player player,
        AbstractRoom room,
        ref Task __result)
    {
        // The prefix above is part of the MVP fourth-route transition and must
        // always remain installed. Goddess blessings are a separate deferred
        // feature, so gate only this postfix instead of the whole patch class.
        if (!MvpFeatureFlags.BossBlessings)
        {
            return;
        }

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
