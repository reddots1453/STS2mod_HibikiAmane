using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Runs;
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

    [HarmonyPatch(typeof(NGridCardHolder), "OnCardReassigned")]
    [HarmonyPostfix]
    public static void AfterCardReassigned(NGridCardHolder __instance)
    {
        Safe.Run(
            () =>
            {
                if (!IsInDeckView(__instance))
                {
                    return;
                }

                __instance.Modulate = IsSealed(__instance)
                    ? SealedColor
                    : Colors.White;
            },
            nameof(DeckSealVisualPatch));
    }

    [HarmonyPatch(typeof(NCardHolder), "CreateHoverTips")]
    [HarmonyPrefix]
    public static bool BeforeCreateHoverTips(NCardHolder __instance)
    {
        bool runOriginal = true;
        Safe.Run(
            () =>
            {
                if (!IsInDeckView(__instance) || !IsSealed(__instance))
                {
                    return;
                }

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
                        "MAIDENSUCCUBUS_SEALED_CARD.description")));
                NHoverTipSet.CreateAndShow(__instance, tips)
                    ?.SetAlignmentForCardHolder(__instance);
                runOriginal = false;
            },
            nameof(DeckSealVisualPatch));
        return runOriginal;
    }

    private static bool IsSealed(NCardHolder holder)
    {
        var card = holder.CardNode?.Model;
        return card?.RunState is RunState runState
            && CombatSealQuery.IsSealed(runState, card);
    }

    private static bool IsInDeckView(Node node)
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
