using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace MaidenSuccubus.UI;

/// <summary>Mandatory post-transition choice for runs started without goddess trials.</summary>
public sealed partial class ActAlignmentChoiceScreen : Control, IScreenContext
{
    private readonly TaskCompletionSource<int?> _choice = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Button _first;
    private bool _resolved;

    public Control? DefaultFocusedControl => _first;

    private ActAlignmentChoiceScreen(int actIndex)
    {
        Name = "MaidenActAlignmentChoice";
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var shade = new ColorRect { Color = new Color(0.015f, 0.02f, 0.04f, 0.88f), MouseFilter = MouseFilterEnum.Stop };
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(shade);
        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(800, 340), MouseFilter = MouseFilterEnum.Ignore };
        center.AddChild(panel);
        var content = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        content.AddThemeConstantOverride("separation", 24);
        panel.AddChild(content);
        var heading = new Label { Text = $"进入第{actIndex + 1}幕：选择堕落值变化", HorizontalAlignment = HorizontalAlignment.Center };
        heading.AddThemeFontSizeOverride("font_size", 31);
        content.AddChild(heading);
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 36);
        content.AddChild(row);
        _first = ChoiceButton("堕落值 +2", 2);
        row.AddChild(_first);
        row.AddChild(ChoiceButton("堕落值 -2", -2));
    }

    private Button ChoiceButton(string title, int delta)
    {
        var button = new Button { Text = title, CustomMinimumSize = new Vector2(290, 100), FocusMode = FocusModeEnum.All };
        button.AddThemeFontSizeOverride("font_size", 27);
        button.Pressed += () =>
        {
            if (_resolved) return;
            _resolved = true;
            _choice.TrySetResult(delta);
            NModalContainer.Instance?.Clear();
        };
        return button;
    }

    internal static async Task<int?> Show(int actIndex)
    {
        var container = NModalContainer.Instance ?? throw new InvalidOperationException("Act alignment choice requires modal container.");
        if (container.OpenModal != null) throw new InvalidOperationException("Act alignment choice cannot replace another modal.");
        var screen = new ActAlignmentChoiceScreen(actIndex);
        container.Add(screen);
        Callable.From(() => screen._first.GrabFocus()).CallDeferred();
        return await screen._choice.Task;
    }

    public override void _EnterTree() => NHotkeyManager.Instance?.AddBlockingScreen(this);
    public override void _ExitTree()
    {
        NHotkeyManager.Instance?.RemoveBlockingScreen(this);
        if (!_resolved) _choice.TrySetResult(null);
    }
}
