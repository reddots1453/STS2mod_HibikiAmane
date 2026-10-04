using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MaidenSuccubus.Acts;

namespace MaidenSuccubus.UI;

/// <summary>Event typography and art without registering a new event room.</summary>
internal sealed partial class TrialEventPage : Control
{
    private static readonly Vector2 DesignSize = new(1920, 1080);
    private Control _canvas = null!;
    private VBoxContainer _body = null!;
    private Label _title = null!;
    private MegaRichTextLabel _description = null!;
    private readonly List<TrialEventOptionButton> _options = [];
    private TextureRect _routePortrait = null!;
    private Tween? _fade;

    internal Control? DefaultFocusedControl => _options.FirstOrDefault(option => option.IsEnabled);

    internal static TrialEventPage Create()
    {
        var page = new TrialEventPage { Name = "TrialEventPage", MouseFilter = MouseFilterEnum.Ignore };
        page.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        page.Build();
        return page;
    }

    private void Build()
    {
        var baseColor = new ColorRect { Color = new Color(.018f, .024f, .043f), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(baseColor); baseColor.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        TrialBackgroundArt.AddBackdrop(this, "narrative.png", .14f);
        // A soft reading shade, rather than an opaque dialog or bordered panel.
        var shade = new TextureRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Texture = new GradientTexture2D
            {
                Width = 512, Height = 1, FillFrom = Vector2.Zero, FillTo = Vector2.Right,
                Gradient = new Gradient
                {
                    Offsets = new float[] { 0f, .43f, .65f, 1f },
                    Colors = new Color[] { new(.01f,.015f,.03f,0), new(.01f,.015f,.03f,.15f),
                        new(.01f,.015f,.03f,.78f), new(.01f,.015f,.03f,.88f) },
                },
            },
        };
        AddChild(shade); shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _canvas = new Control { Name = "EventCanvas", Size = DesignSize, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_canvas);
        _routePortrait = new TextureRect
        {
            Name = "RouteIllustration", Position = new Vector2(96, 310), Size = new Vector2(720, 468),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore, Modulate = new Color(1,1,1,0),
        };
        _canvas.AddChild(_routePortrait);
        // Match the native 800-wide event column. The story gets the remaining
        // height; the options always keep their own visible area below it.
        _body = new VBoxContainer
        {
            Name = "EventContent", Position = new Vector2(980, 150), Size = new Vector2(800, 820),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _body.AddThemeConstantOverride("separation", 16);
        _canvas.AddChild(_body);
        var template = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("events/default_event_layout"))
            .Instantiate<Control>();
        try
        {
            _title = template.GetNode<Label>("VBoxContainer/Title");
            _description = template.GetNode<MegaRichTextLabel>("VBoxContainer/EventDescription");
            _title.GetParent().RemoveChild(_title); _title.Owner = null;
            _description.GetParent().RemoveChild(_description); _description.Owner = null;
        }
        finally { template.Free(); }
        _title.Modulate = Colors.White;
        _title.MouseFilter = MouseFilterEnum.Ignore;
        _title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _body.AddChild(_title);
        var scroll = new ScrollContainer
        {
            Name = "EventStory", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 80),
        };
        _body.AddChild(scroll);
        _description.Modulate = Colors.White;
        _description.CustomMinimumSize = Vector2.Zero;
        _description.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _description.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        _description.FitContent = true; _description.ScrollActive = false;
        _description.AutoSizeEnabled = false;
        _description.MouseFilter = MouseFilterEnum.Pass;
        scroll.AddChild(_description);
    }

    internal void SetStory(string title, string text)
    {
        _fade?.Kill();
        _title.Text = title; _description.Text = text;
        _description.VisibleRatio = 1;
        if (IsInsideTree())
        {
            _description.Modulate = new Color(1, 1, 1, 0);
            _fade = CreateTween();
            _fade.TweenProperty(_description, "modulate:a", 1f, .3);
        }
        else _description.Modulate = Colors.White;
        _body.GetNode<ScrollContainer>("EventStory").ScrollVertical = 0;
    }

    internal void ClearOptions()
    {
        foreach (var option in _options)
        {
            option.SetEnabled(false);
            _body.RemoveChild(option); option.QueueFree();
        }
        _options.Clear();
        _routePortrait.Modulate = new Color(1,1,1,0);
    }

    internal TrialEventOptionButton AddOption(string name, string text, Action action,
        float height = 100, Color? accent = null, Texture2D? icon = null, params IHoverTip[] tips)
    {
        var option = TrialEventOptionButton.Create(name, text, height, accent, icon, tips);
        option.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => action()));
        _body.AddChild(option); _options.Add(option);
        return option;
    }

    internal TrialEventOptionButton AddQuest(FourthRouteQuest quest, Action action)
    {
        bool dark = FourthRouteProgressService.AlignmentOf(quest) == FourthRouteAlignment.Dark;
        string color = dark ? "purple" : "gold";
        string condition = FourthRouteProgressService.QuestText(quest, 1);
        var dormant = FourthRouteProgressService.CreateRelicPreview(quest, 0);
        var reward = FourthRouteProgressService.CreateRelicPreview(quest, 1);
        string text = $"[{color}][b]{FourthRouteOpeningScreen.TextFor(dark ? "dark" : "light")} · {FourthRouteProgressService.QuestName(quest)}[/b][/{color}]"
            + $"\n{FourthRouteOpeningScreen.TextFor(quest + ".flavor")}"
            + $"\n{FourthRouteOpeningScreen.TextFor("condition")}：{condition}"
            + $"\n{FourthRouteOpeningScreen.TextFor("reward")}{reward.Title.GetFormattedText()}";
        string corruption = dark ? "获得1点堕落值。" : "失去1点堕落值。";
        var option = AddOption("Accept" + quest, text, action, 174,
            dark ? new Color(.77f,.49f,.88f) : new Color(.92f,.79f,.45f), dormant.Icon,
            new HoverTip(dormant.Title, $"{FourthRouteOpeningScreen.TextFor("dormant")}\n{condition}", dormant.Icon),
            new HoverTip(reward.Title, reward.DynamicDescription.GetFormattedText() + "\n" + corruption, reward.Icon));
        option.Connect(NClickableControl.SignalName.Focused, Callable.From<NButton>(_ =>
        {
            _routePortrait.Texture = RuntimeTextureAssets.Load("ui/trial/" + (dark ? "sin.png" : "virtue.png"));
            _routePortrait.Modulate = new Color(1,1,1,.8f);
        }));
        return option;
    }

    internal void DisableOptions()
    {
        foreach (var option in _options) option.SetEnabled(false);
    }

    internal void LinkFocus()
    {
        if (!IsInsideTree()) return;
        for (int i = 0; i < _options.Count; i++)
        {
            var option = _options[i];
            option.FocusNeighborTop = _options[(i + _options.Count - 1) % _options.Count].GetPath();
            option.FocusNeighborBottom = _options[(i + 1) % _options.Count].GetPath();
            option.FocusNeighborLeft = option.FocusNeighborRight = option.GetPath();
        }
    }

    public override void _Ready()
    {
        Resized += Layout;
        Layout();
        LinkFocus();
    }

    private void Layout()
    {
        var size = Size;
        if (size.X < 1 || size.Y < 1) size = GetViewportRect().Size;
        float scale = Math.Min(size.X / DesignSize.X, size.Y / DesignSize.Y);
        _canvas.Scale = Vector2.One * scale;
        _canvas.Position = (size - DesignSize * scale) / 2;
    }

    public override void _ExitTree() => _fade?.Kill();
}
