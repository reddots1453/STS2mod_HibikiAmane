using System.Runtime.CompilerServices;
using Godot;
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
    private readonly Control _relicIcon;
    private readonly NButton _previous;
    private readonly NButton _next;
    private readonly StartRouteSelector _routes;
    private bool _eligible;
    private bool _closed;

    private StarterRelicSelector(NCharacterSelectScreen screen)
    {
        _screen = screen;
        _relicPanel = screen.GetNode<Control>("InfoPanel/VBoxContainer/Relic");
        _relicIcon = screen.GetNode<Control>("InfoPanel/VBoxContainer/Relic/Icon");
        NAscensionPanel ascension = screen.GetNode<NAscensionPanel>("%AscensionPanel");
        var layout = new StarterRelicArrowLayout(_relicPanel, _relicIcon,
            ascension.GetNode<NButton>("HBoxContainer/LeftArrowContainer/LeftArrow"),
            ascension.GetNode<NButton>("HBoxContainer/RightArrowContainer/RightArrow"));
        _previous = layout.Previous;
        _next = layout.Next;
        _previous.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => ToggleSafely()));
        _next.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => ToggleSafely()));
        _previous.FocusNeighborRight = _next.GetPath();
        _next.FocusNeighborLeft = _previous.GetPath();
        _routes = new StartRouteSelector(screen);
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
        selector._routes.Close();
        selector._previous.Hide();
        selector._next.Hide();
        selector._previous.SetEnabled(false);
        selector._next.SetEnabled(false);
    }

    private void Refresh()
    {
        var lobby = _screen.Lobby;
        bool visible = !_closed && _eligible && lobby != null
            && lobby.LocalPlayer.character is MaidenSuccubusCharacter;
        _previous.Visible = _next.Visible = visible;
        _routes.Refresh(visible);
        if (!visible || lobby == null) return;
        _previous.SetEnabled(!lobby.LocalPlayer.isReady);
        _next.SetEnabled(!lobby.LocalPlayer.isReady);
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
