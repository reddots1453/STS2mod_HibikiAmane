using System.Globalization;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Corruption;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Presentation;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.UI;

/// <summary>Presentation-only entry point. Empty templates deliberately do nothing.</summary>
internal static class CombatTextFeedback
{
    private static CanvasLayer? _layer;
    private static CombatTextOverlay? _overlay;
    private static NCombatRoom? _room;
    private static bool _initialized;
    private static FeedbackTemplates _templates = new();

    internal static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        DesireEvents.Changed += change =>
        {
            if (change.NewValue <= change.OldValue) return;
            Notify("desire_increased", change.Player.Creature,
                amount: change.NewValue - change.OldValue, oldValue: change.OldValue,
                newValue: change.NewValue);
            if (change.OldValue < 8 && change.NewValue >= 8)
                Notify("desire_reached_8", change.Player.Creature,
                    oldValue: change.OldValue, newValue: change.NewValue);
            if (change.OldValue < Data.Desire.Max && change.NewValue >= Data.Desire.Max)
                Notify("desire_reached_max", change.Player.Creature,
                    oldValue: change.OldValue, newValue: change.NewValue);
        };
        CombatManager.Instance.CombatWon += _ => Clear();
        CombatManager.Instance.CombatEnded += _ => Clear();
    }

    internal static void Notify(string key, Creature target, Creature? source = null,
        int amount = 0, int oldValue = 0, int newValue = 0,
        string controlType = "", string card = "",
        ControlType? bindingType = null) => Safe.Run(() =>
    {
        if (!PerformanceAudience.IsLocalMaiden(target.Player)
            || target.CombatState == null || !CombatManager.Instance.IsInProgress
            || CombatManager.Instance.IsEnding) return;
        NCombatRoom? room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree()) return;
        if (_room != room)
        {
            Clear();
            _room = room;
            // Read once per combat; edits are picked up by the next combat.
            _templates = FeedbackTemplates.Load();
        }
        int corruption = target.Player!.RunState is RunState run ? CorruptionQuery.Get(run) : 0;
        string? template = _templates.Next(key, CorruptionQuery.GetBand(corruption), bindingType);
        if (string.IsNullOrWhiteSpace(template)) return;
        string text = template.Replace("{enemy}", source?.Name ?? "", StringComparison.Ordinal)
            .Replace("{player}", target.Name, StringComparison.Ordinal)
            .Replace("{amount}", amount.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{old}", oldValue.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{new}", newValue.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{control}", controlType, StringComparison.Ordinal)
            .Replace("{card}", card, StringComparison.Ordinal)
            .Replace("{corruption}", corruption.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{control_kind}", bindingType?.ToString().ToLowerInvariant() ?? "", StringComparison.Ordinal)
            .Replace("{control_name}", bindingType switch
            {
                ControlType.Attack => "乳虐",
                ControlType.Skill => "口虐",
                ControlType.Power => "穴虐",
                _ => "",
            }, StringComparison.Ordinal);
        if (_overlay == null || !GodotObject.IsInstanceValid(_overlay))
        {
            _layer = new CanvasLayer { Name = "MaidenCombatText", Layer = 125 };
            _overlay = new CombatTextOverlay { MouseFilter = Control.MouseFilterEnum.Ignore };
            _layer.AddChild(_overlay);
            room.AddChild(_layer);
            _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
        _overlay.ShowMessage(text, key == "damage_received"
            ? new Color("#ffb39f") : new Color("#f1d3ee"));
    }, "CombatTextFeedback." + key);

    internal static void Clear() => Safe.Run(() =>
    {
        if (_layer != null && GodotObject.IsInstanceValid(_layer))
        {
            _overlay?.Hide();
            _layer.QueueFree();
        }
        _layer = null;
        _overlay = null;
        _room = null;
    }, "CombatTextFeedback.Clear");
}

internal partial class CombatTextOverlay : Control
{
    private const float Lifetime = 2.4f;
    private readonly List<Message> _messages = [];

    internal void ShowMessage(string text, Color color)
    {
        // Four distinct lanes; retain every hit's own number without overlapping labels.
        if (_messages.Count >= 4)
        {
            _messages[0].Label.QueueFree();
            _messages.RemoveAt(0);
        }
        var label = new Label
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AnchorLeft = 0.5f, AnchorRight = 0.5f,
            OffsetLeft = -700f, OffsetRight = 700f,
            AutowrapMode = TextServer.AutowrapMode.Off,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            Modulate = new Color(1, 1, 1, 0),
        };
        label.AddThemeFontSizeOverride("font_size", 30);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color("#271620"));
        label.AddThemeConstantOverride("outline_size", 6);
        AddChild(label);
        _messages.Add(new Message(label));
        UpdatePositions(0f);
    }

    public override void _Process(double delta)
    {
        if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding)
        {
            CombatTextFeedback.Clear();
            return;
        }
        UpdatePositions((float)delta);
    }

    private void UpdatePositions(float delta)
    {
        for (int i = _messages.Count - 1; i >= 0; i--)
        {
            Message message = _messages[i];
            message.Age += delta;
            if (message.Age < Lifetime) continue;
            message.Label.QueueFree();
            _messages.RemoveAt(i);
        }
        for (int i = 0; i < _messages.Count; i++)
        {
            Message message = _messages[i];
            message.Label.OffsetTop = 174f + i * 48f - message.Age * 14f;
            message.Label.OffsetBottom = message.Label.OffsetTop + 44f;
            float alpha = Math.Min(Math.Clamp(message.Age / 0.12f, 0, 1),
                Math.Clamp((Lifetime - message.Age) / 0.5f, 0, 1));
            message.Label.Modulate = new Color(1, 1, 1, alpha);
        }
    }

    private sealed class Message(Label label)
    {
        internal Label Label { get; } = label;
        internal float Age;
    }
}
