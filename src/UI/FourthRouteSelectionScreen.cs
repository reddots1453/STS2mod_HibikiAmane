using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.UI;

/// <summary>
/// Dedicated fourth-route screen based on the overlay pattern used by the
/// locally installed Hextech Runes mod. It deliberately contains no CardModel
/// nodes, so route choices can no longer be mistaken for card rewards.
/// </summary>
public sealed partial class FourthRouteSelectionScreen : Control, IOverlayScreen
{
    private enum ScreenMode { QuestChoice, Reward }

    private readonly ScreenMode _mode;
    private readonly TaskCompletionSource<FourthRouteQuest?> _questCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _rewardCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<Button> _buttons = [];
    private bool _resolved;
    private bool _closed;

    public NetScreenType ScreenType => NetScreenType.Rewards;
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl => _buttons.FirstOrDefault();

    private FourthRouteSelectionScreen(
        ScreenMode mode,
        FourthRouteQuest first,
        FourthRouteQuest? second = null)
    {
        _mode = mode;
        Name = mode == ScreenMode.QuestChoice
            ? "FourthRouteQuestSelection"
            : "FourthRouteReward";
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        BuildUi(first, second);
    }

    public static async Task<FourthRouteQuest?> ChooseQuest(
        FourthRouteQuest dark,
        FourthRouteQuest light)
    {
        NOverlayStack stack = NOverlayStack.Instance
            ?? throw new InvalidOperationException("Route selection requires NOverlayStack.");
        FourthRouteSelectionScreen screen = new(ScreenMode.QuestChoice, dark, light);
        stack.Push(screen);
        return await screen._questCompletion.Task;
    }

    public static async Task<bool> ShowReward(FourthRouteQuest quest)
    {
        NOverlayStack stack = NOverlayStack.Instance
            ?? throw new InvalidOperationException("Route reward requires NOverlayStack.");
        FourthRouteSelectionScreen screen = new(ScreenMode.Reward, quest);
        stack.Push(screen);
        return await screen._rewardCompletion.Task;
    }

    private void BuildUi(FourthRouteQuest first, FourthRouteQuest? second)
    {
        ColorRect dim = new()
        {
            Color = new Color(0.01f, 0.015f, 0.03f, 0.82f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(dim);

        CenterContainer center = new() { MouseFilter = MouseFilterEnum.Ignore };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        PanelContainer panel = new()
        {
            CustomMinimumSize = new Vector2(1160f, 720f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
        center.AddChild(panel);

        MarginContainer margin = new() { MouseFilter = MouseFilterEnum.Ignore };
        margin.AddThemeConstantOverride("margin_left", 42);
        margin.AddThemeConstantOverride("margin_right", 42);
        margin.AddThemeConstantOverride("margin_top", 34);
        margin.AddThemeConstantOverride("margin_bottom", 34);
        panel.AddChild(margin);

        VBoxContainer content = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        content.AddThemeConstantOverride("separation", 26);
        margin.AddChild(content);

        content.AddChild(CreateTitle(_mode == ScreenMode.QuestChoice
            ? "选择女神试炼"
            : "试炼完成"));

        if (_mode == ScreenMode.QuestChoice && second is FourthRouteQuest other)
        {
            content.AddChild(CreateSubtitle("选择一条路线。完成试炼后，奖励需要在独立界面领取。"));
            HBoxContainer choices = new()
            {
                Alignment = BoxContainer.AlignmentMode.Center,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            choices.AddThemeConstantOverride("separation", 38);
            choices.AddChild(CreateQuestButton(first));
            choices.AddChild(CreateQuestButton(other));
            content.AddChild(choices);
        }
        else
        {
            content.AddChild(CreateSubtitle(
                $"{FourthRouteProgressService.QuestName(first)}试炼已经达成。确认后获得始源遗物并结算路线效果。"));
            content.AddChild(CreateRewardCard(first));
            Button claim = CreateActionButton("领取奖励", new Color(0.82f, 0.66f, 0.28f));
            claim.Pressed += ResolveReward;
            _buttons.Add(claim);
            content.AddChild(claim);
        }
    }

    private Control CreateQuestButton(FourthRouteQuest quest)
    {
        FourthRouteAlignment alignment = FourthRouteProgressService.AlignmentOf(quest);
        Color accent = alignment == FourthRouteAlignment.Dark
            ? new Color(0.72f, 0.34f, 0.72f)
            : new Color(0.85f, 0.74f, 0.32f);
        Button button = new()
        {
            CustomMinimumSize = new Vector2(495f, 475f),
            Text = string.Empty,
            FocusMode = FocusModeEnum.All,
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        ApplyButtonStyles(button, accent);
        button.Pressed += () => ResolveQuest(quest);
        _buttons.Add(button);

        MarginContainer margin = new() { MouseFilter = MouseFilterEnum.Ignore };
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_top", 26);
        margin.AddThemeConstantOverride("margin_bottom", 26);
        button.AddChild(margin);

        VBoxContainer body = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        body.AddThemeConstantOverride("separation", 20);
        margin.AddChild(body);

        ColorRect stripe = new()
        {
            Color = accent,
            CustomMinimumSize = new Vector2(0f, 7f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        body.AddChild(stripe);
        body.AddChild(CreateSectionLabel(alignment == FourthRouteAlignment.Dark
            ? "七宗罪路线"
            : "七美德路线", accent, 23));
        body.AddChild(CreateSectionLabel(
            FourthRouteProgressService.QuestName(quest), Colors.White, 35));
        body.AddChild(CreateRichText(
            $"[color=#d6d9e5]试炼[/color]\n{FourthRouteProgressService.QuestText(quest)}",
            98f));
        body.AddChild(CreateRewardPreview(quest, includeCorruption: true));
        return button;
    }

    private Control CreateRewardCard(FourthRouteQuest quest)
    {
        PanelContainer card = new()
        {
            CustomMinimumSize = new Vector2(760f, 360f),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        Color accent = FourthRouteProgressService.AlignmentOf(quest) == FourthRouteAlignment.Dark
            ? new Color(0.72f, 0.34f, 0.72f)
            : new Color(0.85f, 0.74f, 0.32f);
        card.AddThemeStyleboxOverride("panel",
            CreateCardStyle(new Color(0.07f, 0.08f, 0.12f, 0.96f), accent, 3));
        MarginContainer margin = new() { MouseFilter = MouseFilterEnum.Ignore };
        margin.AddThemeConstantOverride("margin_left", 34);
        margin.AddThemeConstantOverride("margin_right", 34);
        margin.AddThemeConstantOverride("margin_top", 26);
        margin.AddThemeConstantOverride("margin_bottom", 26);
        card.AddChild(margin);
        VBoxContainer body = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        body.AddThemeConstantOverride("separation", 18);
        margin.AddChild(body);
        body.AddChild(CreateSectionLabel(
            FourthRouteProgressService.QuestName(quest) + "·始源", accent, 34));
        body.AddChild(CreateRewardPreview(quest, includeCorruption: true));
        return card;
    }

    private static Control CreateRewardPreview(FourthRouteQuest quest, bool includeCorruption)
    {
        FourthRouteRelic relic = FourthRouteProgressService.CreateRelicPreview(quest);
        HBoxContainer row = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddThemeConstantOverride("separation", 20);
        TextureRect icon = new()
        {
            Texture = relic.BigIcon,
            CustomMinimumSize = new Vector2(116f, 116f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddChild(icon);
        string corruption = includeCorruption
            ? (FourthRouteProgressService.AlignmentOf(quest) == FourthRouteAlignment.Dark
                ? "\n[color=#d88bd8]获得1点堕落值。[/color]"
                : "\n[color=#f0d878]失去1点堕落值。[/color]")
            : string.Empty;
        MegaRichTextLabel description = CreateRichText(
            $"[color=#f4eed8]{relic.Title.GetFormattedText()}[/color]\n{relic.DynamicDescription.GetFormattedText()}{corruption}",
            190f);
        description.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(description);
        return row;
    }

    private static Label CreateTitle(string text) => new Label()
    {
        Text = text,
        HorizontalAlignment = HorizontalAlignment.Center,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        ThemeTypeVariation = "HeaderLarge",
    }.WithFontSize(44, new Color(0.97f, 0.91f, 0.72f));

    private static Label CreateSubtitle(string text) => new Label()
    {
        Text = text,
        HorizontalAlignment = HorizontalAlignment.Center,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
    }.WithFontSize(21, new Color(0.84f, 0.86f, 0.91f));

    private static Label CreateSectionLabel(string text, Color color, int size) => new Label()
    {
        Text = text,
        HorizontalAlignment = HorizontalAlignment.Center,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        MouseFilter = MouseFilterEnum.Ignore,
    }.WithFontSize(size, color);

    private static MegaRichTextLabel CreateRichText(string text, float minimumHeight)
    {
        MegaRichTextLabel label = new()
        {
            Text = text,
            BbcodeEnabled = true,
            HorizontalAlignment = HorizontalAlignment.Left,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            FitContent = false,
            CustomMinimumSize = new Vector2(0f, minimumHeight),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
            MinFontSize = 17,
            MaxFontSize = 21,
        };
        ApplyDefaultRichTextTheme(label);
        label.AddThemeColorOverride("default_color", new Color(0.9f, 0.91f, 0.94f));
        return label;
    }

    private static Button CreateActionButton(string text, Color accent)
    {
        Button button = new()
        {
            Text = text,
            CustomMinimumSize = new Vector2(310f, 72f),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            FocusMode = FocusModeEnum.All,
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        button.AddThemeFontSizeOverride("font_size", 26);
        button.AddThemeColorOverride("font_color", Colors.White);
        ApplyButtonStyles(button, accent, 18);
        return button;
    }

    private static void ApplyButtonStyles(Button button, Color accent, int radius = 24)
    {
        button.AddThemeStyleboxOverride("normal",
            CreateCardStyle(new Color(0.06f, 0.075f, 0.11f, 0.96f), accent.Darkened(0.25f), 2, radius));
        button.AddThemeStyleboxOverride("hover",
            CreateCardStyle(new Color(0.09f, 0.105f, 0.15f, 0.98f), accent, 4, radius));
        button.AddThemeStyleboxOverride("pressed",
            CreateCardStyle(new Color(0.04f, 0.055f, 0.09f, 1f), accent.Lightened(0.12f), 4, radius));
        button.AddThemeStyleboxOverride("focus",
            CreateCardStyle(new Color(0.09f, 0.105f, 0.15f, 0.98f), accent, 4, radius));
    }

    private static StyleBoxFlat CreatePanelStyle() =>
        CreateCardStyle(new Color(0.035f, 0.045f, 0.07f, 0.98f),
            new Color(0.54f, 0.5f, 0.42f, 0.8f), 2, 30);

    private static StyleBoxFlat CreateCardStyle(
        Color background,
        Color border,
        int borderWidth,
        int radius = 24)
    {
        StyleBoxFlat style = new()
        {
            BgColor = background,
            BorderColor = border,
            ShadowColor = new Color(0f, 0f, 0f, 0.34f),
            ShadowSize = 14,
            ShadowOffset = new Vector2(0f, 8f),
        };
        style.SetBorderWidthAll(borderWidth);
        style.SetCornerRadiusAll(radius);
        return style;
    }

    private static void ApplyDefaultRichTextTheme(MegaRichTextLabel label)
    {
        Font font = label.GetThemeDefaultFont();
        if (font == null) return;
        foreach (string key in new[] { "normal_font", "bold_font", "italics_font", "bold_italics_font", "mono_font" })
            label.AddThemeFontOverride(key, font);
    }

    private void ResolveQuest(FourthRouteQuest quest)
    {
        if (_resolved) return;
        _resolved = true;
        DisableButtons();
        _questCompletion.TrySetResult(quest);
        NOverlayStack.Instance?.Remove(this);
    }

    private void ResolveReward()
    {
        if (_resolved) return;
        _resolved = true;
        DisableButtons();
        _rewardCompletion.TrySetResult(true);
        NOverlayStack.Instance?.Remove(this);
    }

    private void DisableButtons()
    {
        foreach (Button button in _buttons) button.Disabled = true;
    }

    public void AfterOverlayOpened()
    {
        Visible = true;
        DefaultFocusedControl?.GrabFocus();
    }

    public void AfterOverlayClosed()
    {
        if (_closed) return;
        _closed = true;
        if (!_resolved)
        {
            _questCompletion.TrySetResult(null);
            _rewardCompletion.TrySetResult(false);
        }
        QueueFree();
    }

    public void AfterOverlayShown()
    {
        Visible = true;
        DefaultFocusedControl?.GrabFocus();
    }

    public void AfterOverlayHidden() => Visible = false;
}

internal static class FourthRouteLabelExtensions
{
    internal static Label WithFontSize(this Label label, int size, Color color)
    {
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.72f));
        label.AddThemeConstantOverride("outline_size", 4);
        return label;
    }
}
