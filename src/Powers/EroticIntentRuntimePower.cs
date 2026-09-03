using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

/// <summary>
/// Invisible combat-save carrier for per-monster erotic-intent state. Combat
/// snapshots preserve a power's id and Amount, so the state is encoded into
/// Amount instead of living only in a process-local weak table.
/// </summary>
[RegisterPower]
public sealed class EroticIntentRuntimePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;
}
