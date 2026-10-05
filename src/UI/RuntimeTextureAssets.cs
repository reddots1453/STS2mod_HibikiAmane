using Godot;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace MaidenSuccubus.UI;

/// <summary>Demand-loaded images with a bounded warm cache. Live UI owns its own references.</summary>
public static class RuntimeTextureAssets
{
    private const string RootPath = "res://MaidenSuccubus/images/";
    private const string TextureRoot = "res://MaidenSuccubus/textures/";
    private static Dictionary<string, long>? _packedBytes;

    // Standard PNG import remaps accept both Texture2D and CompressedTexture2D
    // consumers and point to one shared native texture. No player-side import is needed.
    public static string GetResourcePath(string relativePath) => RootPath + relativePath;
    public static bool IsImported(string relativePath) =>
        ResourceLoader.Exists(TextureRoot + relativePath + ".ctex")
        && ResourceLoader.Exists(GetResourcePath(relativePath));

    private static long EstimatePackedBytes(string path, Texture2D texture)
    {
        if (_packedBytes == null)
        {
            _packedBytes = new(StringComparer.Ordinal);
            string manifest = TextureRoot + "manifest.json";
            if (Godot.FileAccess.FileExists(manifest))
            {
                using var json = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(manifest));
                foreach (var asset in json.RootElement.EnumerateObject())
                    _packedBytes[asset.Name] = asset.Value.GetProperty("gpu_bytes").GetInt64();
            }
        }
        return _packedBytes.TryGetValue(path, out long bytes)
            ? bytes : (long)texture.GetWidth() * texture.GetHeight() * 4;
    }
    private const long WarmBudget = 96L * 1024 * 1024;
    private sealed class Entry(WeakRef reference, long bytes)
    {
        internal readonly WeakRef Reference = reference;
        internal readonly long Bytes = bytes;
        internal Texture2D? Warm;
        internal LinkedListNode<string>? Position;
    }

    private static readonly Dictionary<string, Entry> Cache = new(StringComparer.Ordinal);
    private static readonly LinkedList<string> Recent = new();
    private static readonly HashSet<string> FailedLoads = new(StringComparer.Ordinal);
    private static long _warmBytes;

    public static bool Exists(string relativePath) => IsImported(relativePath)
        || Godot.FileAccess.FileExists(RootPath + relativePath);
    public static Texture2D? Load(string relativePath) => LoadCore(relativePath, null);

    private static Texture2D? LoadCore(string relativePath, byte[]? source)
    {
        if (Cache.TryGetValue(relativePath, out Entry? entry))
        {
            Texture2D? live = entry.Warm;
            if (live == null || !GodotObject.IsInstanceValid(live))
            {
                using Variant reference = entry.Reference.GetRef();
                live = reference.AsGodotObject() as Texture2D;
            }
            if (live != null && GodotObject.IsInstanceValid(live))
            {
                Retain(relativePath, entry, live);
                return live;
            }
            DropWarm(entry);
            entry.Reference.Dispose();
            Cache.Remove(relativePath);
        }
        if (FailedLoads.Contains(relativePath)) return null;

        var timer = Stopwatch.StartNew();
        string resourcePath = RootPath + relativePath;
        if (IsImported(relativePath))
        {
            // ResourceLoader's path cache and our weak cache share this object.
            // Do not decode, read back from the GPU, or create a second ImageTexture.
            var imported = ResourceLoader.Load<CompressedTexture2D>(GetResourcePath(relativePath));
            if (imported != null)
            {
                long packedBytes = EstimatePackedBytes(relativePath, imported);
                WeakRef? handle = GodotObject.WeakRef(imported);
                if (handle != null)
                {
                    entry = new Entry(handle, packedBytes);
                    Cache[relativePath] = entry;
                    Retain(relativePath, entry, imported);
                }
                MaidenSuccubusMod.Logger.Info(
                    $"[TextureMemory] imported={relativePath}; size={imported.GetWidth()}x{imported.GetHeight()}; "
                    + $"textureEstimateMiB={MiB(packedBytes):F2}; warmEstimateMiB={MiB(_warmBytes):F2}");
                return imported;
            }
            return Fail(relativePath, $"Unable to load compiled texture '{GetResourcePath(relativePath)}'.");
        }
        // Loose legacy development assets remain supported. Shipped packages use .ctex.
        byte[] bytes = source ?? Godot.FileAccess.GetFileAsBytes(resourcePath);
        if (bytes.Length == 0) return Fail(relativePath, $"Unable to read texture '{resourcePath}'.");
        using var image = new Image();
        Error error = relativePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || relativePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                ? image.LoadJpgFromBuffer(bytes) : image.LoadPngFromBuffer(bytes);
        if (error != Error.Ok || image.IsEmpty())
            return Fail(relativePath, $"Unable to decode texture '{resourcePath}' ({error}).");

        Texture2D texture = ImageTexture.CreateFromImage(image);
        long estimate = (long)image.GetWidth() * image.GetHeight() * 4;
        WeakRef? weakHandle = GodotObject.WeakRef(texture);
        if (weakHandle == null) return texture;
        entry = new Entry(weakHandle, estimate);
        Cache[relativePath] = entry;
        Retain(relativePath, entry, texture);
        if (estimate >= 2L * 1024 * 1024 || timer.ElapsedMilliseconds >= 75)
            MaidenSuccubusMod.Logger.Info(
                $"[TextureMemory] load={relativePath}; size={image.GetWidth()}x{image.GetHeight()}; "
                + $"elapsedMs={timer.ElapsedMilliseconds}; pixelsEstimateMiB={MiB(estimate):F1}; "
                + $"warmCount={Recent.Count}; warmEstimateMiB={MiB(_warmBytes):F1}; "
                + $"workingSetMiB={MiB(System.Environment.WorkingSet):F1}");
        return texture;
    }

    private static void Retain(string path, Entry entry, Texture2D texture)
    {
        if (entry.Position != null) Recent.Remove(entry.Position);
        else _warmBytes += entry.Bytes;
        entry.Warm = texture;
        entry.Position = Recent.AddLast(path);
        while (_warmBytes > WarmBudget && Recent.First is { } oldest )
            DropWarm(Cache[oldest.Value]);
        PruneDeadEntries();
    }

    private static void DropWarm(Entry entry)
    {
        if (entry.Position != null)
        {
            Recent.Remove(entry.Position);
            _warmBytes -= entry.Bytes;
            entry.Position = null;
        }
        // Never Dispose a texture: cards, sprites, animations or another mod may still use it.
        entry.Warm = null;
    }

    private static void PruneDeadEntries()
    {
        if (Cache.Count < 512) return;
        foreach (var pair in Cache.Where(pair => pair.Value.Warm == null).ToArray())
        {
            using Variant reference = pair.Value.Reference.GetRef();
            if (reference.AsGodotObject() is { } live && GodotObject.IsInstanceValid(live)) continue;
            pair.Value.Reference.Dispose();
            Cache.Remove(pair.Key);
        }
    }

    internal static void ReleasePrefix(string prefix, string reason)
    {
        foreach (var pair in Cache.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal)))
            DropWarm(pair.Value);
        LogRelease(reason);
    }

    internal static void ReleaseAll(string reason)
    {
        foreach (Entry entry in Cache.Values) DropWarm(entry);
        LogRelease(reason);
    }

    private static void LogRelease(string reason) => MaidenSuccubusMod.Logger.Info(
        $"[TextureMemory] release={reason}; warmCount={Recent.Count}; "
        + $"warmEstimateMiB={MiB(_warmBytes):F1}; workingSetMiB={MiB(System.Environment.WorkingSet):F1}");
    private static double MiB(long bytes) => bytes / 1048576d;

    /// <summary>Cache generated resources by source content, including art updates between builds.</summary>
    public static string PrepareResource(string relativePath, string userResourcePath,
        string fallbackPath, bool reuseExisting = false)
    {
        if (IsImported(relativePath)) return GetResourcePath(relativePath);
        if (reuseExisting && ResourceLoader.Exists(userResourcePath)) return userResourcePath;
        byte[] bytes = Godot.FileAccess.GetFileAsBytes(RootPath + relativePath);
        if (bytes.Length == 0) return fallbackPath;
        string fingerprint = Convert.ToHexString(SHA256.HashData(bytes));
        string stampPath = userResourcePath + ".source.sha256";
        if (Godot.FileAccess.FileExists(stampPath)
            && Godot.FileAccess.GetFileAsString(stampPath).Trim() == fingerprint
            && ResourceLoader.Exists(userResourcePath))
        {
            MaidenSuccubusMod.Logger.Info($"[TextureMemory] resourceReuse={relativePath}");
            return userResourcePath;
        }

        Texture2D? texture = LoadCore(relativePath, bytes);
        if (texture == null) return fallbackPath;
        var timer = Stopwatch.StartNew();
        var flags = userResourcePath.EndsWith(".res", StringComparison.OrdinalIgnoreCase)
            ? ResourceSaver.SaverFlags.Compress : ResourceSaver.SaverFlags.None;
        Error error = ResourceSaver.Save(texture, userResourcePath, flags);
        if (error != Error.Ok)
        {
            MaidenSuccubusMod.Logger.Warn($"Unable to prepare texture resource '{userResourcePath}' ({error}); using '{fallbackPath}'.");
            return fallbackPath;
        }
        using (var stamp = Godot.FileAccess.Open(stampPath, Godot.FileAccess.ModeFlags.Write))
            stamp?.StoreString(fingerprint);
        MaidenSuccubusMod.Logger.Info($"[TextureMemory] resourceSave={relativePath}; elapsedMs={timer.ElapsedMilliseconds}");
        return userResourcePath;
    }

    private static Texture2D? Fail(string relativePath, string message)
    {
        FailedLoads.Add(relativePath);
        MaidenSuccubusMod.Logger.Warn(message);
        return null;
    }
}
