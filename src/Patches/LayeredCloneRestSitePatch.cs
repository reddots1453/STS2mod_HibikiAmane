using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

// Native CloneRestSiteOption filters by `Enchantment is Clone`. Only layered
// cards are supplemented; native cards remain entirely in the original path.
[HarmonyPatch(typeof(CloneRestSiteOption), nameof(CloneRestSiteOption.OnSelect))]
internal static class LayeredCloneRestSitePatch
{
    private static void Prefix(CloneRestSiteOption __instance, out CardModel[] __state)
    {
        CardModel[] cards = [];
        Safe.Run(() => cards = ((Player)AccessTools.PropertyGetter(typeof(RestSiteOption), "Owner")
            .Invoke(__instance, null)!).Deck.Cards
            .Where(card => card.Enchantment is LayeredEnchantment)
            .SelectMany(card => ((LayeredEnchantment)card.Enchantment!).Layers.OfType<Clone>().Select(_ => card))
            .ToArray(), "LayeredEnchantments.CloneRestSnapshot");
        __state = cards;
    }

    private static void Postfix(ref Task<bool> __result, CardModel[] __state)
    {
        Task<bool> result = __result;
        Safe.Run(() =>
        {
            if (__state.Length > 0) result = Complete(result, __state);
        }, "LayeredEnchantments.CloneRestComplete");
        __result = result;
    }

    private static async Task<bool> Complete(Task<bool> original, CardModel[] cards)
    {
        if (!await original) return false;
        List<CardPileAddResult> results = [];
        foreach (var card in cards)
        {
            if (!card.Owner.Deck.Cards.Contains(card)) continue;
            var clone = card.Owner.RunState.CloneCard(card);
            results.Add(await CardPileCmd.Add(clone, PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd(results, style: CardPreviewStyle.MessyLayout);
        return true;
    }
}
