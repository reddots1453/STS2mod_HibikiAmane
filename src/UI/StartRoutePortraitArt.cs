using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MaidenSuccubus.UI;

/// <summary>Godot counterpart of Shion's clipped portrait, glow and lock-cover layers.</summary>
internal partial class StartRoutePortraitArt : NButton
{
    internal static readonly Vector2 PortraitSize = new(260, 154);
    internal Texture2D? Portrait { get; set; }
    private Color _accent;
    private bool _selected, _focused, _locked;

    internal void Paint(Color accent, bool selected, bool focused, bool locked)
    {
        _accent = accent; _selected = selected; _focused = focused; _locked = locked;
        QueueRedraw();
    }

    // The transparent slanted corners must not capture input over another UI.
    public override bool _HasPoint(Vector2 point) => point.Y >= 0 && point.Y <= 154
        && point.X >= 48 * (1 - point.Y / 154) && point.X <= 260 - 48 * point.Y / 154;

    public override void _Draw()
    {
        Vector2[] outline = [new(48, 0), new(260, 0), new(212, 154), new(0, 154), new(48, 0)];
        Vector2[] polygon = outline[..4];
        if (_selected || _focused)
        {
            DrawPolyline(outline, new Color(_accent, .09f), 18, true);
            DrawPolyline(outline, new Color(_accent, .19f), 9, true);
        }
        DrawColoredPolygon(polygon, new Color(.035f, .045f, .07f, .98f));
        if (Portrait is { } texture)
        {
            // Crop to a horizontal head portrait without stretching the face.
            float height = 154f / 260 * texture.GetWidth() / texture.GetHeight();
            Vector2[] uv = polygon.Select(p => new Vector2(p.X / 260, .10f + p.Y / 154 * height)).ToArray();
            DrawPolygon(polygon, new Color[] { Colors.White }, uv, texture);
        }
        // Name/value strip follows the same cut as the portrait itself.
        DrawColoredPolygon([new(11, 118), new(223, 118), new(212, 154), new(0, 154)],
            new Color(.025f, .035f, .055f, .90f));
        if (_locked)
        {
            DrawColoredPolygon(polygon, new Color(.025f, .035f, .055f, .55f));
            var white = new Color(.92f, .95f, 1f, .96f);
            DrawArc(new Vector2(130, 67), 17, Mathf.Pi, Mathf.Tau, 24, white, 3, true);
            var body = new Rect2(107, 65, 46, 38);
            DrawRect(body, new Color(.04f, .06f, .09f, .65f));
            DrawRect(body, white, false, 2);
            DrawCircle(new Vector2(130, 80), 4, white);
            DrawLine(new Vector2(130, 83), new Vector2(130, 92), white, 3, true);
        }
        DrawPolyline(outline, _selected ? _accent : new Color(.75f, .82f, .9f, _focused ? 1 : .66f),
            _selected ? 3 : 2, true);
        if (!_selected)
            DrawLine(new Vector2(56, 7), new Vector2(248, 7), new Color(_accent, .65f), 2, true);
    }
}
