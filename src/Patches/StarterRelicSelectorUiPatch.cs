using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch]
public static class StarterRelicSelectorUiPatch
{
    [HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
    [HarmonyPostfix]
    public static void SelectPostfix(NCharacterSelectScreen __instance, NCharacterSelectButton __0, CharacterModel __1) =>
        Safe.Run(() => StarterRelicSelector.Selected(__instance, __0, __1), "StarterRelic.SelectCharacter");

    [HarmonyPatch(typeof(NCharacterSelectScreen), "OnEmbarkPressed")]
    [HarmonyPostfix]
    public static void EmbarkPostfix(NCharacterSelectScreen __instance) => Refresh(__instance);

    [HarmonyPatch(typeof(NCharacterSelectScreen), "OnUnreadyPressed")]
    [HarmonyPostfix]
    public static void UnreadyPostfix(NCharacterSelectScreen __instance) => Refresh(__instance);

    [HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.PlayerChanged))]
    [HarmonyPostfix]
    public static void PlayerChangedPostfix(NCharacterSelectScreen __instance) => Refresh(__instance);

    private static void Refresh(NCharacterSelectScreen screen) =>
        Safe.Run(() => StarterRelicSelector.RefreshIfPresent(screen), "StarterRelic.Refresh");

    [HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.BeginRun))]
    [HarmonyPrefix]
    public static void BeginPrefix(NCharacterSelectScreen __instance) => Close(__instance);

    [HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuClosed))]
    [HarmonyPrefix]
    public static void ClosePrefix(NCharacterSelectScreen __instance) => Close(__instance);

    private static void Close(NCharacterSelectScreen screen) =>
        Safe.Run(() => StarterRelicSelector.Close(screen), "StarterRelic.Close");
}
