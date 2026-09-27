using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Data;
using System.Runtime.CompilerServices;

namespace MaidenSuccubus.Acts;

internal static class FourthRouteOpeningService
{
    private sealed class ConfirmGate { public bool Busy; }
    private static readonly ConditionalWeakTable<RunState, ConfirmGate> Gates = new();
    internal static bool NeedsOpening(RunState run) => FourthRouteOpeningState.NeedsOpening(
        FourthRouteProgressService.TryGetQuest(run, out _), M5Progress.Handle.Get(run).FourthRouteOpening);

    internal static FourthRouteOpeningState Prepare(RunState run)
    {
        M5Progress.Handle.Modify(run, data =>
        {
            data.FourthRouteOpening ??= new();
            var opening = data.FourthRouteOpening;
            if (opening.HasOffers || opening.Chosen != null || opening.Completed) return;
            opening.Offer(FourthRouteProgressService.DarkQuests[run.Rng.Niche.NextInt(7)],
                FourthRouteProgressService.LightQuests[run.Rng.Niche.NextInt(7)]);
        });
        return M5Progress.Handle.Get(run).FourthRouteOpening!;
    }

    internal static async Task<bool> Confirm(Player player, FourthRouteQuest quest)
    {
        if (!FourthRouteLifecycle.IsEligible(player) || player.RunState is not RunState run) return false;
        var gate = Gates.GetValue(run, _ => new ConfirmGate());
        if (gate.Busy) return false;
        gate.Busy = true;
        try
        {
            bool accepted = false;
            M5Progress.Handle.Modify(run, data =>
            {
                var opening = data.FourthRouteOpening;
                if (FourthRouteProgressService.TryGetQuest(run, out var existing))
                {
                    accepted = existing == quest && opening?.Chosen == quest && !opening.Completed;
                    return;
                }
                if (opening?.Choose(quest) != true) return;
                // Lock the route before any asynchronous relic animation or repeated input.
                data.FourthRouteQuestId = quest.ToString();
                data.FourthRouteAlignment = FourthRouteProgressService.AlignmentOf(quest).ToString();
                data.FourthRouteQuestProgress = 0;
                data.FourthRouteTrial = new();
                accepted = true;
            });
            if (accepted) await FourthRouteProgressService.EnsureDormantRelic(player);
            return accepted;
        }
        finally { gate.Busy = false; }
    }

    internal static bool Finish(RunState run)
    {
        bool finished = false;
        M5Progress.Handle.Modify(run, data =>
        {
            if (FourthRouteProgressService.TryGetQuest(run, out var quest)
                && data.FourthRouteOpening?.Chosen == quest)
                finished = data.FourthRouteOpening.Finish();
        });
        return finished;
    }
}
