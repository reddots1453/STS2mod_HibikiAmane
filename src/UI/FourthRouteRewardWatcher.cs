using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.UI;

/// <summary>Out-of-combat completions wait for an idle scene, never nest inside a card selector.</summary>
public sealed partial class FourthRouteRewardWatcher : Node
{
    private double _elapsed;
    private bool _busy;
    private FourthRouteRewardOffer? _failedOffer;
    private AbstractRoom? _failedRoom;
    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_elapsed < .15 || _busy) return;
        _elapsed = 0;
        Safe.Run(() =>
        {
            if (RunManager.Instance.DebugOnlyGetState() is not { } run
                || LocalContext.GetMe(run) is not { } player || FourthRouteRewardFlow.Pending(run) is not { } offer
                || (_failedOffer == offer && ReferenceEquals(_failedRoom, run.CurrentRoom))
                || !FourthRouteRewardFlow.Ready(player, run)) return;
            _busy = true;
            TaskHelper.RunSafely(Show(player, run, offer));
        }, "FourthRoute.RewardIdleCheck");
    }
    private async Task Show(MegaCrit.Sts2.Core.Entities.Players.Player player, RunState run, FourthRouteRewardOffer offer)
    {
        var room = run.CurrentRoom;
        try
        {
            if (!await FourthRouteRewardFlow.Show(player)) { _failedOffer = offer; _failedRoom = room; }
        }
        finally { _busy = false; }
    }
}
