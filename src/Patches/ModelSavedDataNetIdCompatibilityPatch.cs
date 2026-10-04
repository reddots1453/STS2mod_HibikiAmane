using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// Modern game builds encode saved-property names as net IDs. Framework model
/// data must remain serializable after the game's cache has been initialized.
/// </summary>
[HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
internal static class ModelSavedDataNetIdCompatibilityPatch
{
    private const string PropertyName = "RitsuLib_ModelSavedData";

    // Older builds serialize property names directly and have no such maps.
    private static bool Prepare() =>
        AccessTools.Field(typeof(ModelIdSerializationCache), "_propertyNameToNetIdMap") != null
        && AccessTools.Field(typeof(ModelIdSerializationCache), "_netIdToPropertyNameMap") != null
        && AccessTools.Property(typeof(ModelIdSerializationCache), "PropertyIdBitSize") != null;

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix() => Safe.Run(EnsurePropertyMapping, "ModelSavedDataNetIdCompatibility");

    private static void EnsurePropertyMapping()
    {
        var type = typeof(ModelIdSerializationCache);
        var forward = AccessTools.Field(type, "_propertyNameToNetIdMap")?.GetValue(null)
            as Dictionary<string, int>;
        var reverse = AccessTools.Field(type, "_netIdToPropertyNameMap")?.GetValue(null)
            as List<string>;
        var bitSize = AccessTools.Property(type, "PropertyIdBitSize");
        var setter = bitSize?.GetSetMethod(nonPublic: true);
        if (forward == null || reverse == null || setter == null
            || bitSize?.GetValue(null) is not int oldBits)
            throw new InvalidOperationException("Saved-property cache layout is not supported.");

        if (forward.TryGetValue(PropertyName, out int existing))
        {
            if (existing < 0 || existing >= reverse.Count || reverse[existing] != PropertyName)
                throw new InvalidOperationException("Framework saved-property net ID is inconsistent.");
            return; // A framework version that already registers it needs no repair.
        }

        // Preserve every existing ID. Do not reorder or reset another mod's cache.
        if (forward.Any(pair => pair.Value < 0 || pair.Value >= reverse.Count
            || reverse[pair.Value] != pair.Key))
            throw new InvalidOperationException("Existing saved-property maps are inconsistent.");
        int id = reverse.IndexOf(PropertyName);
        if (id < 0)
        {
            id = reverse.Count;
            reverse.Add(PropertyName);
        }
        forward.Add(PropertyName, id);
        int requiredBits = reverse.Count <= 1 ? 0 : (int)Math.Ceiling(Math.Log2(reverse.Count));
        int newBits = Math.Max(oldBits, requiredBits);
        setter.Invoke(null, [newBits]);
        // Match RitsuLib's attached-state registration: retain the cache hash and
        // the original SavedProperties serializer/checksum, including the payload.
        MaidenSuccubusMod.Logger.Info(
            $"[ModelSavedDataCompat] Registered {PropertyName}: netId={id}; propertyBits={oldBits}->{newBits}.");
    }
}
