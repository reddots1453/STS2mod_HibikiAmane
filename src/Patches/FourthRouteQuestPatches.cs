using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(CardReward), nameof(CardReward.OnSkipped))]
public static class FourthRouteCardRewardSkippedPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardReward __instance)
    {
        Safe.Run(() =>
        {
            if (__instance.Player.RunState is not RunState runState
                || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
                || quest != FourthRouteQuest.Temperance)
                return;
            TaskHelper.RunSafely(FourthRouteProgressService.AddProgress(__instance.Player, quest));
        }, "FourthRoute.CardRewardSkipped");
    }
}

[HarmonyPatch(typeof(RelicReward), nameof(RelicReward.OnSkipped))]
public static class FourthRouteGenerosityRewardPatch
{
    [HarmonyPostfix]
    public static void Postfix(RelicReward __instance)
    {
        Safe.Run(() =>
        {
            GenerosityRouteRelic? relic = __instance.Player.Relics.OfType<GenerosityRouteRelic>()
                .FirstOrDefault(candidate => candidate.Stage >= 3);
            if (relic == null || __instance.Player.Deck.Cards.Count == 0) return;
            TaskHelper.RunSafely(RemoveOne(__instance.Player));
        }, "FourthRoute.GenerosityRelicSkip");
    }

    private static async Task RemoveOne(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(
            player, new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).ToList();
        await CardPileCmd.RemoveFromDeck(cards);
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
            if (!__state || !card.IsUpgraded || card is not (MaidenStrike or MaidenDefend or Transform)
                || card.Owner.RunState is not RunState runState
                || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
                || quest != FourthRouteQuest.Humility)
                return;
            TaskHelper.RunSafely(FourthRouteProgressService.AddProgress(card.Owner, quest));
        }, "FourthRoute.StarterUpgrade");
    }
}

[HarmonyPatch(typeof(NTreasureRoom), "OnProceedButtonReleased")]
public static class FourthRouteTreasureSkippedPatch
{
    private static readonly FieldInfo ClaimedField = AccessTools.Field(typeof(NTreasureRoom), "_hasRelicBeenClaimed");
    private static readonly FieldInfo RunStateField = AccessTools.Field(typeof(NTreasureRoom), "_runState");

    [HarmonyPrefix]
    public static void Prefix(NTreasureRoom __instance)
    {
        Safe.Run(() =>
        {
            if ((bool)ClaimedField.GetValue(__instance)!
                || RunStateField.GetValue(__instance) is not RunState runState
                || runState.Players.Count != 1
                || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
                || quest != FourthRouteQuest.Generosity)
                return;
            TaskHelper.RunSafely(FourthRouteProgressService.AddProgress(runState.Players[0], quest));
        }, "FourthRoute.TreasureSkipped");
    }
}
