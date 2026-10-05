using System.Runtime.CompilerServices;
using Godot;
using MaidenSuccubus.Core.Replay;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.UI;

/// <summary>
/// Tint the native textured frame and portrait, preserving their transparent
/// silhouettes, shaders and readable text. No opaque rectangular overlay.
/// Native card pooling must restore the previous node colors before reuse.
/// </summary>
public static class BattleReplayCardVisuals
{
    private readonly record struct Tint(Color Original, Color Applied);
    private sealed class State
    {
        public Dictionary<CanvasItem, Tint> Nodes { get; } = [];
    }
    private static readonly ConditionalWeakTable<NCard, State> States = new();
    private static readonly string[] Surfaces =
        ["%Portrait", "%AncientPortrait", "%Frame", "%PortraitBorder", "%AncientBorder"];

    public static void Apply(NCard card)
    {
        if (!card.IsNodeReady()) return;
        bool active = card.Model != null
            && ModelCapabilities.TryGet(card.Model, out ModelCapabilitySet? capabilities)
            && capabilities.Get<BattleReplayOriginCapability>() != null;
        if (!active)
        {
            Restore(card);
            return;
        }
        State state = States.GetOrCreateValue(card);
        foreach (string path in Surfaces)
        {
            CanvasItem? node = card.GetNodeOrNull<CanvasItem>(path);
            if (node == null) continue;
            if (state.Nodes.TryGetValue(node, out Tint previous)
                && node.SelfModulate == previous.Applied) continue;
            // Other extensions may have changed a color since the last refresh.
            // Preserve their new base rather than multiplying our tint repeatedly.
            Color original = node.SelfModulate;
            Color tone = path is "%Portrait" or "%AncientPortrait"
                ? new Color(0.63f, 0.66f, 0.77f, 1f)
                : new Color(0.68f, 0.70f, 0.79f, 1f);
            Color applied = original * tone;
            state.Nodes[node] = new Tint(original, applied);
            node.SelfModulate = applied;
        }
    }

    public static void Restore(NCard card)
    {
        if (!States.TryGetValue(card, out State? state)) return;
        foreach (var (node, tint) in state.Nodes)
        {
            if (GodotObject.IsInstanceValid(node) && node.SelfModulate == tint.Applied)
                node.SelfModulate = tint.Original;
        }
        state.Nodes.Clear();
        States.Remove(card);
    }
}
