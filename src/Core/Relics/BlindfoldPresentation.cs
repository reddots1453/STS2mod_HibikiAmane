using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Core.Relics;

/// <summary>Local presentation only: never changes encounter queues or monster moves.</summary>
internal static class BlindfoldPresentation
{
    internal static bool HasEffect(Player? player) =>
        player?.Character is MaidenSuccubusCharacter && LocalContext.IsMe(player)
        && player.Relics.OfType<Blindfold>().Any(relic => !relic.HasBeenRemovedFromState && !relic.IsMelted);

    internal static bool HidesIntents(Creature? creature) => creature is { IsMonster: true, IsEnemy: true, CombatState: { } combat }
        && HasEffect(combat.Players.FirstOrDefault(LocalContext.IsMe));

    internal static IEnumerable<IHoverTip> PowerTips(Creature creature)
    {
        var tips = new List<IHoverTip>();
        if (!CombatManager.Instance.IsInProgress) return tips;
        foreach (PowerModel power in creature.Powers)
            foreach (IHoverTip tip in power.HoverTips)
                tips.MegaTryAddingTip(tip);
        return tips;
    }

    internal static IEnumerable<IHoverTip> PreviewTips(Blindfold relic)
    {
        var tips = new List<IHoverTip>();
        Safe.Run(() =>
        {
            // Canonical encyclopedia/event previews and remote inventories disclose nothing.
            if (!relic.IsMutable || relic.HasBeenRemovedFromState || !HasEffect(relic.Owner)
                || !relic.Owner.Relics.Contains(relic) || relic.Owner.RunState is not RunState run)
                return;
            foreach (RoomType type in new[] { RoomType.Monster, RoomType.Elite, RoomType.Boss })
            {
                string key = "MAIDENSUCCUBUS_BLINDFOLD_" + type.ToString().ToUpperInvariant();
                LocString title = new("static_hover_tips", key + ".title");
                EncounterModel? encounter = Peek(run.Act, type);
                if (encounter == null)
                {
                    tips.Add(new HoverTip(title, new LocString("static_hover_tips",
                        "MAIDENSUCCUBUS_BLINDFOLD_UNAVAILABLE.description")));
                    continue;
                }
                LocString description = new("static_hover_tips", key + ".description");
                description.Add("Encounter", encounter.Title.GetFormattedText());
                // Actual random composition depends on the floor eventually selected.
                // Do not generate future entities or claim all possible enemies will spawn.
                description.Add("Monsters", string.Join("、", encounter.AllPossibleMonsters
                    .Select(monster => monster.Title.GetFormattedText()).Distinct()));
                tips.Add(new HoverTip(title, description));
            }
        }, nameof(PreviewTips));
        return tips;
    }

    internal static EncounterModel? Peek(ActModel act, RoomType type)
    {
        if (!act.IsMutable || type is not (RoomType.Monster or RoomType.Elite or RoomType.Boss)) return null;
        try
        {
            // v0.111.0 PullNextEncounter only reads RoomSet.Next*Encounter;
            // counters advance separately in MarkRoomVisited, never here.
            return act.PullNextEncounter(type);
        }
        catch (DivideByZeroException) { return null; } // Empty normal/elite queue.
        catch (ArgumentOutOfRangeException) { return null; } // Incomplete/invalid saved queue.
        catch (InvalidOperationException) { return null; } // Act has no generated boss yet.
    }

    internal static async Task Refresh(Player? owner)
    {
        if (!LocalContext.IsMe(owner) || owner?.Character is not MaidenSuccubusCharacter
            || owner.Creature.CombatState is not { } combat || NCombatRoom.Instance is not { } room)
            return;
        foreach (Creature enemy in combat.Enemies.Where(enemy => enemy.IsAlive).ToArray())
        {
            Task refresh = Task.CompletedTask;
            Safe.Run(() =>
            {
                var node = room.GetCreatureNode(enemy);
                if (node != null && GodotObject.IsInstanceValid(node))
                {
                    node.HideHoverTips();
                    refresh = node.RefreshIntents();
                }
            }, nameof(Refresh));
            try { await refresh; }
            catch (Exception ex) { MaidenSuccubusMod.Logger.Warn($"[BlindfoldRefresh] {ex.Message}"); }
        }
    }
}
