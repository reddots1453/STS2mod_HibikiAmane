using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;

namespace MaidenSuccubus.Patches;

/// <summary>Large art belongs to visible UI, not the run's permanent preload set.</summary>
[HarmonyPatch(typeof(PreloadManager), "LoadAssetSets")]
internal static class ImportedTexturePreloadPatch
{
    private static void Prefix(ref IEnumerable<string>[] assetSets)
    {
        // Materialize now: preserve the native unload/load diff, not a global cache toggle.
        assetSets = assetSets.Select(set => set.Where(path => !IsDemandArt(path)).ToArray()).ToArray();
    }

    private static bool IsDemandArt(string path) =>
        path.StartsWith("res://MaidenSuccubus/textures/cards/", StringComparison.Ordinal)
        || path.StartsWith("res://MaidenSuccubus/textures/cutscenes/", StringComparison.Ordinal)
        || path.StartsWith("res://MaidenSuccubus/images/cards/", StringComparison.Ordinal)
        || path.StartsWith("res://MaidenSuccubus/images/cutscenes/", StringComparison.Ordinal);
}
