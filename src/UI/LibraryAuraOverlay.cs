using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Cards;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.UI;

// Child coordinates inherit the card's rotation/scale; native highlight stays untouched.
internal partial class LibraryAuraOverlay : Control
{
    private const string NodeName = "MSLibraryAuraOverlay";
    private NCard? _card;
    private LibraryAuraEffect _effect;
    private readonly StyleBoxFlat[] _red = Frames(new Color(1f, .15f, .2f));
    private readonly StyleBoxFlat[] _green = Frames(new Color(.2f, 1f, .4f));

    internal static void Attach(NCard card)
    {
        // Compendium and hover-tip cards may use canonical read-only models.
        // Owner is a mutable-only property, so do not access it for those previews.
        if (card.Model is not { IsMutable: true }) return;
        if (card.GetNodeOrNull<Control>("CardContainer") is not { } container
            || container.GetNodeOrNull<LibraryAuraOverlay>(NodeName) != null
            || card.Model?.Owner?.Character is not MaidenSuccubusCharacter) return;
        container.AddChild(new LibraryAuraOverlay { Name = NodeName, _card = card,
            MouseFilter = MouseFilterEnum.Ignore, ZIndex = 1, Visible = false });
    }

    // A StyleBox shadow can paint the whole rounded rectangle even with a
    // transparent background. Build the glow from hollow contour strokes only.
    // Cache the styles once; no shader, textures or per-frame allocations.
    private static StyleBoxFlat[] Frames(Color color) =>
    [
        Frame(new Color(color, .08f), 18),
        Frame(new Color(color, .16f), 16),
        Frame(new Color(color, .30f), 14),
        Frame(new Color(color, .95f), 12),
    ];

    private static StyleBoxFlat Frame(Color color, int radius) => new()
    {
        DrawCenter = false, BgColor = Colors.Transparent, BorderColor = color,
        BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
        CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
        CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
        ShadowColor = Colors.Transparent, ShadowSize = 0,
    };

    private void DrawEdge(StyleBoxFlat[] frames, Rect2 rect)
    {
        for (int i = 0; i < frames.Length; i++)
            DrawStyleBox(frames[i], rect.Grow((frames.Length - 1 - i) * 2));
    }

    public override void _Process(double delta) => Safe.Run(() =>
    {
        if (_card?.Model is not { } model) { Visible = false; return; }
        var next = _card.DisplayingPile == PileType.Hand ? LibraryHandAura.InHand(model) : LibraryAuraEffect.None;
        Visible = next != LibraryAuraEffect.None;
        if (next == _effect) return;
        _effect = next;
        QueueRedraw();
        // Refresh exactly when adjacency changes, not every frame/compendium scroll.
        _card.UpdateVisuals(_card.DisplayingPile, CardPreviewMode.Normal);
    }, "LibraryAura.Overlay");

    public override void _Draw()
    {
        var rect = new Rect2(-NCard.defaultSize / 2, NCard.defaultSize);
        if ((_effect & LibraryAuraEffect.Exhaust) != 0) DrawEdge(_red, rect.Grow(-3));
        if ((_effect & LibraryAuraEffect.Replay) != 0) DrawEdge(_green,
            rect.Grow((_effect & LibraryAuraEffect.Exhaust) != 0 ? -12 : -3));
    }

    // NCard is pooled: exiting the tree is not permanent destruction.
    public override void _ExitTree() { _effect = LibraryAuraEffect.None; Visible = false; }
}
