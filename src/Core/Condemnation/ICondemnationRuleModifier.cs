using MegaCrit.Sts2.Core.Entities.Creatures;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Core.Condemnation;

public interface ICondemnationRuleModifier
{
    bool ShouldClearAfterJudgment(
        CondemnationPower condemnation,
        Creature target);
}
