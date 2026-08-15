using MegaCrit.Sts2.Core.Entities.Creatures;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Core.Condemnation;

public static class CondemnationRules
{
    public static bool ShouldClear(
        CondemnationPower condemnation,
        Creature target)
    {
        var combatState = target.CombatState;
        return combatState == null
            || combatState.IterateHookListeners()
            .OfType<ICondemnationRuleModifier>()
            .All(modifier => modifier.ShouldClearAfterJudgment(
                condemnation,
                target));
    }
}
