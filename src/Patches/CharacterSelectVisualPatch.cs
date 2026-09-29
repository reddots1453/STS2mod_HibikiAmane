using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MaidenSuccubus.Characters;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// CharacterModel's select-icon API is typed as CompressedTexture2D, while
/// loose reviewed mod PNGs are decoded as ImageTexture. Keep the vanilla-safe
/// path for the original button initialization, then replace only the Maiden
/// button texture after the original lifecycle writes it.
/// </summary>
[HarmonyPatch]
public static class MaidenCharacterSelectVisualPatch
{
    private const string VanillaSelectIcon =
        "res://images/packed/character_select/char_select_ironclad.png";
    private const string VanillaLockedSelectIcon =
        "res://images/packed/character_select/char_select_ironclad_locked.png";

    // Protect the concrete return type as well as the paths. Third-party
    // character-select patches may load the path into an ImageTexture before
    // the original button code asks for its CompressedTexture2D property.
    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectIcon), MethodType.Getter)]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool SelectIconPrefix(CharacterModel __instance, ref CompressedTexture2D __result)
    {
        if (__instance is not MaidenSuccubusCharacter)
            return true;
        CompressedTexture2D? fallback = null;
        Safe.Run(() => fallback = ResourceLoader.Load<CompressedTexture2D>(VanillaSelectIcon),
            nameof(SelectIconPrefix));
        if (fallback == null)
            return true;
        __result = fallback;
        return false;
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectLockedIcon), MethodType.Getter)]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool LockedIconPrefix(CharacterModel __instance, ref CompressedTexture2D __result)
    {
        if (__instance is not MaidenSuccubusCharacter)
            return true;
        CompressedTexture2D? fallback = null;
        Safe.Run(() => fallback = ResourceLoader.Load<CompressedTexture2D>(VanillaLockedSelectIcon),
            nameof(LockedIconPrefix));
        if (fallback == null)
            return true;
        __result = fallback;
        return false;
    }

    // The profile's user:// PNGs are ImageTexture resources. The vanilla
    // CharacterSelectIcon getters load CompressedTexture2D, so normalize only
    // our path before NCharacterSelectButton.Init invokes those getters.
    [HarmonyPatch(typeof(CharacterModel), "CharacterSelectIconPath", MethodType.Getter)]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void SelectIconPathPostfix(CharacterModel __instance, ref string __result)
    {
        bool isMaiden = false;
        Safe.Run(() => isMaiden = __instance is MaidenSuccubusCharacter,
            nameof(SelectIconPathPostfix));
        if (isMaiden)
            __result = VanillaSelectIcon;
    }

    [HarmonyPatch(typeof(CharacterModel), "CharacterSelectLockedIconPath", MethodType.Getter)]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void LockedIconPathPostfix(CharacterModel __instance, ref string __result)
    {
        bool isMaiden = false;
        Safe.Run(() => isMaiden = __instance is MaidenSuccubusCharacter,
            nameof(LockedIconPathPostfix));
        if (isMaiden)
            __result = VanillaLockedSelectIcon;
    }

    [HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init))]
    [HarmonyPostfix]
    public static void InitPostfix(NCharacterSelectButton __instance) =>
        Refresh(__instance);

#if DEBUG
    [HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init))]
    [HarmonyPrefix]
    public static void InitDiagnosticPrefix(CharacterModel __0) => Safe.Run(
        () => MaidenSuccubusMod.Logger.Info(
            $"[CharacterSelectInit] {__0.Id.Entry} ({__0.GetType().FullName})"),
        nameof(InitDiagnosticPrefix));
#endif

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
                ? "ui/character_select/hibiki_amane_character_icon_outline_v02_256.png"
                : "ui/character_select/hibiki_amane_character_icon_v02_256.png";
            Texture2D? texture = RuntimeTextureAssets.Load(file);
            if (texture != null)
            {
                button.GetNode<TextureRect>("%Icon").Texture = texture;
            }
        },
        nameof(MaidenCharacterSelectVisualPatch));
}
