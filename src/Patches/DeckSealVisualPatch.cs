using Godot;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MaidenSuccubus.Core.Seals;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// The permanent deck remains unchanged. This patch only decorates cards that
/// the current route will exclude from the next combat snapshot.
/// </summary>
[HarmonyPatch]
public static class DeckSealVisualPatch
{
    private static readonly Color SealedColor =
        new(0.38f, 0.38f, 0.46f, 0.82f);
    private static readonly ConditionalWeakTable<NGridCardHolder, OwnedVisualOverride<Color>> Tints = new();

    internal static bool HasSealTint(NGridCardHolder holder) =>
        Tints.TryGetValue(holder, out var tint) && tint.Matches(holder.Modulate);

    [HarmonyPatch(typeof(NGridCardHolder), "OnCardReassigned")]
    [HarmonyPostfix]
    public static void AfterCardReassigned(NGridCardHolder __instance)
    {
        Safe.Run(() => Refresh(__instance), nameof(AfterCardReassigned));
    }

    // A newly allocated/reused holder can be assigned before it has a deck-view parent.
    [HarmonyPatch(typeof(NDeckViewScreen), "DisplayCards")]
    [HarmonyPostfix]
    public static void AfterDeckDisplayed(NDeckViewScreen __instance) => Safe.Run(() =>
    {
        var pending = new Stack<Node>();
        pending.Push(__instance);
        while (pending.TryPop(out var node))
        {
            if (node is NGridCardHolder holder) Refresh(holder);
            else foreach (Node child in node.GetChildren()) pending.Push(child);
        }
    }, nameof(AfterDeckDisplayed));

    [HarmonyPatch(typeof(NGridCardHolder), nameof(NGridCardHolder.OnFreedToPool))]
    [HarmonyPrefix]
    public static void BeforeReturnedToPool(NGridCardHolder __instance) =>
        Safe.Run(() => Restore(__instance), nameof(BeforeReturnedToPool));

    private static void Restore(NGridCardHolder holder)
    {
        if (!Tints.TryGetValue(holder, out var tint)) return;
        holder.Modulate = tint.Restore(holder.Modulate);
        Tints.Remove(holder);
    }

    private static void Refresh(NGridCardHolder holder)
    {
        Restore(holder);
        if (!IsInDeckView(holder) || SealPresentation.DescriptionKey(holder.CardModel) == null) return;
        holder.Modulate = Tints.GetOrCreateValue(holder).Apply(holder.Modulate, color => color * SealedColor);
    }

    [HarmonyPatch(typeof(NCardHolder), "CreateHoverTips")]
    [HarmonyPrefix]
    public static bool BeforeCreateHoverTips(NCardHolder __instance)
    {
        bool runOriginal = true;
        Safe.Run(
            () =>
            {
                if (!IsInDeckView(__instance))
                {
                    return;
                }

                string? descriptionKey = SealPresentation.DescriptionKey(__instance.CardModel);
                if (descriptionKey == null) return;

                var card = __instance.CardNode?.Model;
                if (card == null)
                {
                    return;
                }

                var tips = card.HoverTips.ToList();
                tips.Add(new HoverTip(
                    new LocString(
                        "static_hover_tips",
                        "MAIDENSUCCUBUS_SEALED_CARD.title"),
                    new LocString(
                        "static_hover_tips",
                        descriptionKey)));
                NHoverTipSet.CreateAndShow(__instance, tips)
                    ?.SetAlignmentForCardHolder(__instance);
                runOriginal = false;
            },
            nameof(DeckSealVisualPatch));
        return runOriginal;
    }

    internal static bool IsInDeckView(Node node)
    {
        for (Node? current = node; current != null; current = current.GetParent())
        {
            if (current is NDeckViewScreen)
            {
                return true;
            }
        }

        return false;
    }
}
