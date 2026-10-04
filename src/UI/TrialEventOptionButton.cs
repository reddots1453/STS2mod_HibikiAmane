using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace MaidenSuccubus.UI;

/// <summary>Native event visual children, with input owned by the trial modal.</summary>
internal sealed partial class TrialEventOptionButton : NButton
{
    private NinePatchRect _image = null!, _outline = null!;
    private ShaderMaterial? _hsv;
    private Tween? _animation;
    private IHoverTip[] _tips = [];
    private Color _accent = StsColors.blueGlow;

    internal static TrialEventOptionButton Create(string name, string text,
        float height = 174, Color? accent = null, Texture2D? icon = null, params IHoverTip[] tips)
    {
        var button = new TrialEventOptionButton
        {
            Name = name, Size = new Vector2(800, height),
            CustomMinimumSize = new Vector2(800, height),
            FocusMode = FocusModeEnum.All, MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            _tips = tips, _accent = accent ?? StsColors.blueGlow,
        };
        // Never add the template itself to the tree: its script requires a live
        // EventModel and calls NEventRoom. Only its inert visual children are reused.
        var template = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("events/event_option_button"))
            .Instantiate<Control>();
        try
        {
            foreach (string child in new[] { "Shadow", "Outline", "Image", "Text" })
            {
                var node = template.GetNode<Control>(child);
                template.RemoveChild(node); node.Owner = null;
                node.MouseFilter = MouseFilterEnum.Ignore;
                node.FocusMode = FocusModeEnum.None;
                button.AddChild(node);
            }
            button._image = button.GetNode<NinePatchRect>("Image");
            button._outline = button.GetNode<NinePatchRect>("Outline");
            foreach (var art in new[] { button._image, button._outline, button.GetNode<NinePatchRect>("Shadow") })
            {
                art.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                art.PivotOffset = new Vector2(400, height / 2);
            }
            if (button._image.Material is ShaderMaterial material)
            {
                button._hsv = (ShaderMaterial)material.Duplicate();
                button._image.Material = button._hsv;
            }
            var label = button.GetNode<MegaRichTextLabel>("Text");
            label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            label.OffsetLeft = 42; label.OffsetRight = icon == null ? -36 : -118;
            label.OffsetTop = 13; label.OffsetBottom = -13;
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.MinFontSize = 20; label.MaxFontSize = 24;
            label.Text = text;
            if (icon != null)
            {
                var relic = new TextureRect
                {
                    Name = "TrialRelicIcon", Texture = icon,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                button.AddChild(relic);
                relic.AnchorLeft = relic.AnchorRight = 1;
                relic.AnchorTop = relic.AnchorBottom = .5f;
                relic.OffsetLeft = -102; relic.OffsetRight = -42;
                relic.OffsetTop = -30; relic.OffsetBottom = 30;
            }
        }
        finally { template.Free(); }
        return button;
    }

    public override void _Ready()
    {
        ConnectSignals();
        Resized += () => PivotOffset = Size / 2;
        PivotOffset = Size / 2;
    }

    protected override void OnFocus()
    {
        base.OnFocus();
        _animation?.Kill();
        _animation = CreateTween().SetParallel();
        _animation.TweenProperty(this, "scale", Vector2.One * 1.01f, .05);
        _animation.TweenProperty(_outline, "modulate", _accent, .05);
        _hsv?.SetShaderParameter("v", 1.2f);
        NHoverTipSet.Remove(this);
        if (_tips.Length > 0) NHoverTipSet.CreateAndShow(this, _tips, HoverTipAlignment.Left);
    }

    protected override void OnPress()
    {
        base.OnPress();
        _animation?.Kill();
        _animation = CreateTween();
        _animation.TweenProperty(this, "scale", Vector2.One * .99f, .12);
        _hsv?.SetShaderParameter("v", .9f);
    }

    protected override void OnUnfocus()
    {
        base.OnUnfocus();
        ResetVisuals();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        NHoverTipSet.Remove(this);
        _animation?.Kill();
        _animation = CreateTween().SetParallel();
        _animation.TweenProperty(this, "scale", Vector2.One, .2);
        _animation.TweenProperty(_outline, "modulate:a", 0f, .2);
        _hsv?.SetShaderParameter("v", .9f);
    }

    public override void _ExitTree()
    {
        _animation?.Kill();
        NHoverTipSet.Remove(this);
        base._ExitTree();
    }
}
