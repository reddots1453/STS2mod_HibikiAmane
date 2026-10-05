using Godot;
using HarmonyLib;
using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// Shares imported HD art across native and third-party card surfaces and
/// selects live route variants. PortraitPath exposes the real imported image;
/// typed CompressedTexture2D consumers no longer need a vanilla placeholder.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Portrait), MethodType.Getter)]
[HarmonyPriority(Priority.Last)]
public static class MaidenCardModelPortraitPatch
{
    private static bool _loggedFirstReplacement;

    // Return the actual model texture before vanilla resolves the compatibility
    // path. Keep the postfix as well for frameworks that supply their own result.
    public static bool Prefix(CardModel __instance, ref Texture2D __result)
    {
        if (!CardArtAssets.IsMaidenCard(__instance)) return true;
        Texture2D? texture = null;
        Safe.Run(() => texture = CardArtAssets.GetPortraitTexture(__instance),
            nameof(MaidenCardModelPortraitPatch));
        if (texture == null) return true;
        __result = texture;
        return false;
    }

    public static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (!CardArtAssets.IsMaidenCard(__instance))
        {
            return;
        }

        Texture2D? texture = null;
        Safe.Run(
            () => texture = CardArtAssets.GetPortraitTexture(__instance),
            nameof(MaidenCardModelPortraitPatch));
        if (texture == null)
        {
            return;
        }

        __result = texture;
        if (!_loggedFirstReplacement)
        {
            _loggedFirstReplacement = true;
            MaidenSuccubusMod.Logger.Info(
                $"Card portrait pipeline active: {__instance.Id.Entry} -> "
                + $"{texture.GetType().Name} {texture.GetSize()}.");
        }
    }
}

/// <summary>
/// Reapplies the model-selected texture directly to initialized NCard nodes.
/// This is a compatibility fallback for card UI extensions that cache or
/// replace the portrait after reading CardModel.Portrait.
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
[HarmonyPriority(Priority.Last)]
public static class MaidenCardPortraitPresentationPatch
{
    private static bool _loggedFirstPresentation;
    private static readonly FieldInfo? PortraitField =
        AccessTools.DeclaredField(typeof(NCard), "_portrait");
    private static readonly FieldInfo? AncientPortraitField =
        AccessTools.DeclaredField(typeof(NCard), "_ancientPortrait");

    public static void Postfix(NCard __instance) => Safe.Run(
        () => Apply(__instance),
        nameof(MaidenCardPortraitPresentationPatch));

    internal static void Apply(NCard card)
    {
        CardModel? model = card.Model;
        if (model == null || !CardArtAssets.IsMaidenCard(model))
        {
            return;
        }

        Texture2D? texture = CardArtAssets.GetPortraitTexture(model);
        if (texture == null)
        {
            return;
        }

        // NCard.Reload has already assigned Model.Portrait when its postfixes
        // run. Replace the initialized backing nodes synchronously so the
        // custom image does not depend on another mod scheduling a later card
        // refresh, and so pooled/off-screen compendium cards cannot lose a
        // deferred assignment before it is processed.
        TextureRect? portrait = PortraitField?.GetValue(card) as TextureRect
            ?? card.GetNodeOrNull<TextureRect>("%Portrait");
        TextureRect? ancientPortrait =
            AncientPortraitField?.GetValue(card) as TextureRect
            ?? card.GetNodeOrNull<TextureRect>("%AncientPortrait");

        if (portrait != null)
        {
            portrait.Texture = texture;
        }
        if (ancientPortrait != null)
        {
            ancientPortrait.Texture = texture;
        }
        if (!_loggedFirstPresentation && (portrait != null || ancientPortrait != null))
        {
            _loggedFirstPresentation = true;
            MaidenSuccubusMod.Logger.Info(
                $"Card portrait presentation active: {model.Id.Entry} -> "
                + $"{texture.GetType().Name} {texture.GetSize()}.");
        }
    }
}

/// <summary>Native previews refresh portraits without going through Reload.</summary>
[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
[HarmonyPriority(Priority.Last)]
public static class MaidenCardPortraitRefreshPatch
{
    public static void Postfix(NCard __instance) => Safe.Run(
        () => MaidenCardPortraitPresentationPatch.Apply(__instance),
        nameof(MaidenCardPortraitRefreshPatch));
}

/// <summary>
/// Finalize the whole visual refresh after framework/UI extensions. This also
/// covers upgrade previews and card-pool reuse if the texture getter is inlined.
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
[HarmonyPriority(Priority.Last)]
public static class MaidenCardVisualRefreshPatch
{
    public static void Postfix(NCard __instance) => Safe.Run(
        () => MaidenCardPortraitPresentationPatch.Apply(__instance),
        nameof(MaidenCardVisualRefreshPatch));
}

/// <summary>
/// Reapply HD art after every inspect-screen extension has finished. This is
/// the built-in large-card compatibility layer for mods that overwrite the
/// NCard portrait from CardModel.PortraitPath during UpdateCardDisplay.
/// </summary>
[HarmonyPatch(typeof(NInspectCardScreen), "UpdateCardDisplay")]
[HarmonyPriority(Priority.Last)]
public static class MaidenInspectCardPortraitPatch
{
    public static void Postfix(NInspectCardScreen __instance) => Safe.Run(
        () =>
        {
            NCard? card = __instance.GetNodeOrNull<NCard>("Card");
            if (card != null)
            {
                MaidenCardPortraitPresentationPatch.Apply(card);
            }
        },
        nameof(MaidenInspectCardPortraitPatch));
}
