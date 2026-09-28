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
    private readonly StyleBoxFlat _red = Frame(new Color(1f, .15f, .2f));
    private readonly StyleBoxFlat _green = Frame(new Color(.2f, 1f, .4f));

    internal static void Attach(NCard card)
    {
        if (card.GetNodeOrNull<Control>("CardContainer") is not { } container
            || container.GetNodeOrNull<LibraryAuraOverlay>(NodeName) != null
            || card.Model?.Owner?.Character is not MaidenSuccubusCharacter) return;
        container.AddChild(new LibraryAuraOverlay { Name = NodeName, _card = card,
            MouseFilter = MouseFilterEnum.Ignore, ZIndex = 1, Visible = false });
    }

    private static StyleBoxFlat Frame(Color color) => new()
    {
        BgColor = Colors.Transparent, BorderColor = color, BorderWidthLeft = 2, BorderWidthRight = 2,
        BorderWidthTop = 2, BorderWidthBottom = 2, CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
        CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12, ShadowColor = new Color(color, .5f), ShadowSize = 7,
    };

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
        if ((_effect & LibraryAuraEffect.Exhaust) != 0) DrawStyleBox(_red, rect.Grow(-3));
        if ((_effect & LibraryAuraEffect.Replay) != 0) DrawStyleBox(_green,
            rect.Grow((_effect & LibraryAuraEffect.Exhaust) != 0 ? -8 : -3));
    }

    // NCard is pooled: exiting the tree is not permanent destruction.
    public override void _ExitTree() { _effect = LibraryAuraEffect.None; Visible = false; }
}
