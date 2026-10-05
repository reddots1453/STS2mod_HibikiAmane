using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// EnchantmentModel.Icon has a CompressedTexture2D return type. Our loose PNGs
/// are decoded to ImageTexture, so the native getter cannot return them. Keep
/// its typed contract intact and supply the custom texture at Texture2D UI sites.
/// </summary>
[HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.Icon), MethodType.Getter)]
internal static class EnchantmentCompressedIconCompatibilityPatch
{
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(EnchantmentModel __instance, ref CompressedTexture2D __result)
    {
        if (!HasLooseIcon(__instance)) return true;
        __result = PreloadManager.Cache.GetCompressedTexture2D(EnchantmentModel.MissingIconPath);
        return false;
    }

    private static bool HasLooseIcon(EnchantmentModel enchantment) =>
        enchantment.IconPath.StartsWith("user://maiden_enchantment_", StringComparison.Ordinal);

    internal static Texture2D? CustomIcon(EnchantmentModel enchantment)
    {
        return HasLooseIcon(enchantment)
            ? ResourceLoader.Load<Texture2D>(enchantment.IconPath)
            : null;
    }
}

[HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.HoverTip), MethodType.Getter)]
internal static class EnchantmentHoverTipIconPatch
{
    private static void Postfix(EnchantmentModel __instance, ref HoverTip __result)
    {
        HoverTip result = __result;
        Safe.Run(() =>
        {
            Texture2D? icon = EnchantmentCompressedIconCompatibilityPatch.CustomIcon(__instance);
            if (icon is not null)
                result = new HoverTip(__instance.Title, __instance.DynamicDescription, icon);
        }, nameof(EnchantmentHoverTipIconPatch));
        __result = result;
    }
}

[HarmonyPatch(typeof(NCard), "UpdateEnchantmentVisuals")]
internal static class EnchantmentCardIconPatch
{
    private static void Postfix(NCard __instance)
    {
        Safe.Run(() =>
        {
            if (__instance.Model?.Enchantment is not { } enchantment
                || EnchantmentCompressedIconCompatibilityPatch.CustomIcon(enchantment)
                    is not { } icon)
                return;
            if (__instance.GetNodeOrNull<TextureRect>("%Enchantment/Icon") is { } rect)
                rect.Texture = icon;
        }, nameof(EnchantmentCardIconPatch));
    }
}


/// <summary>
/// The reveal shader reads a separate viewport icon, not NCard's normal tab.
/// Replace that Texture2D after native _Ready sets its compressed fallback.
/// </summary>
[HarmonyPatch(typeof(NCardEnchantVfx), nameof(NCardEnchantVfx._Ready))]
internal static class EnchantmentAnimationIconPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCardEnchantVfx __instance, CardModel? ____cardModel)
    {
        Safe.Run(() =>
        {
            // Read the VFX's model directly; adding its preview child can be deferred.
            EnchantmentModel? enchantment = ____cardModel?.Enchantment;
            if (enchantment == null
                || EnchantmentCompressedIconCompatibilityPatch.CustomIcon(enchantment) is not { } icon)
                return;
            if (__instance.GetNodeOrNull<TextureRect>("%EnchantmentInViewport/Icon") is { } rect)
                rect.Texture = icon;
        }, nameof(EnchantmentAnimationIconPatch));
    }
}
