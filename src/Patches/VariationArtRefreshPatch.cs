using Godot;
using HarmonyLib;
using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// Native relic nodes only reload when assigned a model. Keep weak references
/// to initialized variant views, and redraw when their own run changes route.
/// No polling, model replacement, reward hooks or saved visual state is needed.
/// </summary>
internal static class VariationArtRefresh
{
    private static readonly List<WeakReference<NCard>> Cards = [];
    private static readonly List<WeakReference<NRelic>> Relics = [];
    private static readonly MethodInfo? IconChanged =
        AccessTools.DeclaredMethod(typeof(RelicModel), "RelicIconChanged");
    private static bool _subscribed;

    internal static bool HasVariationArt(RelicModel? model) => model is
        HeroOrb or TwinSoulChalice or PrayerEarrings or InternalCondom
        or CounterCurseMirror or ForgottenSoul;

    internal static void Invalidate(RelicModel model)
    {
        // RelicModel caches ResolvedBigIconPath even if AssetProfile is dynamic.
        // A cloned prototype can inherit that path before it receives an owner.
        IconChanged?.Invoke(model, null);
    }

    internal static void Track(NRelic node)
    {
        if (!node.IsNodeReady() || !HasVariationArt(node.Model)) return;
        Subscribe();
        AddOnce(Relics, node);
    }

    internal static void Track(NCard node)
    {
        if (!node.IsNodeReady() || node.Model is not (Transform or DarkElementBase)) return;
        Subscribe();
        AddOnce(Cards, node);
    }

    private static void AddOnce<T>(List<WeakReference<T>> views, T node) where T : Node
    {
        // Prune freed views on registration as well as corruption changes, so
        // a run that keeps the same value does not accumulate preview entries.
        views.RemoveAll(view => !view.TryGetTarget(out T? target)
            || !GodotObject.IsInstanceValid(target));
        if (!views.Any(view => view.TryGetTarget(out T? target) && ReferenceEquals(target, node)))
            views.Add(new WeakReference<T>(node));
    }

    private static void Subscribe()
    {
        if (_subscribed) return;
        _subscribed = true;
        CorruptionEvents.Changed += OnChanged;
    }

    private static void OnChanged(CorruptionChanged change)
    {
        if (change.Delta == 0) return;
        foreach (var player in change.RunState.Players)
        {
            if (player.Character is not MaidenSuccubusCharacter) continue;
            foreach (RelicModel relic in player.Relics.Where(HasVariationArt))
                Safe.Run(() => Invalidate(relic), nameof(VariationArtRefresh));
        }

        // Reload can register/prune views. Iterate a snapshot instead of a list
        // whose indices could change while a native reload postfix is running.
        foreach (WeakReference<NRelic> view in Relics.ToArray())
        {
            if (!view.TryGetTarget(out NRelic? node) || !GodotObject.IsInstanceValid(node))
            {
                Relics.Remove(view);
                continue;
            }
            if (!node.IsInsideTree() || !HasVariationArt(node.Model)) continue;
            RelicModel model = node.Model;
            if (!model.IsMutable || !ReferenceEquals(model.Owner?.RunState, change.RunState)) continue;
            Safe.Run(() => node.Model = model, nameof(VariationArtRefresh));
        }
        foreach (WeakReference<NCard> view in Cards.ToArray())
        {
            if (!view.TryGetTarget(out NCard? node) || !GodotObject.IsInstanceValid(node))
            {
                Cards.Remove(view);
                continue;
            }
            if (!node.IsInsideTree() || node.Model is not (Transform or DarkElementBase)) continue;
            CardModel model = node.Model;
            if (!model.IsMutable || !ReferenceEquals(model.Owner?.RunState, change.RunState)) continue;
            Safe.Run(() => MaidenCardPortraitPresentationPatch.Apply(node), nameof(VariationArtRefresh));
        }
    }
}

[HarmonyPatch(typeof(NRelic), "Reload")]
internal static class VariationRelicReloadPatch
{
    private static void Prefix(RelicModel? ____model)
    {
        if (VariationArtRefresh.HasVariationArt(____model))
            Safe.Run(() => VariationArtRefresh.Invalidate(____model!), nameof(VariationRelicReloadPatch));
    }

    private static void Postfix(NRelic __instance) => Safe.Run(
        () => VariationArtRefresh.Track(__instance), nameof(VariationRelicReloadPatch));
}

[HarmonyPatch(typeof(NCard), "Reload")]
internal static class VariationCardReloadPatch
{
    private static void Postfix(NCard __instance) => Safe.Run(
        () => VariationArtRefresh.Track(__instance), nameof(VariationCardReloadPatch));
}
