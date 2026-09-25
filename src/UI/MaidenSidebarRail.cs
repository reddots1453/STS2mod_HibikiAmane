using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace MaidenSuccubus.UI;

/// <summary>
/// Shared, narrow presentation rail for the character's persistent resources.
/// Keeping both widgets under one parent guarantees stable vertical spacing
/// and lets settings-screen visibility be handled as one atomic UI state.
/// </summary>
internal sealed partial class MaidenSidebarRail : Control
{
    private const string RailNodeName = "MaidenSidebarRail";
    private static readonly Vector2 ScreenPosition = new(14f, 206f);
    private static readonly Vector2 RailSize = new(78f, 304f);

    internal static readonly Vector2 TemptationPosition = new(7f, 8f);
    internal static readonly Vector2 DesirePosition = new(7f, 88f);

    private NTopBar? _topBar;
    private NSettingsScreen? _settingsScreen;
    private bool _settingsOpen;

    internal static MaidenSidebarRail GetOrCreate(NTopBar topBar)
    {
        MaidenSidebarRail? existing =
            topBar.GetNodeOrNull<MaidenSidebarRail>(RailNodeName);
        if (existing != null)
        {
            return existing;
        }

        var rail = new MaidenSidebarRail
        {
            Name = RailNodeName,
            CustomMinimumSize = RailSize,
            Size = RailSize,
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 100,
        };
        topBar.AddChild(rail);
        rail.Initialize(topBar);
        return rail;
    }

    public override void _Process(double delta)
    {
        UpdateScreenPosition();
        if (_settingsScreen == null)
        {
            TryBindSettingsScreen();
        }
        else
        {
            // Signals are authoritative. This visibility check is a fallback
            // for transitions that restore an already-instantiated submenu.
            bool visiblyOpen = _settingsScreen.IsVisibleInTree();
            if (visiblyOpen != _settingsOpen)
            {
                SetSettingsOpen(visiblyOpen);
            }
        }
    }

    public override void _ExitTree()
    {
        if (_settingsScreen != null
            && GodotObject.IsInstanceValid(_settingsScreen))
        {
            _settingsScreen.SettingsOpened -= OnSettingsOpened;
            _settingsScreen.SettingsClosed -= OnSettingsClosed;
        }
        base._ExitTree();
    }

    private void Initialize(NTopBar topBar)
    {
        _topBar = topBar;
        BuildBackdrop();
        UpdateScreenPosition();
        SetProcess(true);
        CallDeferred(nameof(TryBindSettingsScreen));
    }

    private void BuildBackdrop()
    {
        var backdrop = new Panel
        {
            Name = "Backdrop",
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 0,
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.018f, 0.026f, 0.052f, 0.76f),
            BorderColor = new Color(0.44f, 0.55f, 0.78f, 0.62f),
            ShadowColor = new Color(0f, 0f, 0f, 0.42f),
            ShadowSize = 7,
            ShadowOffset = new Vector2(2f, 3f),
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(13);
        backdrop.AddThemeStyleboxOverride("panel", style);
        AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var separator = new ColorRect
        {
            Name = "ResourceSeparator",
            Position = new Vector2(16f, 80f),
            Size = new Vector2(46f, 1f),
            Color = new Color(0.88f, 0.56f, 0.76f, 0.48f),
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 1,
        };
        AddChild(separator);
    }

    private void UpdateScreenPosition()
    {
        if (_topBar != null && GodotObject.IsInstanceValid(_topBar))
        {
            Position = ScreenPosition - _topBar.GlobalPosition;
        }
    }

    private void TryBindSettingsScreen()
    {
        NSettingsScreen? settings = NRun.Instance?.GlobalUi?
            .SubmenuStack?.Stack?.GetSubmenuType<NSettingsScreen>();
        if (settings == null || !GodotObject.IsInstanceValid(settings))
        {
            return;
        }

        _settingsScreen = settings;
        settings.SettingsOpened += OnSettingsOpened;
        settings.SettingsClosed += OnSettingsClosed;
        SetSettingsOpen(settings.IsVisibleInTree());
    }

    private void OnSettingsOpened() => SetSettingsOpen(true);

    private void OnSettingsClosed() => SetSettingsOpen(false);

    private void SetSettingsOpen(bool open)
    {
        _settingsOpen = open;
        Visible = !open;
        if (!open)
        {
            return;
        }

        // Hiding a parent does not emit MouseExited for a hovered child.
        // Explicitly remove any resource tooltip with the rail.
        foreach (Control child in GetChildren().OfType<Control>())
        {
            NHoverTipSet.Remove(child);
        }
    }
}
