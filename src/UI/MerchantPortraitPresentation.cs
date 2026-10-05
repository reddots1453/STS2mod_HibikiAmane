using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;

namespace MaidenSuccubus.UI;

/// <summary>One portrait definition for the shop and both fake-merchant creation paths.</summary>
internal static class MerchantPortraitPresentation
{
    private const string ImagePath = "character/hibiki_amane_merchant.png";
    private const string LegacyPortraitName = "MaidenNormalShopPortrait";
    // Existing normal-shop Sprite2D settings; the foot anchor is shared by both rooms.
    private static readonly Vector2 SpriteScale = new(0.35f, 0.35f);
    private static readonly Vector2 SpritePosition = new(0f, 13.65f);

    internal static MaidenSuccubusMerchantCharacter? TryCreate()
    {
        Texture2D? texture = RuntimeTextureAssets.Load(ImagePath);
        if (texture == null)
        {
            MaidenSuccubusMod.Logger.Warn("[MerchantPortrait] Mod PNG unavailable; retaining the framework fallback.");
            return null;
        }
        var root = new MaidenSuccubusMerchantCharacter { Name = "MaidenSuccubusMerchant" };
        root.AddChild(CreateSprite(texture, "Visuals"));
        MaidenSuccubusMod.Logger.Info($"[MerchantPortrait] Loaded directly from mod PNG: {texture.GetWidth()}x{texture.GetHeight()}; scale={SpriteScale}");
        return root;
    }

    private static Sprite2D CreateSprite(Texture2D texture, string name) => new()
    {
        Name = name, Texture = texture, Centered = false,
        Offset = new Vector2(-texture.GetWidth() * 0.5f, -texture.GetHeight()),
        Position = SpritePosition, Scale = SpriteScale,
    };

    internal static bool MatchLegacyFake(NCreatureVisuals visuals)
    {
        if (visuals is not MaidenSuccubusCreatureVisuals
            || visuals.GetParent() is not Control container) return false;
        var combatArt = visuals.GetNodeOrNull<Node2D>("Visuals");
        if (combatArt == null) return false;
        NFakeMerchant? screen = FindFakeScreen(container);
        if (screen == null) return false;
        Texture2D? texture = RuntimeTextureAssets.Load(ImagePath);
        if (texture == null) return false;
        if (visuals.GetNodeOrNull<Sprite2D>(LegacyPortraitName) == null)
            visuals.AddChild(CreateSprite(texture, LegacyPortraitName));
        MatchFakeScale(screen, visuals);
        combatArt.Visible = false;
        return true;
    }

    internal static void MatchFakeScale(NFakeMerchant screen, Node2D portrait)
    {
        if (portrait.GetParent() is not CanvasItem parent) return;
        // Include the complete ancestor transform relative to the event root, not
        // just CharacterContainer.Scale. Window/viewport scaling applies equally to both rooms.
        Transform2D relative = screen.GetGlobalTransformWithCanvas().AffineInverse()
            * parent.GetGlobalTransformWithCanvas();
        Vector2 parentScale = new(relative.X.Length(), relative.Y.Length());
        if (Mathf.IsZeroApprox(parentScale.X) || Mathf.IsZeroApprox(parentScale.Y)) return;
        Vector2 normalScale = NormalShopParentScale();
        portrait.Scale = normalScale / parentScale;
        MaidenSuccubusMod.Logger.Info($"[MerchantPortrait] Fake merchant matched: parentScale={parentScale}; rootScale={portrait.Scale}; shopParentScale={normalScale}");
    }

    private static NFakeMerchant? FindFakeScreen(Node node)
    {
        for (Node? parent = node; parent != null; parent = parent.GetParent())
            if (parent is NFakeMerchant screen) return screen;
        return null;
    }

    private static Vector2 NormalShopParentScale()
    {
        var state = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("rooms/merchant_room")).GetState();
        string? containerPath = null;
        for (int i = 0; i < state.GetNodeCount(); i++)
        {
            string path = state.GetNodePath(i).ToString();
            if (path.EndsWith("SceneContainer/CharacterContainer", StringComparison.Ordinal))
            {
                containerPath = path;
                break;
            }
        }
        if (containerPath == null) throw new InvalidOperationException("Normal merchant scene has no character container.");
        Vector2 scale = Vector2.One;
        for (int i = 0; i < state.GetNodeCount(); i++)
        {
            string path = state.GetNodePath(i).ToString();
            if (path != "." && path != containerPath
                && !containerPath.StartsWith(path + "/", StringComparison.Ordinal)) continue;
            for (int j = 0; j < state.GetNodePropertyCount(i); j++)
                if (state.GetNodePropertyName(i, j) == "scale")
                    scale *= state.GetNodePropertyValue(i, j).AsVector2();
        }
        return scale;
    }
}
