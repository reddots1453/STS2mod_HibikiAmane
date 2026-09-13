using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

/// <summary>
/// Invisible combat-save carrier for per-monster erotic-intent state. Every
/// mutable value is a named SavedProperty; Amount has no hidden bit-field
/// protocol and remains an ordinary Power field.
/// </summary>
[RegisterPower]
public sealed class EroticIntentRuntimePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    [SavedProperty] public bool ForceStun { get; set; }
    [SavedProperty] public bool ControlDisabled { get; set; }
    [SavedProperty] public int DesireIntentUses { get; set; }
    [SavedProperty] public int ControlIntentUses { get; set; }
    [SavedProperty] public int InvasionIntentUses { get; set; }
    [SavedProperty] public int LastNaturalRollTurn { get; set; } = -1;
    [SavedProperty] public int DesireCooldownThroughTurn { get; set; } = -1;
    [SavedProperty] public int ControlCooldownThroughTurn { get; set; } = -1;
}
