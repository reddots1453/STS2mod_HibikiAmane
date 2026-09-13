using System.Globalization;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MaidenSuccubus.Powers.Scriptures;

/// <summary>
/// Keeps Guardian Scripture's stored block at its canonical base value while
/// presenting the value that the normal block hooks will award right now.
/// The actual gain still goes through CreatureCmd.GainBlock exactly once.
/// </summary>
internal sealed class GuardianScriptureBlockVar : BlockVar
{
    internal const int BaseBlock = 3;

    internal GuardianScriptureBlockVar()
        : base(BaseBlock, ValueProp.Move)
    {
    }

    public override string ToString() =>
        CurrentValue.ToString(CultureInfo.InvariantCulture);

    protected override decimal GetBaseValueForIConvertible() => CurrentValue;

    private decimal CurrentValue
    {
        get
        {
            if (_owner is not GuardianScripturePower power || !power.IsMutable)
            {
                return BaseValue;
            }

            return Hook.ModifyBlock(
                power.CombatState,
                power.Owner,
                BaseValue,
                Props,
                null,
                null,
                out _);
        }
    }
}
