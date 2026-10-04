using Godot;

namespace MaidenSuccubus.UI;

/// <summary>Shion-style diagonal rail and staggered information separators; no modal box.</summary>
internal partial class StartRouteRailArt : Control
{
    internal Color AccentColor { get; set; }

    public override void _Draw()
    {
        DrawColoredPolygon([new(514, 0), new(610, 0), new(296, 720), new(190, 720)],
            new Color(.02f, .025f, .04f, .65f));
        DrawColoredPolygon([new(577, 0), new(595, 0), new(272, 720), new(254, 720)],
            new Color(.3f, .4f, .55f, .16f));
        for (int i = 0; i < 4; i++)
        {
            float x = 34 + i * 20, y = 66 + i * 58;
            DrawColoredPolygon([new(x - 10, y - 10), new(598, y - 10), new(580, y + 34), new(x - 23, y + 34)],
                new Color(.025f, .04f, .065f, .72f));
            DrawRect(new Rect2(x, y, 20, 20), new Color(AccentColor, .8f));
            DrawRect(new Rect2(x, y, 20, 20), new Color(.87f, .91f, .95f, .8f), false, 1);
            DrawLine(new Vector2(x + 29, y + 31), new Vector2(592, y + 31),
                new Color(.68f, .77f, .84f, .4f), 1, true);
        }
    }
}
