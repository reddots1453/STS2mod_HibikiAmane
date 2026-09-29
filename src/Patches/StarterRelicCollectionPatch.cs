using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// The vanilla starter collection enumerates CharacterModel.StartingRelics,
/// which can contain only the default orb without granting both at run start.
/// Add the alternate orb to its existing starter/upgraded categories instead.
/// </summary>
[HarmonyPatch(typeof(NRelicCollectionCategory), nameof(NRelicCollectionCategory.LoadRelics))]
internal static class StarterRelicCollectionPatch
{
    [HarmonyPostfix]
    private static void AfterLoad(NRelicCollectionCategory __instance, RelicRarity relicRarity,
        NRelicCollection collection, HashSet<RelicModel> seenRelics,
        HashSet<RelicModel> allUnlockedRelics)
    {
        if (relicRarity != RelicRarity.Starter) return;
        Safe.Run(() =>
        {
            var groups = __instance.GetChildren().OfType<NRelicCollectionCategory>()
                .Where(group => !group.IsQueuedForDeletion()).ToArray();
            if (groups.Length < 2) return;
            Add(groups[0], collection, ModelDb.Relic<HeroOrb>(), seenRelics, allUnlockedRelics);
            Add(groups[1], collection, ModelDb.Relic<EternalOrb>(), seenRelics, allUnlockedRelics);
        }, nameof(AfterLoad));
    }

    private static void Add(NRelicCollectionCategory category, NRelicCollection collection,
        RelicModel relic, HashSet<RelicModel> seen, HashSet<RelicModel> unlocked)
    {
        GridContainer grid = category.GetNode<GridContainer>("%RelicsContainer");
        if (grid.GetChildren().OfType<NRelicCollectionEntry>().Any(entry => entry.relic.Id == relic.Id)) return;
        // Both selectable starter orbs are always unlocked for this character.
        ModelVisibility visibility = seen.Contains(relic)
            ? ModelVisibility.Visible : ModelVisibility.NotSeen;
        NRelicCollectionEntry entry = NRelicCollectionEntry.Create(relic, visibility);
        grid.AddChild(entry);
        entry.Connect(NClickableControl.SignalName.Released,
            Callable.From(() =>
            {
                MegaCrit.Sts2.Core.Nodes.NGame.Instance?.GetInspectRelicScreen()
                    .Open(collection.Relics, relic);
                collection.SetLastFocusedRelic(entry);
            }));
        collection.AddRelics([relic]);
    }
}
