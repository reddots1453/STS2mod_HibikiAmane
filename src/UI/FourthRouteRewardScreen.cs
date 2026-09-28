using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MaidenSuccubus.Acts;

namespace MaidenSuccubus.UI;

/// <summary>Native chest relic control without registering a shared chest or granting its preview.</summary>
public sealed partial class FourthRouteRewardScreen : Control, IScreenContext
{
    internal readonly record struct Pick(Vector2 Position, Vector2 Scale);
    private readonly FourthRouteRewardOffer _offer;
    private readonly Func<bool> _isCurrent;
    private readonly TaskCompletionSource<Pick?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private NTreasureRoomRelicHolder? _holder;
    private bool _closed;
    public Control? DefaultFocusedControl => _holder;

    private FourthRouteRewardScreen(FourthRouteRewardOffer offer, Func<bool> isCurrent)
    {
        _offer = offer; _isCurrent = isCurrent;
        Name = "FourthRouteTrialReward";
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    internal static async Task<Pick?> Show(FourthRouteRewardOffer offer, Func<bool> isCurrent)
    {
        if (!offer.IsValid || !isCurrent()) return null;
        var container = NModalContainer.Instance ?? throw new InvalidOperationException("Trial reward requires modal container.");
        if (container.OpenModal != null) throw new InvalidOperationException("Trial reward cannot replace an existing modal.");
        var screen = new FourthRouteRewardScreen(offer, isCurrent);
        container.Add(screen);
        return await screen._completion.Task;
    }

    public override void _Ready()
    {
        try { Build(); }
        catch (Exception ex) { _completion.TrySetException(ex); Close(null); }
    }

    private void Build()
    {
        NHotkeyManager.Instance?.AddBlockingScreen(this);
        var shade = new ColorRect { Color = new Color(.015f, .02f, .035f, .94f), MouseFilter = MouseFilterEnum.Stop };
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(shade);
        var body = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        body.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        body.AnchorLeft = .15f; body.AnchorRight = .85f; body.AnchorTop = .08f; body.AnchorBottom = .92f;
        body.AddThemeConstantOverride("separation", 20);
        AddChild(body);
        body.AddChild(Heading(Text("title"), 42));
        body.AddChild(Heading(FourthRouteProgressService.QuestText(_offer.Quest, _offer.Trial), 24));

        // Current game's standalone chest control. Never instantiate the shared
        // collection, whose _Ready subscribes to the real chest synchronizer.
        _holder = ResourceLoader.Load<PackedScene>("res://scenes/ui/treasure_relic_holder.tscn")
            .Instantiate<NTreasureRoomRelicHolder>();
        var relic = FourthRouteProgressService.CreateRelicPreview(_offer.Quest, _offer.Stage);
        var iconArea = new Control { CustomMinimumSize = new Vector2(0, 160) };
        body.AddChild(iconArea);
        iconArea.AddChild(_holder);
        _holder.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        _holder.Show();
        _holder.Relic.Model = relic;
        _holder.Relic.Modulate = Colors.White;
        _holder.VoteContainer.Hide();
        _holder.Enable();
        _holder.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => Select()));
        body.AddChild(Heading(relic.Title.GetFormattedText(), 32));
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 80) };
        var description = new MegaRichTextLabel { BbcodeEnabled = true, AutoSizeEnabled = false,
            MinFontSize = 24, MaxFontSize = 24, FitContent = true, ScrollActive = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Pass };
        description.AddThemeFontSizeOverride("normal_font_size", 24);
        foreach (string key in new[] { "normal_font", "bold_font", "italics_font", "bold_italics_font", "mono_font" })
            description.AddThemeFontOverride(key, description.GetThemeDefaultFont());
        description.Text = relic.DynamicDescription.GetFormattedText(); // No Owner: no future progress hint.
        scroll.AddChild(description);
        body.AddChild(scroll);
        body.AddChild(Heading(Text("claim"), 22));
        Callable.From(() => { if (!_closed && IsInsideTree()) _holder?.GrabFocus(); }).CallDeferred();
    }

    private static string Text(string key) => new LocString("events", "MAIDEN_SUCCUBUS_TRIAL_REWARD." + key).GetFormattedText();
    private static Label Heading(string text, int size) => new Label { Text = text,
        HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart,
        MouseFilter = MouseFilterEnum.Ignore }.WithFontSize(size, new Color(.96f, .91f, .77f));

    private void Select()
    {
        if (_closed || !_isCurrent() || _holder is null) return;
        _holder.Disable();
        Close(new Pick(_holder.GlobalPosition, _holder.Scale));
    }
    private void Close(Pick? result)
    {
        if (_closed) return;
        _closed = true;
        Hide();
        MouseFilter = MouseFilterEnum.Ignore;
        NHotkeyManager.Instance?.RemoveBlockingScreen(this);
        _completion.TrySetResult(result);
        if (NModalContainer.Instance is { } container && ReferenceEquals(container.OpenModal, this)) container.Clear();
        else QueueFree();
    }
    public override void _Process(double delta) { if (!_isCurrent()) Close(null); }
    public override void _UnhandledInput(InputEvent input)
    {
        if (input.IsActionPressed("ui_cancel")) GetViewport().SetInputAsHandled();
    }
    public override void _ExitTree()
    {
        _closed = true;
        if (_holder != null && GodotObject.IsInstanceValid(_holder)) NHoverTipSet.Remove(_holder);
        NHotkeyManager.Instance?.RemoveBlockingScreen(this);
        _completion.TrySetResult(null);
    }
}
