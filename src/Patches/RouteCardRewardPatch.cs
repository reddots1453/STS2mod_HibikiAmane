using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Rewards;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(
    typeof(Hook),
    nameof(Hook.TryModifyCardRewardOptions))]
public static class RouteCardRewardPatch
{
    public static void Prefix(
        Player __1,
        List<CardCreationResult> __2,
        CardCreationOptions __3)
    {
        Safe.Run(
            () => RouteCardRewardService.TryReplaceOne(__1, __2, __3),
            nameof(RouteCardRewardPatch));
    }
}
