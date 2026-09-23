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
/// Supplies loose HD art at the same model-level texture getter consumed by
/// every vanilla card surface. The separately exposed PortraitPath remains a
/// valid vanilla CompressedTexture2D so third-party inspect-card patches that
/// insist on loading that concrete type cannot fail their cast.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Portrait), MethodType.Getter)]
[HarmonyPriority(Priority.Last)]
public static class MaidenCardModelPortraitPatch
{
    private static bool _loggedFirstReplacement;

    public static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (!CardArtAssets.IsMaidenCard(__instance))
        {
            return;
        }

        Texture2D? texture = null;
        Safe.Run(
            () => texture = CardArtAssets.GetPortraitTexture(__instance.GetType()),
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

        Texture2D? texture = CardArtAssets.GetPortraitTexture(model.GetType());
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
    }
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
