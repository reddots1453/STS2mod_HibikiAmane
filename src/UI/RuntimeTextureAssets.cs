using Godot;

namespace MaidenSuccubus.UI;

/// <summary>
/// Loads loose mod images without relying on Godot's import database. Debug
/// deployments deliberately mirror PNG files beside the game executable, and
/// packed releases expose the same paths through FileAccess.
/// </summary>
public static class RuntimeTextureAssets
{
    private const string RootPath = "res://MaidenSuccubus/images/";

    private static readonly Dictionary<string, Texture2D> Cache =
        new(StringComparer.Ordinal);
    private static readonly HashSet<string> FailedLoads =
        new(StringComparer.Ordinal);

    public static bool Exists(string relativePath) =>
        Godot.FileAccess.FileExists(RootPath + relativePath);

    public static Texture2D? Load(string relativePath)
    {
        if (Cache.TryGetValue(relativePath, out Texture2D? cached)
            && GodotObject.IsInstanceValid(cached))
        {
            return cached;
        }
        if (FailedLoads.Contains(relativePath))
        {
            return null;
        }

        string resourcePath = RootPath + relativePath;
        byte[] bytes = Godot.FileAccess.GetFileAsBytes(resourcePath);
        if (bytes.Length == 0)
        {
            return Fail(relativePath, $"Unable to read texture '{resourcePath}'.");
        }

        using var image = new Image();
        Error error = relativePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || relativePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                ? image.LoadJpgFromBuffer(bytes)
                : image.LoadPngFromBuffer(bytes);
        if (error != Error.Ok || image.IsEmpty())
        {
            return Fail(
                relativePath,
                $"Unable to decode texture '{resourcePath}' ({error}).");
        }

        Texture2D texture = ImageTexture.CreateFromImage(image);
        Cache[relativePath] = texture;
        return texture;
    }

    /// <summary>
    /// RitsuLib secondary-resource definitions accept paths rather than texture
    /// instances. Save the already decoded texture as a user resource so its
    /// normal ResourceLoader path can consume the custom image reliably.
    /// </summary>
    public static string PrepareResource(
        string relativePath,
        string userResourcePath,
        string fallbackPath,
        bool reuseExisting = false)
    {
        if (reuseExisting && ResourceLoader.Exists(userResourcePath))
        {
            return userResourcePath;
        }

        Texture2D? texture = Load(relativePath);
        if (texture == null)
        {
            return fallbackPath;
        }

        Error error = ResourceSaver.Save(texture, userResourcePath);
        if (error != Error.Ok)
        {
            MaidenSuccubusMod.Logger.Warn(
                $"Unable to prepare texture resource '{userResourcePath}' ({error}); "
                + $"using '{fallbackPath}'.");
            return fallbackPath;
        }

        return userResourcePath;
    }

    private static Texture2D? Fail(string relativePath, string message)
    {
        FailedLoads.Add(relativePath);
        MaidenSuccubusMod.Logger.Warn(message);
        return null;
    }
}
