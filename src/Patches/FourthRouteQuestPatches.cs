using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(CardReward), nameof(CardReward.OnSkipped))]
public static class FourthRouteCardRewardSkippedPatch
{
    private static readonly ConditionalWeakTable<CardReward, object> Counted = new();
    [HarmonyPostfix]
    public static void Postfix(CardReward __instance)
    {
        Safe.Run(() =>
        {
            if (__instance.Player.RunState is not RunState runState
                || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
                || quest != FourthRouteQuest.Temperance)
                return;
            if (Counted.TryGetValue(__instance, out _)) return;
            Counted.Add(__instance, new object());
            TaskHelper.RunSafely(FourthRouteProgressService.AddProgress(__instance.Player, quest));
        }, "FourthRoute.CardRewardSkipped");
    }
}

[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Upgrade), [typeof(CardModel), typeof(CardPreviewStyle)])]
public static class FourthRouteStarterUpgradePatch
{
    [HarmonyPrefix]
    public static void Prefix(CardModel card, out bool __state)
    {
        bool wasUpgradable = false;
        Safe.Run(() => wasUpgradable = card.IsUpgradable, "FourthRoute.StarterUpgrade.Before");
        __state = wasUpgradable;
    }

    [HarmonyPostfix]
    public static void Postfix(CardModel card, bool __state)
    {
        Safe.Run(() =>
        {
            if (!__state || !card.IsUpgraded
                || card.Pile?.Type != PileType.Deck
                || card is not (MaidenStrike or MaidenDefend or Transform or DarkElement)
                || card.Owner.RunState is not RunState runState
                || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
                || quest != FourthRouteQuest.Humility)
                return;
            TaskHelper.RunSafely(FourthRouteProgressService.AddProgress(card.Owner, quest));
        }, "FourthRoute.StarterUpgrade");
    }
}

// Generosity must be an explicit mutually exclusive offering choice. Merely
// leaving a chest/reward screen is NOT an offering and no longer grants credit.
