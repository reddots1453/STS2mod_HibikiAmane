using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Relics;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Acts;

internal static class FourthRouteRewardFlow
{
    private sealed class Gate { public bool Busy; }
    private static readonly ConditionalWeakTable<RunState, Gate> Gates = new();
    internal static FourthRouteRewardOffer? Pending(RunState run) =>
        FourthRouteProgressService.TryGetQuest(run, out var quest)
        && FourthRouteTrialRules.Pending(FourthRouteProgressService.Trial(run).Phase)
            ? new(quest, FourthRouteProgressService.Trial(run).Phase) : null;

    internal static bool Ready(Player player, RunState run, bool victoryBoundary = false) =>
        FourthRouteRewardOffer.CanPresent(FourthRouteLifecycle.IsEligible(player) && LocalContext.IsMe(player)
                && RunManager.Instance.NetService.Type != NetGameType.Replay,
            run.Players.Count == 1, TestMode.IsOn, !player.Creature.IsDead,
            ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), run) && NRun.Instance is { } node
                && GodotObject.IsInstanceValid(node) && node.IsInsideTree(),
            CombatManager.Instance.IsInProgress, NModalContainer.Instance?.OpenModal != null,
            NOverlayStack.Instance?.ScreenCount > 0 || NCapstoneContainer.Instance?.InUse == true
                || NGame.Instance?.InspectCardScreen?.Visible == true || NGame.Instance?.InspectRelicScreen?.Visible == true
                || NGame.Instance?.FeedbackScreen?.Visible == true
                || NRun.Instance?.TreasureRoom?.GetNodeOrNull<Control>("%RelicCollection")?.Visible == true,
            NGame.Instance?.Transition.InTransition == true,
            run.CurrentRoom is EventRoom { LocalMutableEvent: Neow } && FourthRouteOpeningService.NeedsOpening(run),
            RunManager.Instance.ActionExecutor.IsRunning || RunManager.Instance.ActionExecutor.IsPaused, victoryBoundary,
            EventSettled(run));

    private static bool EventSettled(RunState run)
    {
        if (run.CurrentRoom is not EventRoom room) return true;
        // Event handlers are fire-and-forget tasks, independent of ActionExecutor.
        // Never nest a trial reward while the event is still choosing/updating its result page.
        bool finished = room.LocalMutableEvent.IsFinished;
        bool optionsSettled = finished && RunManager.Instance.EventSynchronizer.AwaitPendingOptionTasks().IsCompletedSuccessfully;
        return FourthRouteRewardOffer.EventReady(true, finished, optionsSettled);
    }

    internal static async Task<bool> Show(Player player, bool victoryBoundary = false)
    {
        if (player.RunState is not RunState run || Pending(run) is not { } offer || !Ready(player, run, victoryBoundary)) return false;
        var gate = Gates.GetValue(run, _ => new Gate());
        if (gate.Busy) return false;
        gate.Busy = true;
        var scene = NRun.Instance;
        var room = run.CurrentRoom;
        bool Current() => GodotObject.IsInstanceValid(scene) && scene!.IsInsideTree()
            && ReferenceEquals(NRun.Instance, scene) && ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), run)
            && !player.Creature.IsDead && ReferenceEquals(run.CurrentRoom, room) && Pending(run) == offer;
        try
        {
            return await FourthRouteRewardScreen.Show(player, offer, Current);
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[FourthRouteReward] Presentation/claim failed; saved trial state retained: " + ex);
            return false;
        }
        finally
        {
            gate.Busy = false;
        }
    }

    internal static async Task<bool> Claim(Player player, FourthRouteRewardOffer offer)
    {
        if (!FourthRouteLifecycle.IsEligible(player) || player.RunState is not RunState run
            || !FourthRouteProgressService.TryGetQuest(run, out var quest)
            || !offer.Matches(quest, FourthRouteProgressService.Trial(run).Phase)) return false;
        await FourthRouteProgressService.ClaimInitialReward(player);
        return !offer.Matches(quest, FourthRouteProgressService.Trial(run).Phase);
    }
}
