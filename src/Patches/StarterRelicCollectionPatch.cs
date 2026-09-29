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
    private static void AfterLoad(NRelicCollectionCategory __instance, RelicRarity __0,
        NRelicCollection __1, HashSet<RelicModel> __3)
    {
        if (__0 != RelicRarity.Starter) return;
        Safe.Run(() =>
        {
            var groups = __instance.GetChildren().OfType<NRelicCollectionCategory>()
                .Where(group => !group.IsQueuedForDeletion()).ToArray();
            if (groups.Length < 2) return;
            Add(groups[0], __1, ModelDb.Relic<HeroOrb>(), __3);
            Add(groups[1], __1, ModelDb.Relic<EternalOrb>(), __3);
        }, nameof(AfterLoad));
    }

    private static void Add(NRelicCollectionCategory category, NRelicCollection collection,
        RelicModel relic, HashSet<RelicModel> seen)
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
