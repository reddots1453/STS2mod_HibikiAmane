using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
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
    private readonly Control _relicPanel;
    private readonly Button _previous;
    private readonly Button _next;
    private bool _eligible;
    private bool _closed;

    private StarterRelicSelector(NCharacterSelectScreen screen)
    {
        _screen = screen;
        _relicPanel = screen.GetNode<Control>("InfoPanel/VBoxContainer/Relic");
        NAscensionPanel ascension = screen.GetNode<NAscensionPanel>("%AscensionPanel");
        _previous = MakeArrow(ascension.GetNode<NButton>("HBoxContainer/LeftArrowContainer/LeftArrow"),
            "MAIDEN_SUCCUBUS_STARTER_PREVIOUS");
        _next = MakeArrow(ascension.GetNode<NButton>("HBoxContainer/RightArrowContainer/RightArrow"),
            "MAIDEN_SUCCUBUS_STARTER_NEXT");
        screen.AddChild(_previous);
        screen.AddChild(_next);
        _relicPanel.Resized += PlaceArrows;
        _previous.Pressed += ToggleSafely;
        _next.Pressed += ToggleSafely;
        _previous.FocusNeighborRight = _next.GetPath();
        _next.FocusNeighborLeft = _previous.GetPath();
    }

    private static Button MakeArrow(NButton source, string tooltip)
    {
        // Borrow the actual ascension-arrow artwork; the original NButton keeps
        // its own signals and is not moved or duplicated.
        Texture2D? icon = source.FindChildren("*", "TextureRect", true, false)
            .OfType<TextureRect>().Select(node => node.Texture).FirstOrDefault(texture => texture != null);
        var button = new Button
        {
            Icon = icon, CustomMinimumSize = new Vector2(44, 40),
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = new LocString("characters", tooltip).GetFormattedText(),
            MouseFilter = Control.MouseFilterEnum.Stop,
            Visible = false,
        };
        var transparent = new StyleBoxEmpty();
        button.AddThemeStyleboxOverride("normal", transparent);
        button.AddThemeStyleboxOverride("hover", transparent);
        button.AddThemeStyleboxOverride("pressed", transparent);
        return button;
    }

    private void PlaceArrows()
    {
        Rect2 bounds = _relicPanel.GetGlobalRect();
        _previous.GlobalPosition = new Vector2(bounds.Position.X - _previous.Size.X - 8f,
            bounds.GetCenter().Y - _previous.Size.Y / 2f);
        _next.GlobalPosition = new Vector2(bounds.End.X + 8f,
            bounds.GetCenter().Y - _next.Size.Y / 2f);
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
        selector._previous.Hide();
        selector._next.Hide();
        selector._previous.Disabled = selector._next.Disabled = true;
    }

    private void Refresh()
    {
        var lobby = _screen.Lobby;
        bool visible = !_closed && _eligible && lobby != null
            && lobby.LocalPlayer.character is MaidenSuccubusCharacter;
        _previous.Visible = _next.Visible = visible;
        if (!visible || lobby == null) return;
        PlaceArrows();
        _previous.Disabled = _next.Disabled = lobby.LocalPlayer.isReady;
        StarterRelicKind kind = StarterRelicChoice.Handle.Lobby.TryGet(lobby, lobby.LocalPlayer.id, out var data)
            ? StarterRelicChoice.Normalize(data.Kind) : StarterRelicKind.Omnipotent;
        RelicModel relic = StarterRelicSelection.Preview(kind);
        _screen.GetNode<MegaRichTextLabel>("InfoPanel/VBoxContainer/Relic/Name/RichTextLabel").Text = relic.Title.GetFormattedText();
        _screen.GetNode<MegaRichTextLabel>("InfoPanel/VBoxContainer/Relic/Description").Text = relic.DynamicDescription.GetFormattedText();
        _screen.GetNode<TextureRect>("InfoPanel/VBoxContainer/Relic/Icon").Texture = relic.Icon;
        _screen.GetNode<TextureRect>("InfoPanel/VBoxContainer/Relic/Icon/Outline").Texture = relic.IconOutline;
    }

    private void ToggleSafely() => Safe.Run(() =>
    {
        var lobby = _screen.Lobby;
        if (_closed || !_eligible || !_previous.IsVisibleInTree() || lobby == null
            || lobby.LocalPlayer.isReady || lobby.LocalPlayer.character is not MaidenSuccubusCharacter) return;
        // Only the local player's bucket is written; RitsuLib synchronizes it
        // to the host and commits the authoritative snapshot before startup.
        StarterRelicChoice.Handle.Lobby.Modify(lobby, lobby.LocalPlayer.id,
            state => { state.Kind = StarterRelicChoice.Next(state.Kind); state.Applied = false; });
        Refresh();
    }, "StarterRelic.Toggle");
}
