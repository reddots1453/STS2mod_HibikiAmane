using Godot;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Routes;
using MegaCrit.Sts2.Core.Models;

namespace MaidenSuccubus.UI;

/// <summary>
/// Resolves reviewed card art by the card's concrete C# class name. Cards that
/// do not yet have dedicated artwork share the reviewed default illustration.
///
/// CardAssetProfile exposes the real imported CompressedTexture2D path, like
/// native cards. Third-party previews and the scoped variant patches share the
/// same texture; no placeholder art or user:// serialization is needed.
/// </summary>
public static class CardArtAssets
{
    private const string CardIdPrefix = "MAIDEN_SUCCUBUS_CARD_";
    private const string VanillaFallback =
        "res://images/packed/card_portraits/ironclad/bash.png";
    private static readonly Dictionary<Type, string> RelativePaths = [];

    public static string GetPortraitPath(Type cardType)
    {
        string relative = ResolveRelativePath(cardType);
        return RuntimeTextureAssets.IsImported(relative)
            ? RuntimeTextureAssets.GetResourcePath(relative) : VanillaFallback;
    }

    public static bool IsMaidenCard(CardModel model) =>
        model.GetType().Assembly == typeof(CardArtAssets).Assembly
        || model.Id.Entry.StartsWith(CardIdPrefix, StringComparison.Ordinal);

    public static Texture2D? GetPortraitTexture(CardModel card)
    {
        // RouteKind already implements the live corruption thresholds and
        // defaults to the base art for cards without an attached run state.
        string? variant = card switch
        {
            Transform transform when transform.RouteKind == RouteCardKind.Corrupt
                => "cards/Transform_Corrupt.png",
            DarkElementBase dark when dark.RouteKind == RouteCardKind.Holy
                => $"cards/{card.GetType().Name}_Holy.png",
            _ => null,
        };
        if (variant != null && RuntimeTextureAssets.Exists(variant))
        {
            return RuntimeTextureAssets.Load(variant);
        }
        return GetPortraitTexture(card.GetType());
    }

    public static Texture2D? GetPortraitTexture(Type cardType) =>
        RuntimeTextureAssets.Load(ResolveRelativePath(cardType));

    private static string ResolveRelativePath(Type cardType)
    {
        if (!RelativePaths.TryGetValue(cardType, out string? relativePath))
        {
            string dedicated = $"cards/{cardType.Name}.png";
            relativePath = RuntimeTextureAssets.Exists(dedicated)
                ? dedicated
                : "cards/default.png";
            RelativePaths[cardType] = relativePath;
        }
        return relativePath;
    }
}
