using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Powers;

/// <summary>
/// Combat-save carrier for temptation modifiers. The visible total is derived
/// from form, armor durability, hand-only effects and this modifier.
/// </summary>
[RegisterPower]
public sealed class TemptationRuntimePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    [SavedProperty]
    public int Modifier { get; set; }
}
