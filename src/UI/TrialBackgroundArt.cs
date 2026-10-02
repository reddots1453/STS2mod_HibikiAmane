using Godot;

namespace MaidenSuccubus.UI;

/// <summary>Decorative trial artwork; neither layout minimums nor input ownership come from an image.</summary>
internal static class TrialBackgroundArt
{
    internal static Control AddBackdrop(Control parent, string image, float dim = .25f)
    {
        var layer = new Control
        {
            Name = "TrialBackground_" + image.Replace(".png", string.Empty),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None,
            ClipContents = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        parent.AddChild(layer);
        layer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var picture = new TextureRect
        {
            Name = "Artwork",
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None,
        };
        layer.AddChild(picture);
        picture.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        // Ignore the PNG's minimum size before assigning its HD texture. The
        // existing containers, scroll areas and viewport determine geometry.
        picture.Texture = RuntimeTextureAssets.Load("ui/trial/" + image);
        var veil = new ColorRect
        {
            Name = "ReadingShade",
            Color = new Color(.01f, .02f, .05f, dim),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None,
        };
        layer.AddChild(veil);
        veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return layer;
    }

    internal static PanelContainer Banner(VBoxContainer content)
    {
        var panel = new PanelContainer
        {
            Name = "TrialOpeningBanner",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = .9f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        AddBackdrop(panel, "title.png");
        panel.AddChild(content);
        return panel;
    }
}
