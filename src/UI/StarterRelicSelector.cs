using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.addons.mega_text;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Characters.Starts;
using MaidenSuccubus.Data;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.UI;

/// <summary>Screen-owned controls; no static global choice, polling or second modal.</summary>
internal sealed class StarterRelicSelector
{
    private static readonly ConditionalWeakTable<NCharacterSelectScreen, StarterRelicSelector> Instances = new();
    private readonly NCharacterSelectScreen _screen;
    private readonly HBoxContainer _row;
    private readonly Button _previous;
    private readonly Button _next;
    private readonly Label _name;
    private bool _eligible;
    private bool _closed;

    private StarterRelicSelector(NCharacterSelectScreen screen)
    {
        _screen = screen;
        var parent = screen.GetNode<VBoxContainer>("InfoPanel/VBoxContainer");
        _row = new HBoxContainer
        {
            Name = "MaidenStarterRelicSelector", Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        _row.AddThemeConstantOverride("separation", 12);
        _previous = MakeArrow("‹", "MAIDEN_SUCCUBUS_STARTER_PREVIOUS");
        _next = MakeArrow("›", "MAIDEN_SUCCUBUS_STARTER_NEXT");
        _name = new Label
        {
            CustomMinimumSize = new Vector2(240, 40),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _name.AddThemeFontSizeOverride("font_size", 20);
        _name.AddThemeColorOverride("font_color", new Color("efdfb2"));
        Font font = screen.GetNode<MegaRichTextLabel>("InfoPanel/VBoxContainer/Relic/Name/RichTextLabel")
            .GetThemeFont("normal_font");
        _name.AddThemeFontOverride("font", font);
        _row.AddChild(_previous);
        _row.AddChild(_name);
        _row.AddChild(_next);
        parent.AddChild(_row);
        parent.MoveChild(_row, parent.GetNode<Control>("Relic").GetIndex() + 1);
        _previous.Pressed += ToggleSafely;
        _next.Pressed += ToggleSafely;
        _previous.FocusNeighborRight = _next.GetPath();
        _next.FocusNeighborLeft = _previous.GetPath();
    }

    private static Button MakeArrow(string text, string tooltip)
    {
        var button = new Button
        {
            Text = text, CustomMinimumSize = new Vector2(44, 40),
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = new LocString("characters", tooltip).GetFormattedText(),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        button.AddThemeFontSizeOverride("font_size", 28);
        var normal = new StyleBoxFlat
        {
            BgColor = new Color("26343bee"), BorderColor = new Color("9d8c60"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
        };
        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = new Color("475363");
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        return button;
    }

    internal static void Selected(NCharacterSelectScreen screen, NCharacterSelectButton button, CharacterModel character)
    {
        var selector = Instances.GetValue(screen, value => new(value));
        selector._closed = false;
        selector._eligible = character is MaidenSuccubusCharacter && !button.IsLocked && !button.IsRandom;
        selector.Refresh();
    }

    internal static void RefreshIfPresent(NCharacterSelectScreen screen)
    {
        if (Instances.TryGetValue(screen, out var selector)) selector.Refresh();
    }

    internal static void Close(NCharacterSelectScreen screen)
    {
        if (!Instances.TryGetValue(screen, out var selector)) return;
        selector._closed = true;
        selector._row.Hide();
        selector._previous.Disabled = selector._next.Disabled = true;
    }

    private void Refresh()
    {
        var lobby = _screen.Lobby;
        _row.Visible = !_closed && _eligible && lobby != null
            && lobby.LocalPlayer.character is MaidenSuccubusCharacter;
        if (!_row.Visible || lobby == null) return;
        _previous.Disabled = _next.Disabled = lobby.LocalPlayer.isReady;
        StarterRelicKind kind = StarterRelicChoice.Handle.Lobby.TryGet(lobby, lobby.LocalPlayer.id, out var data)
            ? StarterRelicChoice.Normalize(data.Kind) : StarterRelicKind.Omnipotent;
        RelicModel relic = StarterRelicSelection.Preview(kind);
        _name.Text = new LocString("characters", "MAIDEN_SUCCUBUS_STARTER_LABEL").GetFormattedText()
            + "：" + relic.Title.GetFormattedText();
        _screen.GetNode<MegaRichTextLabel>("InfoPanel/VBoxContainer/Relic/Name/RichTextLabel").Text = relic.Title.GetFormattedText();
        _screen.GetNode<MegaRichTextLabel>("InfoPanel/VBoxContainer/Relic/Description").Text = relic.DynamicDescription.GetFormattedText();
        _screen.GetNode<TextureRect>("InfoPanel/VBoxContainer/Relic/Icon").Texture = relic.Icon;
        _screen.GetNode<TextureRect>("InfoPanel/VBoxContainer/Relic/Icon/Outline").Texture = relic.IconOutline;
    }

    private void ToggleSafely() => Safe.Run(() =>
    {
        var lobby = _screen.Lobby;
        if (_closed || !_eligible || !_row.IsVisibleInTree() || lobby == null
            || lobby.LocalPlayer.isReady || lobby.LocalPlayer.character is not MaidenSuccubusCharacter) return;
        // Only the local player's bucket is written; RitsuLib synchronizes it
        // to the host and commits the authoritative snapshot before startup.
        StarterRelicChoice.Handle.Lobby.Modify(lobby, lobby.LocalPlayer.id,
            state => { state.Kind = StarterRelicChoice.Next(state.Kind); state.Applied = false; });
        Refresh();
    }, "StarterRelic.Toggle");
}
