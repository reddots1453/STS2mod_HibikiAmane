using Godot;

namespace MaidenSuccubus.UI;

public static class BattleReplayCardVisuals
{
    public const string OverlayName = "MaidenSuccubusBattleReplayShadow";

    public static Control CreateShadowOverlay()
    {
        Control root = new()
        {
            Name = OverlayName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        Color outerShadow = new(0.025f, 0.035f, 0.065f, 0.64f);
        Color innerShadow = new(0.04f, 0.055f, 0.095f, 0.22f);
        AddEdgeBands(root, "Outer", 20f, outerShadow);
        AddEdgeBands(root, "Inner", 48f, innerShadow);
        return root;
    }

    private static void AddEdgeBands(
        Control root,
        string namePrefix,
        float thickness,
        Color color)
    {
        root.AddChild(CreateBand(
            $"{namePrefix}Top", color,
            0f, 0f, 1f, 0f,
            0f, 0f, 0f, thickness));
        root.AddChild(CreateBand(
            $"{namePrefix}Bottom", color,
            0f, 1f, 1f, 1f,
            0f, -thickness, 0f, 0f));
        root.AddChild(CreateBand(
            $"{namePrefix}Left", color,
            0f, 0f, 0f, 1f,
            0f, thickness, thickness, -thickness));
        root.AddChild(CreateBand(
            $"{namePrefix}Right", color,
            1f, 0f, 1f, 1f,
            -thickness, thickness, 0f, -thickness));
    }

    private static ColorRect CreateBand(
        string name,
        Color color,
        float anchorLeft,
        float anchorTop,
        float anchorRight,
        float anchorBottom,
        float offsetLeft,
        float offsetTop,
        float offsetRight,
        float offsetBottom) =>
        new()
        {
            Name = name,
            Color = color,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = anchorLeft,
            AnchorTop = anchorTop,
            AnchorRight = anchorRight,
            AnchorBottom = anchorBottom,
            OffsetLeft = offsetLeft,
            OffsetTop = offsetTop,
            OffsetRight = offsetRight,
            OffsetBottom = offsetBottom,
        };
}
