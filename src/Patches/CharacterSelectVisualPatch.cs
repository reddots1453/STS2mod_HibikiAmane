using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MaidenSuccubus.Characters;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// CharacterModel's select-icon API is typed as CompressedTexture2D, while
/// loose reviewed mod PNGs are decoded as ImageTexture. Keep the vanilla-safe
/// path for preloading and replace only the Maiden button texture after the
/// original character-select lifecycle writes it.
/// </summary>
[HarmonyPatch]
public static class MaidenCharacterSelectVisualPatch
{
    [HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init))]
    [HarmonyPostfix]
    public static void InitPostfix(NCharacterSelectButton __instance) =>
        Refresh(__instance);

    [HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.LockForAnimation))]
    [HarmonyPostfix]
    public static void LockPostfix(NCharacterSelectButton __instance) =>
        Refresh(__instance);

    [HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.DebugUnlock))]
    [HarmonyPostfix]
    public static void DebugUnlockPostfix(NCharacterSelectButton __instance) =>
        Refresh(__instance);

    [HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.UnlockIfPossible))]
    [HarmonyPostfix]
    public static void UnlockIfPossiblePostfix(NCharacterSelectButton __instance) =>
        Refresh(__instance);

    private static void Refresh(NCharacterSelectButton button) => Safe.Run(
        () =>
        {
            if (button.Character is not MaidenSuccubusCharacter)
            {
                return;
            }

            string file = button.IsLocked
                ? "ui/core/hibiki_amane_character_icon_outline_256.png"
                : "ui/core/hibiki_amane_character_icon_256.png";
            Texture2D? texture = RuntimeTextureAssets.Load(file);
            if (texture != null)
            {
                button.GetNode<TextureRect>("%Icon").Texture = texture;
            }
        },
        nameof(MaidenCharacterSelectVisualPatch));
}
