using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.addons.mega_text;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Characters.Starts;
using MaidenSuccubus.Data;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.UI;

/// <summary>Native-looking route row, owned by the same screen as the relic selector.</summary>
internal sealed class StartRouteSelector
{
    private readonly NCharacterSelectScreen _screen;
    private readonly Control _row;
    private readonly MegaRichTextLabel _title;
    private readonly NButton _previous;
    private readonly NButton _next;

    internal StartRouteSelector(NCharacterSelectScreen screen)
    {
        _screen = screen;
        var panel = screen.GetNode<VBoxContainer>("InfoPanel/VBoxContainer");
        var relic = panel.GetNode<Control>("Relic");
        _row = new Control { Name = "MaidenStartingRoute", CustomMinimumSize = new Vector2(0, 48),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        panel.AddChild(_row);
        panel.MoveChild(_row, relic.GetIndex());
        var source = relic.GetNode<MegaRichTextLabel>("Name/RichTextLabel");
        _title = MakeLabel(source, "RouteName", 28, 0, 44);
        _row.AddChild(_title);
        var anchor = new Control { Name = "ArrowAnchor", Position = new Vector2(0, 0), Size = new Vector2(0, 44),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        _row.AddChild(anchor);
        var ascension = screen.GetNode<NAscensionPanel>("%AscensionPanel");
        var layout = new StarterRelicArrowLayout(_row, anchor,
            ascension.GetNode<NButton>("HBoxContainer/LeftArrowContainer/LeftArrow"),
            ascension.GetNode<NButton>("HBoxContainer/RightArrowContainer/RightArrow"));
        _previous = layout.Previous; _next = layout.Next;
        _previous.Name = "MaidenStartingRoutePrevious"; _next.Name = "MaidenStartingRouteNext";
        _previous.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => Toggle(-1)));
        _next.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => Toggle(1)));
        _previous.FocusNeighborRight = _next.GetPath(); _next.FocusNeighborLeft = _previous.GetPath();
    }

    private static MegaRichTextLabel MakeLabel(MegaRichTextLabel source, string name, int fontSize, float top, float bottom)
    {
        var label = (MegaRichTextLabel)source.Duplicate((int)Node.DuplicateFlags.Scripts);
        NativeUiClone.RestoreOwners(label);
        label.Name = name;
        label.UniqueNameInOwner = false;
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        label.OffsetLeft = 0; label.OffsetRight = 0; label.OffsetTop = top; label.OffsetBottom = bottom;
        label.CustomMinimumSize = Vector2.Zero;
        label.FitContent = false;
        label.AutoSizeEnabled = false;
        label.ScrollActive = false;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.BbcodeEnabled = true;
        label.AddThemeFontSizeOverride("normal_font_size", fontSize);
        return label;
    }

    internal void Close()
    {
        _row.Hide(); _previous.SetEnabled(false); _next.SetEnabled(false);
    }

    internal void Refresh(bool eligible)
    {
        var lobby = _screen.Lobby;
        _row.Visible = eligible && lobby != null && lobby.LocalPlayer.character is MaidenSuccubusCharacter;
        if (!_row.Visible || lobby == null) return;
        var route = StarterRelicChoice.Handle.Lobby.TryGet(lobby, lobby.LocalPlayer.id, out var choice)
            ? StartUnlockProgress.Normalize(choice.Route) : MaidenSuccubusStartProfileId.Normal;
        if (!StartUnlockProgress.IsUnlocked(route))
        {
            route = MaidenSuccubusStartProfileId.Normal;
            if (!lobby.LocalPlayer.isReady) SaveRoute(route);
        }
        string name = route switch
        {
            MaidenSuccubusStartProfileId.Succubus => "[color=#ed74ba]堕落开局 · 堕落值 +3[/color]",
            MaidenSuccubusStartProfileId.HolyMaiden => "[color=#f0d46e]圣洁开局 · 堕落值 -3[/color]",
            _ => "中立开局 · 堕落值 0",
        };
        _title.Text = $"[center]{name}[/center]";
        bool canSwitch = !lobby.LocalPlayer.isReady
            && (StartUnlockProgress.IsUnlocked(MaidenSuccubusStartProfileId.Succubus)
                || StartUnlockProgress.IsUnlocked(MaidenSuccubusStartProfileId.HolyMaiden));
        _previous.Visible = _next.Visible = true;
        _previous.SetEnabled(canSwitch); _next.SetEnabled(canSwitch);
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

    private void Toggle(int direction) => Safe.Run(() =>
    {
        var lobby = _screen.Lobby;
        if (!_row.IsVisibleInTree() || lobby == null || lobby.LocalPlayer.isReady
            || lobby.LocalPlayer.character is not MaidenSuccubusCharacter) return;
        var route = StarterRelicChoice.Handle.Lobby.TryGet(lobby, lobby.LocalPlayer.id, out var choice)
            ? choice.Route : MaidenSuccubusStartProfileId.Normal;
        SaveRoute(StartUnlockProgress.Next(route, direction));
        Refresh(true);
    }, "StartRoutes.Toggle");
}
