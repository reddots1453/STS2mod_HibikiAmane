using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// RitsuLib's character filter normally matches only CharacterModel.CardPool.
/// This character deliberately uses separate route and generated-card pools,
/// so the compendium predicate must group those pools without merging reward
/// generation pools.
/// </summary>
[HarmonyAfter("com.ritsukage.sts2-RitsuLib.framework-character-assets")]
[HarmonyPriority(Priority.Last)]
[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
public static class CardLibraryRoutePoolPatch
{
    public static void Postfix(
        Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters,
        Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters)
    {
        Safe.Run(
            () =>
            {
                KeyValuePair<CharacterModel, NCardPoolFilter> entry =
                    ____cardPoolFilters.FirstOrDefault(pair =>
                        pair.Key is MaidenSuccubusCharacter);

                if (entry.Key is null || entry.Value is null)
                {
                    MaidenSuccubusMod.Logger.Warn(
                        "[CardLibraryRoutePoolPatch] Maiden character filter was not registered by RitsuLib.");
                    return;
                }

                ____poolFilters[entry.Value] = IsMaidenSuccubusCompendiumCard;
                MaidenSuccubusMod.Logger.Info(
                    "[CardLibraryRoutePoolPatch] Installed three-route compendium filter.");
            },
            nameof(CardLibraryRoutePoolPatch));
    }

    private static bool IsMaidenSuccubusCompendiumCard(CardModel card) =>
        card is IMaidenSuccubusRouteCard
        || card is MaidenStrike or MaidenDefend;
}
