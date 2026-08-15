using MegaCrit.Sts2.Core.Entities.Cards;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Core.Control;

public sealed record EscapeResolution(
    ControlPower Control,
    int EscapeAmount);

public static class EscapeProjectionTracker
{
    private static readonly Dictionary<CardPlay, EscapeResolution> Pending =
        new(ReferenceEqualityComparer.Instance);

    public static void Begin(CardPlay cardPlay, ControlPower control)
    {
        int amount = Math.Max(0, cardPlay.Resources.EnergyValue);
        Pending[cardPlay] = new EscapeResolution(control, amount);
    }

    public static bool TryTake(
        CardPlay cardPlay,
        ControlPower control,
        out int amount)
    {
        amount = 0;
        if (!Pending.TryGetValue(cardPlay, out EscapeResolution? resolution)
            || !ReferenceEquals(resolution.Control, control))
        {
            return false;
        }

        Pending.Remove(cardPlay);
        amount = resolution.EscapeAmount;
        return true;
    }
}
