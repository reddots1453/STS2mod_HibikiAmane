using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Cards;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.UI;

// Reuse the native playable highlight's texture, shader and width animation.
// The native SDF contains a filled centre: this node must stay behind the
// portrait/frame, just like native Highlight, so the card masks that centre.
internal partial class LibraryAuraOverlay : Control
{
    private const string NodeName = "MSLibraryAuraOverlay";
    private NCard? _card;
    private NCardHighlight? _native;
    private NCardHighlight? _red;
    private NCardHighlight? _green;
    private Control? _redClip;
    private Control? _greenClip;
    private LibraryAuraEffect _effect;
    private bool _ownsNativeVisibility;
    private Color _savedNativeSelfModulate;

    internal static void Attach(NCard card)
    {
        if (card.Model is not { IsMutable: true }
            || card.Model.Owner?.Character is not MaidenSuccubusCharacter
            || card.CardHighlight is not { Material: ShaderMaterial } native
            || native.GetParent() is not Control parent
            || parent.GetNodeOrNull<LibraryAuraOverlay>(NodeName) != null) return;

        var overlay = new LibraryAuraOverlay
        {
            Name = NodeName, _card = card, _native = native,
            MouseFilter = MouseFilterEnum.Ignore, ZIndex = native.ZIndex,
            ZAsRelative = native.ZAsRelative, Visible = false,
        };
        overlay._redClip = Clip("RedClip");
        overlay._greenClip = Clip("GreenClip");
        overlay._red = Highlight(native, "RedGlow", new Color(1f, .15f, .2f, .98f));
        overlay._green = Highlight(native, "GreenGlow", new Color(.2f, 1f, .4f, .98f));
        overlay.AddChild(overlay._redClip);
        overlay.AddChild(overlay._greenClip);
        overlay._redClip.AddChild(overlay._red);
        overlay._greenClip.AddChild(overlay._green);
        parent.AddChild(overlay);
        // card_ripple shades the SDF interior too. Its native placement behind
        // PortraitCanvasGroup and Frame is what turns that fill into edge glow.
        // Appending above the card instead would tint the entire portrait/text.
        parent.MoveChild(overlay, native.GetIndex() + 1);
    }

    private static Control Clip(string name) => new()
    {
        Name = name, MouseFilter = MouseFilterEnum.Ignore,
    };

    private static NCardHighlight Highlight(NCardHighlight native, string name, Color color)
    {
        // Construct a fresh native node rather than duplicating its live tween.
        // Each glow needs its own width uniform; sharing the material would also
        // hide/show the original highlight and unrelated pooled cards.
        var material = (ShaderMaterial)native.Material.Duplicate();
        material.ResourceLocalToScene = true;
        material.SetShaderParameter("width", 0f);
        return new NCardHighlight
        {
            Name = name, Texture = native.Texture, Material = material,
            ExpandMode = native.ExpandMode, StretchMode = native.StretchMode,
            TextureFilter = native.TextureFilter, TextureRepeat = native.TextureRepeat,
            FlipH = native.FlipH, FlipV = native.FlipV,
            Modulate = color, MouseFilter = MouseFilterEnum.Ignore,
        };
    }

    public override void _Process(double delta) => Safe.Run(() =>
    {
        if (!GodotObject.IsInstanceValid(_card) || !GodotObject.IsInstanceValid(_native)) return;
        LibraryAuraEffect next = _card!.DisplayingPile == PileType.Hand
            ? LibraryHandAura.InHand(_card.Model) : LibraryAuraEffect.None;
        if (next != LibraryAuraEffect.None)
        {
            SyncGeometry(next);
            SuppressNativeBlue();
        }
        else RestoreNative();

        if (next == _effect) return;
        _effect = next;
        Visible = next != LibraryAuraEffect.None;
        SetGlow(_red!, (next & LibraryAuraEffect.Exhaust) != 0);
        SetGlow(_green!, (next & LibraryAuraEffect.Replay) != 0);
        // Adjacency also changes displayed Exhaust/Replay text. Refresh once,
        // without restarting the native glow's 0.5-second tween every frame.
        _card.UpdateVisuals(_card.DisplayingPile, CardPreviewMode.Normal);
    }, "LibraryAura.NativeGlow");

    private static void SetGlow(NCardHighlight glow, bool show)
    {
        if (show) glow.AnimShow();
        else glow.AnimHideInstantly();
    }

    private void SyncGeometry(LibraryAuraEffect effect)
    {
        NCardHighlight native = _native!;
        Position = native.Position;
        Size = native.Size;
        Scale = native.Scale;
        Rotation = native.Rotation;
        PivotOffset = native.PivotOffset;
        _red!.Texture = native.Texture;
        _green!.Texture = native.Texture;
        _red.Size = native.Size;
        _green.Size = native.Size;

        bool both = (effect & (LibraryAuraEffect.Exhaust | LibraryAuraEffect.Replay))
            == (LibraryAuraEffect.Exhaust | LibraryAuraEffect.Replay);
        _redClip!.ClipContents = both;
        _greenClip!.ClipContents = both;
        if (both)
        {
            // Two copies of the same native contour, clipped at its horizontal
            // centre, give simultaneous red/green edges without additive yellow.
            // Generous outer margins preserve the shader's soft bloom.
            float w = native.Size.X, h = native.Size.Y;
            _redClip.Position = new Vector2(-w, -h);
            _redClip.Size = new Vector2(w * 1.5f, h * 3f);
            _red.Position = new Vector2(w, h);
            _greenClip.Position = new Vector2(w / 2f, -h);
            _greenClip.Size = new Vector2(w * 1.5f, h * 3f);
            _green.Position = new Vector2(-w / 2f, h);
        }
        else
        {
            _redClip.Position = _greenClip.Position = Vector2.Zero;
            _redClip.Size = _greenClip.Size = native.Size;
            _red.Position = _green.Position = Vector2.Zero;
        }
    }

    private void SuppressNativeBlue()
    {
        if (!_ownsNativeVisibility)
        {
            _savedNativeSelfModulate = _native!.SelfModulate;
            _ownsNativeVisibility = true;
        }
        // Keep the holder's normal color/tween logic running underneath. Hide
        // only this leaf while the corresponding native-tinted copies display.
        _native!.SelfModulate = new Color(_savedNativeSelfModulate, 0f);
    }

    private void RestoreNative()
    {
        if (!_ownsNativeVisibility) return;
        if (GodotObject.IsInstanceValid(_native))
            _native!.SelfModulate = _savedNativeSelfModulate;
        _ownsNativeVisibility = false;
    }

    public override void _ExitTree()
    {
        // Native NCard nodes are pooled and re-enter with a different model.
        RestoreNative();
        _effect = LibraryAuraEffect.None;
        Visible = false;
    }
}
