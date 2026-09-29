using System.Collections.Generic;

namespace MaidenSuccubus.Acts;

/// <summary>Pure rules shared by the live map gate and the offline regression check.</summary>
public static class ActAlignmentChoiceRules
{
    public static bool Needs(int actIndex, IReadOnlySet<int> resolvedActs) =>
        actIndex is 1 or 2 && !resolvedActs.Contains(actIndex);

    public static bool ValidDelta(int delta) => delta is 2 or -2;
}
