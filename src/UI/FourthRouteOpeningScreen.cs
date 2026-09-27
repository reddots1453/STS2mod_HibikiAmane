using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;

namespace MaidenSuccubus.UI;

/// <summary>Opaque, non-cancellable choice → narrative flow. Scene exit cancels the UI, not the saved choice.</summary>
public sealed partial class FourthRouteOpeningScreen : Control, IScreenContext
{
    private readonly Player _player;
    private readonly RunState _run;
    private readonly Func<bool> _isCurrent;
    private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<Button> _buttons = [];
    private VBoxContainer _body = null!;
    private bool _busy;
    private bool _closed;
    public Control? DefaultFocusedControl => _buttons.FirstOrDefault(button => !button.Disabled);

    internal static string TextFor(string key) => new LocString("events", "MAIDEN_SUCCUBUS_ROUTE_OPENING." + key).GetFormattedText();

    private FourthRouteOpeningScreen(Player player, RunState run, Func<bool> isCurrent)
    {
        _player = player; _run = run; _isCurrent = isCurrent;
        Name = "FourthRouteOpening";
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    internal static async Task<bool> Show(Player player, RunState run, Func<bool> isCurrent)
    {
        if (!isCurrent()) return false;
        NModalContainer container = NModalContainer.Instance ?? throw new InvalidOperationException("Opening requires modal container.");
        if (container.OpenModal != null) throw new InvalidOperationException("Opening cannot replace another modal.");
        var screen = new FourthRouteOpeningScreen(player, run, isCurrent);
        container.Add(screen);
        return await screen._completion.Task;
    }

    public override void _Ready()
    {
        try { BuildPage(); }
        catch (Exception ex)
        {
            _completion.TrySetException(ex);
            Close(false);
        }
    }

    private void BuildPage()
    {
        NHotkeyManager.Instance?.AddBlockingScreen(this);
        var background = new ColorRect { Color = new Color(0.018f, 0.024f, 0.043f, 1f), MouseFilter = MouseFilterEnum.Stop };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);
        var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AnchorLeft = .06f; margin.AnchorRight = .94f;
        margin.AnchorTop = .04f; margin.AnchorBottom = .96f;
        AddChild(margin);
        _body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _body.AddThemeConstantOverride("separation", 20);
        margin.AddChild(_body);
        var opening = FourthRouteOpeningService.Prepare(_run);
        if (opening.Chosen is { } chosen)
            TaskHelper.RunSafely(Confirm(chosen)); // Resume an interrupted, already locked narrative.
        else ShowChoices(opening);
    }

    private void ClearPage()
    {
        _buttons.Clear();
        foreach (Node child in _body.GetChildren()) { _body.RemoveChild(child); child.QueueFree(); }
    }

    private void ShowChoices(FourthRouteOpeningState opening)
    {
        ClearPage();
        _body.AddChild(Heading(TextFor("title"), 38));
        var story = Scroll(TextFor("common"));
        story.SizeFlagsStretchRatio = .9f;
        _body.AddChild(story);
        var columns = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.35f };
        columns.AddThemeConstantOverride("separation", 32);
        columns.AddChild(Choice(opening.Dark!.Value, true));
        columns.AddChild(Choice(opening.Light!.Value, false));
        _body.AddChild(columns);
        FocusFirst();
    }

    private Control Choice(FourthRouteQuest quest, bool dark)
    {
        Color accent = dark ? new Color(.77f, .49f, .88f) : new Color(.92f, .79f, .45f);
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        var style = new StyleBoxFlat { BgColor = new Color(.055f, .065f, .095f), BorderColor = accent.Darkened(.3f),
            ContentMarginLeft = 24, ContentMarginRight = 24, ContentMarginTop = 20, ContentMarginBottom = 20 };
        style.SetBorderWidthAll(2); style.SetCornerRadiusAll(16);
        panel.AddThemeStyleboxOverride("panel", style);
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", 12);
        panel.AddChild(column);
        var alignment = Heading(TextFor(dark ? "dark" : "light"), 22);
        alignment.AddThemeColorOverride("font_color", accent);
        column.AddChild(alignment);
        column.AddChild(Heading(FourthRouteProgressService.QuestName(quest), 30));
        var dormant = FourthRouteProgressService.CreateRelicPreview(quest, 0);
        var reward = FourthRouteProgressService.CreateRelicPreview(quest, 1);
        column.AddChild(Scroll($"{TextFor(quest + ".flavor")}\n\n{TextFor("condition")}：{FourthRouteProgressService.QuestText(quest, 1)}"
            + $"\n\n{TextFor("dormant")}{dormant.Title.GetFormattedText()}"
            + $"\n\n{TextFor("reward")}{reward.Title.GetFormattedText()}\n{reward.DynamicDescription.GetFormattedText()}"));
        var accept = ActionButton(TextFor("accept"), "Accept" + quest);
        accept.Pressed += () => TaskHelper.RunSafely(Confirm(quest));
        column.AddChild(accept);
        return panel;
    }

    private async Task Confirm(FourthRouteQuest quest)
    {
        if (_busy || _closed || !_isCurrent()) return;
        _busy = true;
        foreach (var button in _buttons) button.Disabled = true;
        try
        {
            if (!await FourthRouteOpeningService.Confirm(_player, quest))
                throw new InvalidOperationException("Opening choice no longer matches the saved offer.");
            if (_closed || !_isCurrent()) { Close(false); return; }
            ClearPage();
            _body.AddChild(Heading(FourthRouteProgressService.QuestName(quest), 38));
            _body.AddChild(Scroll(TextFor(quest + ".story")));
            var proceed = ActionButton(TextFor("continue"), "EnterSpire");
            proceed.Pressed += () =>
            {
                if (!_busy && !_closed && _isCurrent() && FourthRouteOpeningService.Finish(_run)) Close(true);
            };
            _body.AddChild(proceed);
            FocusFirst();
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[FourthRouteOpening] " + ex);
            _completion.TrySetException(ex);
            Close(false);
        }
        finally { _busy = false; }
    }

    private Button ActionButton(string text, string name)
    {
        var button = new Button { Name = name, Text = text, CustomMinimumSize = new Vector2(230, 60),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter, FocusMode = FocusModeEnum.All,
            MouseDefaultCursorShape = CursorShape.PointingHand };
        button.AddThemeFontSizeOverride("font_size", 26);
        _buttons.Add(button);
        return button;
    }

    private static Label Heading(string text, int size) => new Label { Text = text,
        HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart,
        MouseFilter = MouseFilterEnum.Ignore }.WithFontSize(size, new Color(.96f, .91f, .77f));

    private static ScrollContainer Scroll(string text)
    {
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 60) };
        var label = new MegaRichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false,
            AutoSizeEnabled = false, MinFontSize = 24, MaxFontSize = 24,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Pass,
            AutowrapMode = TextServer.AutowrapMode.WordSmart };
        foreach (string key in new[] { "normal_font", "bold_font", "italics_font", "bold_italics_font", "mono_font" })
            label.AddThemeFontOverride(key, label.GetThemeDefaultFont());
        label.AddThemeFontSizeOverride("normal_font_size", 24);
        label.AddThemeColorOverride("default_color", new Color(.91f, .92f, .96f));
        label.Text = text;
        scroll.AddChild(label);
        return scroll;
    }

    private void FocusFirst() => Callable.From(() =>
    {
        if (!_closed && IsInsideTree()) DefaultFocusedControl?.GrabFocus();
    }).CallDeferred();

    private void Close(bool result)
    {
        if (_closed) return;
        _closed = true;
        _completion.TrySetResult(result);
        var container = NModalContainer.Instance;
        if (container != null && ReferenceEquals(container.OpenModal, this)) container.Clear();
        else QueueFree();
    }

    public override void _Process(double delta)
    {
        if (!_isCurrent()) Close(false);
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (input.IsActionPressed("ui_cancel")) GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        _closed = true;
        NHotkeyManager.Instance?.RemoveBlockingScreen(this);
        _completion.TrySetResult(false);
    }
}
