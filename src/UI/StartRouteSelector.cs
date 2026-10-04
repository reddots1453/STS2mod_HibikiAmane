using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.addons.mega_text;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Characters.Starts;
using MaidenSuccubus.Data;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.UI;

/// <summary>Screen-owned route preview; locked routes are browsable but cannot embark.</summary>
internal sealed class StartRouteSelector
{
    private static readonly MaidenSuccubusStartProfileId[] Routes =
        [MaidenSuccubusStartProfileId.Normal, MaidenSuccubusStartProfileId.Succubus, MaidenSuccubusStartProfileId.HolyMaiden];
    private readonly NCharacterSelectScreen _screen;
    private readonly Control _panel;
    private readonly NConfirmButton _embark;
    private readonly MegaRichTextLabel _details;
    private readonly MegaRichTextLabel _status;
    private readonly List<(MaidenSuccubusStartProfileId Route, NButton Button, StyleBoxFlat Style, MegaRichTextLabel Label)> _tabs = [];
    private bool _blockedEmbark;
    private bool _restoreEmbark;
    private NButton? _focusedButton;

    internal StartRouteSelector(NCharacterSelectScreen screen)
    {
        _screen = screen;
        _embark = screen.GetNode<NConfirmButton>("ConfirmButton");
        var titleSource = screen.GetNode<MegaRichTextLabel>("InfoPanel/VBoxContainer/Relic/Name/RichTextLabel");
        var textSource = screen.GetNode<MegaRichTextLabel>("InfoPanel/VBoxContainer/Relic/Description");
        _panel = new Control { Name = "MaidenStartingRoute", MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = 1, AnchorRight = 1, AnchorTop = 1, AnchorBottom = 1, Visible = false };
        screen.AddChild(_panel);
        var background = new Panel { Name = "Background", MouseFilter = Control.MouseFilterEnum.Ignore };
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        background.AddThemeStyleboxOverride("panel", Box(new Color(.62f, .54f, .34f), new Color(.045f, .06f, .085f, .94f), 2));
        _panel.AddChild(background);
        _panel.AddChild(Label(titleSource, "Heading", "选择开局", 30, new Rect2(24, 18, 372, 42)));

        for (int index = 0; index < Routes.Length; index++)
        {
            var route = Routes[index];
            var button = new NButton { Name = "Route" + route, Position = new Vector2(24 + index * 126, 76),
                Size = new Vector2(120, 102), FocusMode = Control.FocusModeEnum.All,
                MouseFilter = Control.MouseFilterEnum.Stop };
            _panel.AddChild(button);
            var face = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
            face.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var style = Box(Accent(route).Darkened(.5f), new Color(.065f, .08f, .11f), 1);
            face.AddThemeStyleboxOverride("panel", style);
            button.AddChild(face);
            var label = Label(titleSource, "NameAndLock", "", 23, new Rect2(6, 13, 108, 82));
            button.AddChild(label);
            _tabs.Add((route, button, style, label));
            button.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => Select(route)));
            button.Connect(NClickableControl.SignalName.Focused, Callable.From<NButton>(_ => Safe.Run(() =>
            { _focusedButton = button; PaintTabs(); }, "StartRoutes.Focus")));
            button.Connect(NClickableControl.SignalName.Unfocused, Callable.From<NButton>(_ => Safe.Run(() =>
            { if (ReferenceEquals(_focusedButton, button)) _focusedButton = null; PaintTabs(); }, "StartRoutes.Unfocus")));
        }
        for (int index = 0; index < _tabs.Count; index++)
        {
            var button = _tabs[index].Button;
            button.FocusNeighborLeft = _tabs[(index + 2) % 3].Button.GetPath();
            button.FocusNeighborRight = _tabs[(index + 1) % 3].Button.GetPath();
            button.FocusNeighborBottom = _embark.GetPath();
        }
        _details = Label(textSource, "EffectsAndConditions", "", 21, new Rect2(24, 202, 372, 200));
        _status = Label(textSource, "Availability", "", 19, new Rect2(24, 435, 372, 43));
        _panel.AddChild(_details);
        _panel.AddChild(Label(textSource, "SharedStart", "各路线共用初始牌组和可选初始遗物。", 16, new Rect2(24, 406, 372, 24)));
        _panel.AddChild(_status);
        screen.Resized += Layout;
        Layout();
    }

    private static StyleBoxFlat Box(Color border, Color fill, int width) => new()
    {
        BgColor = fill, BorderColor = border,
        BorderWidthLeft = width, BorderWidthRight = width, BorderWidthTop = width, BorderWidthBottom = width,
        CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
    };

    private static MegaRichTextLabel Label(MegaRichTextLabel source, string name, string text, int fontSize, Rect2 rect)
    {
        var label = (MegaRichTextLabel)source.Duplicate((int)Node.DuplicateFlags.Scripts);
        NativeUiClone.RestoreOwners(label);
        label.Name = name;
        label.UniqueNameInOwner = false;
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        label.Position = rect.Position;
        label.Size = rect.Size;
        label.CustomMinimumSize = Vector2.Zero;
        label.FitContent = false;
        label.AutoSizeEnabled = false;
        label.ScrollActive = false;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.BbcodeEnabled = true;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        foreach (string kind in new[] { "normal", "bold", "italics", "bold_italics", "mono" })
            label.AddThemeFontSizeOverride(kind + "_font_size", fontSize);
        label.Text = text;
        return label;
    }

    private void Layout() => Safe.Run(() =>
    {
        // Standard 1920x1080 canvas; proportional shrink leaves remote-player
        // information above and the native confirm button below this panel.
        float scale = Math.Min(1f, Math.Min(_screen.Size.Y / 1080f, _screen.Size.X / 1920f));
        if (scale <= 0) scale = 1;
        _panel.Scale = new Vector2(scale, scale);
        _panel.OffsetLeft = -(420 + 52) * scale;
        _panel.OffsetTop = -(490 + 190) * scale;
        _panel.OffsetRight = _panel.OffsetLeft + 420;
        _panel.OffsetBottom = _panel.OffsetTop + 490;
    }, "StartRoutes.Layout");

    private MaidenSuccubusStartProfileId Current =>
        _screen.Lobby != null && StarterRelicChoice.Handle.Lobby.TryGet(_screen.Lobby, _screen.Lobby.LocalPlayer.id, out var choice)
            ? StartUnlockProgress.Normalize(choice.Route) : MaidenSuccubusStartProfileId.Normal;

    private static string Name(MaidenSuccubusStartProfileId route) => route switch
    {
        MaidenSuccubusStartProfileId.Succubus => "堕落",
        MaidenSuccubusStartProfileId.HolyMaiden => "圣洁",
        _ => "中立",
    };

    private static Color Accent(MaidenSuccubusStartProfileId route) => route switch
    {
        MaidenSuccubusStartProfileId.Succubus => new Color("ed74ba"),
        MaidenSuccubusStartProfileId.HolyMaiden => new Color("f0d46e"),
        _ => new Color("8ed4e5"),
    };

    private void PaintTabs()
    {
        var selected = Current;
        foreach (var tab in _tabs)
        {
            bool unlocked = StartUnlockProgress.IsUnlocked(tab.Route);
            bool current = tab.Route == selected;
            Color accent = Accent(tab.Route);
            bool focused = ReferenceEquals(_focusedButton, tab.Button);
            tab.Style.BorderColor = current || focused ? accent : accent.Darkened(.55f);
            int width = current ? 3 : focused ? 2 : 1;
            tab.Style.BorderWidthLeft = tab.Style.BorderWidthRight = tab.Style.BorderWidthTop = tab.Style.BorderWidthBottom = width;
            tab.Style.BgColor = current ? accent.Darkened(.82f) : new Color(.065f, .08f, .11f);
            string value = StartUnlockProgress.InitialValue(tab.Route).ToString("+0;-0;0");
            tab.Label.Text = $"[center][color=#{accent.ToHtml(false)}]{Name(tab.Route)} · {value}[/color]\n[font_size=17]{(unlocked ? "已解锁" : "未解锁")}[/font_size][/center]";
        }
    }

    internal void Close()
    {
        _panel.Hide();
        foreach (var tab in _tabs) tab.Button.SetEnabled(false);
        ReleaseEmbark();
    }

    internal void CharacterSelected(bool locked)
    {
        // Native character selection has already disabled confirm for a locked
        // character. Do not undo that decision when leaving our locked preview.
        if (!locked) return;
        _blockedEmbark = false;
        _restoreEmbark = false;
    }

    internal void Refresh(bool eligible)
    {
        var lobby = _screen.Lobby;
        _panel.Visible = eligible && lobby != null && lobby.LocalPlayer.character is MaidenSuccubusCharacter;
        if (!_panel.Visible || lobby == null) { ReleaseEmbark(); return; }
        var route = Current;
        bool unlocked = StartUnlockProgress.IsUnlocked(route);
        string color = Accent(route).ToHtml(false);
        string value = StartUnlockProgress.InitialValue(route).ToString("+0;-0;0");
        string effect = route switch
        {
            MaidenSuccubusStartProfileId.Succubus => "圣洁牌被封印，不参与战斗。",
            MaidenSuccubusStartProfileId.HolyMaiden => "堕落牌被封印，不参与战斗。",
            _ => "开局不因堕落值封印卡牌。",
        };
        string condition = route switch
        {
            MaidenSuccubusStartProfileId.Succubus => "天音通关时，最终堕落值 ≥ +3。",
            MaidenSuccubusStartProfileId.HolyMaiden => "天音通关时，最终堕落值 ≤ -3。",
            _ => "默认获得。",
        };
        _details.Text = $"[color=#{color}]开局效果[/color]\n初始堕落值：[color=#{color}]{value}[/color]\n{effect}\n\n[color=#{color}]解锁条件[/color]\n{condition}";
        _status.Text = unlocked ? $"[color=#{color}]已解锁 · 可以开始游戏[/color]"
            : "[color=#c8bfb4]未解锁 · 仅可预览，不能以此开局[/color]";
        foreach (var tab in _tabs) tab.Button.SetEnabled(!lobby.LocalPlayer.isReady);
        PaintTabs();
        if (!lobby.LocalPlayer.isReady)
        {
            if (unlocked) ReleaseEmbark();
            else BlockEmbark();
        }
    }

    private void BlockEmbark()
    {
        if (!_blockedEmbark) _restoreEmbark = _embark.IsEnabled;
        _blockedEmbark = true;
        _embark.Disable();
    }

    private void ReleaseEmbark()
    {
        if (!_blockedEmbark) return;
        _blockedEmbark = false;
        if (_restoreEmbark && _screen.Lobby?.LocalPlayer.isReady != true) _embark.Enable();
        _restoreEmbark = false;
    }

    internal bool CanEmbark()
    {
        var lobby = _screen.Lobby;
        if (lobby == null || lobby.LocalPlayer.character is not MaidenSuccubusCharacter) return true;
        bool unlocked = StartUnlockProgress.IsUnlocked(Current);
        if (!unlocked) { BlockEmbark(); return false; }
        // Revalidate the current profile and refresh the authoritative lobby
        // flag before readiness freezes its snapshot. Preview never grants it.
        SaveRoute(Current);
        return true;
    }

    private void SaveRoute(MaidenSuccubusStartProfileId route)
    {
        var lobby = _screen.Lobby;
        StarterRelicChoice.Handle.Lobby.Modify(lobby, lobby.LocalPlayer.id, state =>
        {
            state.Route = route;
            state.RouteUnlockedAtSelection = StartUnlockProgress.IsUnlocked(route);
            state.RouteApplied = false;
        });
    }

    private void Select(MaidenSuccubusStartProfileId route) => Safe.Run(() =>
    {
        var lobby = _screen.Lobby;
        if (!_panel.IsVisibleInTree() || lobby == null || lobby.LocalPlayer.isReady
            || lobby.LocalPlayer.character is not MaidenSuccubusCharacter) return;
        SaveRoute(route);
        Refresh(true);
    }, "StartRoutes.SelectPreview");
}
