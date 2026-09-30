using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MaidenSuccubus.UI;

/// <summary>Panel-owned native arrows: layout never depends on screen animation or hover state.</summary>
internal sealed class StarterRelicArrowLayout
{
    internal NButton Previous { get; }
    internal NButton Next { get; }
    private readonly Control _icon;
    private readonly Vector2 _previousExtent;
    private readonly Vector2 _nextExtent;

    internal StarterRelicArrowLayout(Control panel, Control icon, NButton previousSource, NButton nextSource)
    {
        _icon = icon;
        Previous = MakeArrow(previousSource, panel, false);
        Next = MakeArrow(nextSource, panel, true);
        _previousExtent = Previous.Size * Previous.Scale;
        _nextExtent = Next.Size * Next.Scale;
        panel.AddChild(Previous);
        panel.AddChild(Next);
        // The parent owns translation, scale and width changes through anchors.
        // Listen to the icon's local rectangle, including position-only changes.
        icon.ItemRectChanged += PlaceArrows;
        PlaceArrows();
    }

    private static NButton MakeArrow(NButton source, Control panel, bool right)
    {
        var button = (NButton)source.Duplicate((int)Node.DuplicateFlags.Scripts);
        NativeUiClone.RestoreOwners(button);
        button.Name = right ? "MaidenStarterRelicNext" : "MaidenStarterRelicPrevious";
        button.TooltipText = string.Empty;
        button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        button.CustomMinimumSize = Vector2.Zero;
        button.Size = source.Size;
        // Convert the ascension parent's scale to the relic parent's space once.
        button.Scale = (source.GetGlobalTransform().Scale / panel.GetGlobalTransform().Scale).Abs();
        button.AnchorLeft = button.AnchorRight = right ? 1f : 0f;
        IsolateMaterials(button);
        button.Visible = false;
        return button;
    }

    private static void IsolateMaterials(Node node)
    {
        if (node is CanvasItem canvas && canvas.Material != null)
            canvas.Material = (Material)canvas.Material.Duplicate(true);
        foreach (Node child in node.GetChildren()) IsolateMaterials(child);
    }

    private void PlaceArrows()
    {
        float centerY = _icon.Position.Y + _icon.Size.Y * _icon.Scale.Y / 2f;
        Place(Previous, -_previousExtent.X - 8f, centerY, _previousExtent);
        Place(Next, 8f, centerY, _nextExtent);
    }

    private static void Place(NButton button, float x, float centerY, Vector2 extent)
    {
        // Native hover changes only the TextureRect, never this fixed hitbox.
        // Account for a scaled control's pivot without sampling its animated rectangle.
        Vector2 pivotShift = button.PivotOffset * (Vector2.One - button.Scale);
        Vector2 offset = new Vector2(x, centerY - extent.Y / 2f) - pivotShift;
        Vector2 size = extent / button.Scale;
        button.OffsetLeft = offset.X;
        button.OffsetTop = offset.Y;
        button.OffsetRight = offset.X + size.X;
        button.OffsetBottom = offset.Y + size.Y;
    }
}
