using HarmonyLib;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Runs;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
internal static class TextureCacheRunCleanupPatch
{
    [HarmonyPostfix]
    private static void Postfix() => Safe.Run(
        () => RuntimeTextureAssets.ReleaseAll("run-cleanup"), "TextureCache.RunCleanup");
}

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuClosed))]
internal static class TextureCacheLibraryClosedPatch
{
    [HarmonyPostfix]
    private static void Postfix() => Safe.Run(
        () => RuntimeTextureAssets.ReleasePrefix("cards/", "library-closed"), "TextureCache.LibraryClosed");
}
