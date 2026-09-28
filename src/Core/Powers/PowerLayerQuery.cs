using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace MaidenSuccubus.Core.Powers;

/// <summary>
/// Counts gameplay-visible power layers by Amount, rather than counting power
/// model instances. Hidden cleanup models are intentionally excluded.
/// </summary>
public static class PowerLayerQuery
{
    public static int CountBuffLayers(Creature creature) =>
        Count(creature, PowerType.Buff);

    public static int CountDebuffLayers(Creature creature) =>
        Count(creature, PowerType.Debuff);

    private static int Count(Creature creature, PowerType type)
    {
        ArgumentNullException.ThrowIfNull(creature);
        // Native TypeForCurrentAmount classifies negative Strength/Dexterity
        // as debuffs. Their magnitude is the layer count, not a negative count
        // and not zero; hidden cleanup models remain excluded.
        return PowerLayerMath.Count(creature.Powers
            .Where(power => power.IsVisible
                && power.TypeForCurrentAmount == type)
            .Select(power => power.Amount));
    }
}
