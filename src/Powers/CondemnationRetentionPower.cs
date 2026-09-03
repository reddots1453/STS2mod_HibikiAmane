using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Core.Condemnation;

namespace MaidenSuccubus.Powers;

/// <summary>
/// Framework implementation for “万劫不复”: judgments still deal damage but
/// no longer clear Condemnation.
/// </summary>
[RegisterPower]
public sealed class CondemnationRetentionPower
    : MaidenSuccubusPowerTemplate, ICondemnationRuleModifier
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => CommonPowerAssets.Corruption;

    public bool ShouldClearAfterJudgment(
        CondemnationPower condemnation,
        Creature target) =>
        !(Owner.Player != null
            && target.CombatState == Owner.CombatState);
}
