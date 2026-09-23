using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// Installs loose HD art directly on this mod's NCard instances. The model's
/// path remains a valid vanilla CompressedTexture2D so third-party inspect-card
/// patches cannot fail their compressed-texture cast.
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
[HarmonyPriority(Priority.Last)]
public static class MaidenCardPortraitPresentationPatch
{
    public static void Postfix(NCard __instance) => Safe.Run(
        () => Apply(__instance),
        nameof(MaidenCardPortraitPresentationPatch));

    internal static void Apply(NCard card)
    {
        CardModel? model = card.Model;
        if (model == null
            || model.GetType().Assembly != typeof(MaidenSuccubusMod).Assembly)
        {
            return;
        }

        Texture2D? texture = CardArtAssets.GetPortraitTexture(model.GetType());
        if (texture == null)
        {
            return;
        }

        card.GetNodeOrNull<TextureRect>("%Portrait")?.SetDeferred(
            TextureRect.PropertyName.Texture,
            texture);
        card.GetNodeOrNull<TextureRect>("%AncientPortrait")?.SetDeferred(
            TextureRect.PropertyName.Texture,
            texture);
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
