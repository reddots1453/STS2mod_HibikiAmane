using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.Acts;

/// <summary>All peers select the same room; only entitled players receive free prices.</summary>
internal static class GreedShopService
{
    private sealed record Selection(string Location, GreedRouteRelic[] Relics);
    private static readonly ConditionalWeakTable<RunState, Selection> Selections = new();

    internal static string Location(IRunState run) => $"{run.CurrentActIndex}:{run.ActFloor}";

    internal static bool Select(RunState run, MapPointType pointType, bool reserved)
    {
        Selections.Remove(run);
        if (pointType != MapPointType.Unknown || reserved) return false;
        string location = Location(run);
        GreedRouteRelic[] candidates = run.Players.Where(player => player.IsActiveForHooks && FourthRouteLifecycle.IsEligible(player))
            .SelectMany(player => player.Relics.OfType<GreedRouteRelic>())
            .Where(relic => relic.CanReplaceUnknown(location)).ToArray();
        if (candidates.Length == 0) return false;
        Selections.Add(run, new Selection(location, candidates));
        return true;
    }

    internal static void Cancel(RunState run) => Selections.Remove(run);

    internal static void Created(RunState run, MapPointType pointType, AbstractRoom room)
    {
        if (!Selections.TryGetValue(run, out Selection? selection)) return;
        Selections.Remove(run);
        if (pointType != MapPointType.Unknown || room is not MerchantRoom merchant
            || selection.Location != Location(run)) return;
        foreach (GreedRouteRelic relic in selection.Relics)
            if (relic.Owner.Relics.Contains(relic) && relic.CanReplaceUnknown(selection.Location))
                relic.BindFreeShop(selection.Location, merchant);
    }
}
