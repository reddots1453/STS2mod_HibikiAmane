using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Characters.Starts;
using MaidenSuccubus.Data;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.UI;

/// <summary>Shion-style information rows and portrait carousel; locked starts remain browsable.</summary>
internal sealed class StartRouteSelector
{
    private static readonly MaidenSuccubusStartProfileId[] Routes =
        [MaidenSuccubusStartProfileId.Normal, MaidenSuccubusStartProfileId.Succubus, MaidenSuccubusStartProfileId.HolyMaiden];
    private static readonly Vector2 DesignSize = new(610, 720);
    private readonly NCharacterSelectScreen _screen;
    private readonly Control _panel;
    private readonly StartRouteRailArt _rail;
    private readonly NConfirmButton _embark;
    private readonly MegaRichTextLabel _title;
    private readonly MegaRichTextLabel[] _rows = new MegaRichTextLabel[3];
    private readonly MegaRichTextLabel _characterDescription;
    private readonly List<(MaidenSuccubusStartProfileId Route, StartRoutePortraitArt Button, MegaRichTextLabel Label)> _portraits = [];
    private bool _blockedEmbark, _restoreEmbark, _eligible, _layoutAvailable = true;
    private NButton? _focusedButton;
    private Tween? _transition;
    private MaidenSuccubusStartProfileId? _paintedRoute;

    internal StartRouteSelector(NCharacterSelectScreen screen)
    {
        _screen = screen;
        _embark = screen.GetNode<NConfirmButton>("ConfirmButton");
        var titleSource = screen.GetNode<MegaRichTextLabel>("InfoPanel/VBoxContainer/Relic/Name/RichTextLabel");
        var textSource = screen.GetNode<MegaRichTextLabel>("InfoPanel/VBoxContainer/Relic/Description");
        _characterDescription = screen.GetNode<MegaRichTextLabel>("InfoPanel/VBoxContainer/DescriptionLabel");
        _panel = new Control { Name = "MaidenStartingRoute", Size = DesignSize,
            MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        screen.AddChild(_panel);
        _rail = new StartRouteRailArt { Name = "DiagonalRail", Size = DesignSize,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        _panel.AddChild(_rail);
        _title = Label(titleSource, "RouteTitle", "", 28, new Rect2(34, 8, 550, 42));
        _panel.AddChild(_title);
        for (int i = 0; i < _rows.Length; i++)
        {
            _rows[i] = Label(textSource, "Info" + i, "", 22,
                new Rect2(64 + i * 20, 61 + i * 58, 525 - i * 20, 47));
            _panel.AddChild(_rows[i]);
        }
        foreach (var route in Routes)
        {
            string portrait = route switch
            {
                MaidenSuccubusStartProfileId.HolyMaiden => "character/character_armor_3.png",
                MaidenSuccubusStartProfileId.Succubus => "character/character_corrupt_armor_3.png",
                _ => "character/character_normal.png",
            };
            var button = new StartRoutePortraitArt { Name = "Route" + route,
                Size = StartRoutePortraitArt.PortraitSize, PivotOffset = StartRoutePortraitArt.PortraitSize / 2,
                Portrait = RuntimeTextureAssets.Load(portrait), FocusMode = Control.FocusModeEnum.All,
                MouseFilter = Control.MouseFilterEnum.Stop };
            _panel.AddChild(button);
            var label = Label(titleSource, "MemoryName", "", 19, new Rect2(12, 103, 207, 51));
            button.AddChild(label);
            _portraits.Add((route, button, label));
            button.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => Select(route)));
            button.Connect(NClickableControl.SignalName.Focused, Callable.From<NButton>(_ => Safe.Run(() =>
            { _focusedButton = button; PaintPortraits(false); }, "StartRoutes.Focus")));
            button.Connect(NClickableControl.SignalName.Unfocused, Callable.From<NButton>(_ => Safe.Run(() =>
            { if (ReferenceEquals(_focusedButton, button)) _focusedButton = null; PaintPortraits(false); }, "StartRoutes.Unfocus")));
        }
        screen.Resized += Layout;
        _embark.ItemRectChanged += Layout;
        Layout();
    }

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
        if (!GodotObject.IsInstanceValid(_panel)) return;
        Vector2 size = _screen.Size;
        if (size.X <= 0 || size.Y <= 0) return;
        // Work entirely in the screen's local canvas. No anchors + scaled offsets.
        // Reserve the actual native button rectangle, including its animation.
        Transform2D toScreen = _screen.GetGlobalTransform().AffineInverse();
        Rect2 button = _embark.GetGlobalRect();
        Vector2 a = toScreen * button.Position;
        Vector2 b = toScreen * button.End;
        float top = size.Y * .16f;
        float bottom = button.Size.Y > 0
            ? Math.Min(size.Y, Math.Min(a.Y, b.Y)) - 32 : size.Y * .80f - 32;
        float scale = Math.Min(1f, Math.Min((bottom - top) / DesignSize.Y,
            Math.Min(size.Y / 1080f, (size.X * .34f - 32) / DesignSize.X)));
        _layoutAvailable = scale > 0;
        if (!_layoutAvailable) { _panel.Hide(); return; }
        _panel.Scale = new Vector2(scale, scale);
        _panel.Size = DesignSize;
        _panel.Position = new Vector2(size.X - DesignSize.X * scale - Math.Clamp(size.X * .016f, 16, 32), top);
        _panel.Visible = _eligible && _screen.Lobby?.LocalPlayer.character is MaidenSuccubusCharacter;
    }, "StartRoutes.Layout");

    private MaidenSuccubusStartProfileId Current =>
        _screen.Lobby != null && StarterRelicChoice.Handle.Lobby.TryGet(_screen.Lobby, _screen.Lobby.LocalPlayer.id, out var choice)
            ? StartUnlockProgress.Normalize(choice.Route) : MaidenSuccubusStartProfileId.Normal;

    private static string Name(MaidenSuccubusStartProfileId route) => route switch
    {
        MaidenSuccubusStartProfileId.Succubus => "秘密的记忆：淫欲的囚徒",
        MaidenSuccubusStartProfileId.HolyMaiden => "秘密的记忆：纯洁无暇",
        _ => "秘密的记忆：初尝快乐",
    };

    private static Color Accent(MaidenSuccubusStartProfileId route) => route switch
    {
        MaidenSuccubusStartProfileId.Succubus => new Color("ed74ba"),
        MaidenSuccubusStartProfileId.HolyMaiden => new Color("f0d46e"),
        _ => new Color("8ed4e5"),
    };

    private void PaintPortraits(bool animate)
    {
        var selected = Current;
        int current = Array.IndexOf(Routes, selected);
        bool changed = _paintedRoute != selected;
        bool moving = animate && changed && _paintedRoute != null;
        if (changed)
        {
            _transition?.Kill();
            _transition = moving ? _screen.CreateTween().SetParallel().SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out) : null;
        }
        for (int i = 0; i < _portraits.Count; i++)
        {
            var item = _portraits[i];
            int relative = (i - current + 3) % 3; // 2 is previous, 1 is next.
            Vector2 center = relative switch { 0 => new(435, 489), 2 => new(292, 388), _ => new(292, 628) };
            Vector2 scale = relative == 0 ? Vector2.One : new Vector2(.7f, .7f);
            Vector2 position = center - StartRoutePortraitArt.PortraitSize / 2;
            if (changed)
            {
                if (moving)
                {
                    _transition!.TweenProperty(item.Button, "position", position, .22);
                    _transition.TweenProperty(item.Button, "scale", scale, .22);
                }
                else { item.Button.Position = position; item.Button.Scale = scale; }
            }
            Color accent = Accent(item.Route);
            item.Button.Paint(accent, item.Route == selected, ReferenceEquals(_focusedButton, item.Button),
                !StartUnlockProgress.IsUnlocked(item.Route));
            item.Label.Text = $"[center][color=#{accent.ToHtml(false)}]{Name(item.Route).Replace("：", "：\n")}[/color][/center]";
            item.Button.FocusNeighborTop = _portraits[(i + 2) % 3].Button.GetPath();
            item.Button.FocusNeighborBottom = _portraits[(i + 1) % 3].Button.GetPath();
            item.Button.FocusNeighborLeft = item.Button.FocusNeighborTop;
            item.Button.FocusNeighborRight = _embark.GetPath();
        }
        _paintedRoute = selected;
        // Selected portrait sits in front, but entirely inside the reserved region.
        _panel.MoveChild(_portraits[current].Button, _panel.GetChildCount() - 1);
    }

    internal void Close()
    {
        _eligible = false;
        _transition?.Kill(); _transition = null; _paintedRoute = null;
        _panel.Hide();
        foreach (var item in _portraits) item.Button.SetEnabled(false);
        ReleaseEmbark();
    }

    internal void CharacterSelected(bool locked)
    {
        if (!locked) return;
        _blockedEmbark = false;
        _restoreEmbark = false;
    }

    internal void Refresh(bool eligible)
    {
        _eligible = eligible;
        var lobby = _screen.Lobby;
        Layout();
        _panel.Visible = eligible && _layoutAvailable && lobby != null && lobby.LocalPlayer.character is MaidenSuccubusCharacter;
        if (!_panel.Visible || lobby == null) { ReleaseEmbark(); return; }
        var route = Current;
        bool unlocked = StartUnlockProgress.IsUnlocked(route);
        string color = Accent(route).ToHtml(false);
        string value = StartUnlockProgress.InitialValue(route).ToString("+0;-0;0");
        string condition = route switch
        {
            MaidenSuccubusStartProfileId.Succubus => "天音以最终堕落值 ≥ +3 通关。",
            MaidenSuccubusStartProfileId.HolyMaiden => "天音以最终堕落值 ≤ -3 通关。",
            _ => "默认获得。",
        };
        _rail.AccentColor = Accent(route); _rail.QueueRedraw();
        _title.Text = $"[color=#{color}]{Name(route)}[/color]";
        _rows[0].Text = "获取条件：" + condition;
        _rows[1].Text = $"初始堕落值：[color=#{color}]{value}[/color]";
        _rows[2].Text = unlocked ? $"[color=#{color}]已解锁[/color]" : "未解锁";
        _characterDescription.Text = route switch
        {
            MaidenSuccubusStartProfileId.HolyMaiden =>
                "与魔导书·娅露丝相遇，获得魔法力量的少女。\n拥有正直的心，身体对污秽之事一无所知。",
            MaidenSuccubusStartProfileId.Succubus =>
                "与魔导书·娅露丝相遇，获得魔法力量的少女。\n身体已经沉醉于快感，理性正在被侵蚀。",
            _ => new LocString("characters", lobby.LocalPlayer.character.CharacterSelectDesc).GetFormattedText(),
        };
        foreach (var item in _portraits) item.Button.SetEnabled(!lobby.LocalPlayer.isReady);
        PaintPortraits(true);
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
