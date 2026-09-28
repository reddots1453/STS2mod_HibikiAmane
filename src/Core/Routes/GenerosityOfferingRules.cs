using MaidenSuccubus.Acts;

namespace MaidenSuccubus.Core.Routes;

internal static class GenerosityOfferingRules
{
    internal static bool CanOffer(bool maiden, bool selected, FourthTrialPhase phase, int relicStage, int removableCards) =>
        maiden && selected && (FourthRouteTrialRules.Active(phase)
            || phase == FourthTrialPhase.Complete && relicStage >= 3 && removableCards > 0);

    // Ordinary reward indexes stay untouched. Each group owns two stable child slots.
    internal const int ChildIndexBase = 1_000_000;
    internal static int Encode(int parent, int child)
    {
        if (parent is < 0 or >= ChildIndexBase || child is < 0 or > 1)
            throw new ArgumentOutOfRangeException();
        return ChildIndexBase + parent * 2 + child;
    }
    internal static bool TryDecode(int encoded, int count, out int parent, out int child)
    {
        parent = (encoded - ChildIndexBase) / 2;
        child = (encoded - ChildIndexBase) % 2;
        return encoded >= ChildIndexBase && parent >= 0 && parent < ChildIndexBase && parent < count;
    }
}
