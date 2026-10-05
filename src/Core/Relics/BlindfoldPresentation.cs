using Godot;
using HarmonyLib;
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

    internal static HoverTip EncounterTip(ActModel act, RoomType type)
    {
        string key = "MAIDENSUCCUBUS_BLINDFOLD_" + type.ToString().ToUpperInvariant();
        LocString title = new("static_hover_tips", key + ".title");
        EncounterModel? encounter = Peek(act, type);
        if (encounter == null)
            return new HoverTip(title, new LocString("static_hover_tips",
                "MAIDENSUCCUBUS_BLINDFOLD_UNAVAILABLE.description"));
        LocString description = new("static_hover_tips", key + ".description");
        description.Add("Encounter", encounter.Title.GetFormattedText());
        description.Add("Monsters", string.Join("、", encounter.AllPossibleMonsters
            .Select(monster => monster.Title.GetFormattedText()).Distinct()));
        return new HoverTip(title, description);
    }

    internal static EncounterModel? Peek(ActModel act, RoomType type)
    {
        if (!act.IsMutable || type is not (RoomType.Monster or RoomType.Elite or RoomType.Boss)) return null;
        EncounterModel? next = null;
        Safe.Run(() =>
        {
            // Adapted from local sts2_foresight/EncounterReader. No dequeue, generation or RNG.
            object? rooms = Traverse.Create(act).Field("_rooms").GetValue();
            if (rooms == null) return;
            if (type == RoomType.Boss)
            {
                // BossReader reads Boss; the next-encounter view also respects Double Boss.
                // Avoid an unset Boss getter before this act's rooms have been generated.
                if (Traverse.Create(rooms).Field("_boss").GetValue<EncounterModel>() == null) return;
                next = Traverse.Create(rooms).Property("NextBossEncounter").GetValue<EncounterModel>();
                return;
            }
            bool elite = type == RoomType.Elite;
            var list = Traverse.Create(rooms).Field(elite ? "eliteEncounters" : "normalEncounters")
                .GetValue<List<EncounterModel>>();
            if (list == null || list.Count == 0) return;
            int visited = Traverse.Create(rooms).Field(elite ? "eliteEncountersVisited" : "normalEncountersVisited")
                .GetValue<int>();
            if (visited >= 0) next = list[visited % list.Count];
        }, "BlindfoldEncounterPeek");
        return next;
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
