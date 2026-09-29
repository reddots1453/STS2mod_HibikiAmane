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
    private readonly NButton _previous;
    private readonly NButton _next;
    private bool _eligible;
    private bool _closed;

    private StarterRelicSelector(NCharacterSelectScreen screen)
    {
        _screen = screen;
        _relicPanel = screen.GetNode<Control>("InfoPanel/VBoxContainer/Relic");
        NAscensionPanel ascension = screen.GetNode<NAscensionPanel>("%AscensionPanel");
        _previous = MakeArrow(ascension.GetNode<NButton>("HBoxContainer/LeftArrowContainer/LeftArrow"));
        _next = MakeArrow(ascension.GetNode<NButton>("HBoxContainer/RightArrowContainer/RightArrow"));
        screen.AddChild(_previous);
        screen.AddChild(_next);
        _relicPanel.Resized += PlaceArrows;
        _previous.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => ToggleSafely()));
        _next.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => ToggleSafely()));
        _previous.FocusNeighborRight = _next.GetPath();
        _next.FocusNeighborLeft = _previous.GetPath();
    }

    private static NButton MakeArrow(NButton source)
    {
        // Clone the complete native control, including hover/press visuals.
        // Exclude its existing Released connection, which changes ascension.
        var flags = Node.DuplicateFlags.UseInstantiation | Node.DuplicateFlags.Scripts |
            Node.DuplicateFlags.Groups;
        var button = (NButton)source.Duplicate((int)flags);
        button.Name = "MaidenStarterRelicArrow";
        button.TooltipText = string.Empty;
        button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        button.CustomMinimumSize = source.Size;
        button.Size = source.Size;
        button.Visible = false;
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
        selector._previous.SetEnabled(false);
        selector._next.SetEnabled(false);
    }

    private void Refresh()
    {
        var lobby = _screen.Lobby;
        bool visible = !_closed && _eligible && lobby != null
            && lobby.LocalPlayer.character is MaidenSuccubusCharacter;
        _previous.Visible = _next.Visible = visible;
        if (!visible || lobby == null) return;
        PlaceArrows();
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
