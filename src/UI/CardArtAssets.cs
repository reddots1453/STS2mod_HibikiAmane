namespace MaidenSuccubus.UI;

/// <summary>
/// Resolves reviewed card art by the card's concrete C# class name. Cards that
/// do not yet have dedicated artwork share the reviewed default illustration.
/// The loose PNG is decoded once and persisted as a user Texture2D resource so
/// the game's ordinary CardAssetProfile loader can consume it.
/// </summary>
public static class CardArtAssets
{
    private const string VanillaFallback =
        "res://images/packed/card_portraits/ironclad/bash.png";
    private static readonly Dictionary<Type, string> Paths = [];

    public static string GetPortraitPath(Type cardType)
    {
        if (Paths.TryGetValue(cardType, out string? cached))
        {
            return cached;
        }

        string dedicated = $"cards/{cardType.Name}.png";
        string relativePath = RuntimeTextureAssets.Exists(dedicated)
            ? dedicated
            : "cards/default.png";
        string path = RuntimeTextureAssets.PrepareResource(
            relativePath,
            $"user://maiden_succubus_card_art_{cardType.Name}.tres",
            VanillaFallback);
        Paths[cardType] = path;
        return path;
    }
}
