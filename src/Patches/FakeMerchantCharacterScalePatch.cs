using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>The fake merchant uses the same portrait and display scale as the normal shop.</summary>
[HarmonyPatch(typeof(NFakeMerchant), "StartCharacterAnimation")]
internal static class FakeMerchantCharacterScalePatch
{
    private const string ShopScenePath = "res://MaidenSuccubus/scenes/maiden_succubus_merchant.tscn";
    private const string PortraitName = "MaidenNormalShopPortrait";

    [HarmonyPrefix]
    private static bool Prefix(NCreatureVisuals visuals)
    {
        if (visuals is not MaidenSuccubusCreatureVisuals) return true;
        bool matched = false;
        Safe.Run(() => matched = MatchShop(visuals), nameof(FakeMerchantCharacterScalePatch));
        // The normal shop portrait is a Sprite2D, so it has no Spine idle to start.
        return !matched;
    }

    private static bool MatchShop(NCreatureVisuals visuals)
    {
        if (visuals.GetParent() is not Control container) return false;
        var scene = ResourceLoader.Load<PackedScene>(ShopScenePath);
        if (scene == null) return false;
        var template = scene.Instantiate<Node2D>();
        try
        {
            var source = template.GetNodeOrNull<Sprite2D>("Visuals");
            var combatArt = visuals.GetNodeOrNull<Node2D>("Visuals");
            if (source?.Texture == null || combatArt == null) return false;
            Vector2 normalContainerScale = NormalShopContainerScale();
            if (Mathf.IsZeroApprox(container.Scale.X) || Mathf.IsZeroApprox(container.Scale.Y)) return false;
            // Absolute shop size, independent of a combat root scale or the
            // obsolete half-size patch; applying it twice yields the same result.
            Vector2 targetScale = template.Scale * normalContainerScale / container.Scale;

            // Use the actual shop node: texture, offset, position, flip and local
            // scale all follow its scene. Do not estimate size from PNG dimensions.
            if (visuals.GetNodeOrNull<Sprite2D>(PortraitName) == null)
            {
                template.RemoveChild(source); source.Owner = null;
                source.Name = PortraitName;
                visuals.AddChild(source);
            }
            combatArt.Visible = false;
            visuals.Scale = targetScale;
            return true;
        }
        finally { template.Free(); }
    }

    private static Vector2 NormalShopContainerScale()
    {
        // Read the native scene's container scale without instantiating the shop,
        // its merchant/background scripts, or opening another room.
        var state = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("rooms/merchant_room")).GetState();
        for (int i = 0; i < state.GetNodeCount(); i++)
        {
            if (!state.GetNodePath(i).ToString().EndsWith("SceneContainer/CharacterContainer", StringComparison.Ordinal)) continue;
            for (int j = 0; j < state.GetNodePropertyCount(i); j++)
                if (state.GetNodePropertyName(i, j) == "scale")
                    return state.GetNodePropertyValue(i, j).AsVector2();
            return Vector2.One;
        }
        throw new InvalidOperationException("Normal merchant scene has no character container.");
    }
}
