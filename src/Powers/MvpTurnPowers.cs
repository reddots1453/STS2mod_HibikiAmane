using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class MultipleReproductionPower :
    MaidenSuccubusPowerTemplate,
    IPowerExtraIconAmountLabelSpecsProvider,
    IPowerExtraIconAmountLabelsChangeSource
{
    private bool _delayOneTurn;

    [SavedProperty]
    public bool DelayOneTurn
    {
        get => _delayOneTurn;
        set
        {
            if (_delayOneTurn == value)
            {
                return;
            }

            _delayOneTurn = value;
            PowerExtraIconAmountLabelsInvalidated?.Invoke();
        }
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new(
        "powers",
        $"{Id.Entry}.{(DelayOneTurn ? "nextTurnTitle" : "thisTurnTitle")}");

    public override LocString Description => new(
        "powers",
        $"{Id.Entry}.{(DelayOneTurn ? "nextTurnDescription" : "thisTurnDescription")}");

    protected override string SmartDescriptionLocKey =>
        $"{Id.Entry}.{(DelayOneTurn ? "nextTurnSmartDescription" : "thisTurnSmartDescription")}";

    public event Action? PowerExtraIconAmountLabelsInvalidated;

    public IReadOnlyList<ExtraIconAmountLabelSpec>
        GetPowerExtraIconAmountLabelSpecs() =>
        [
            ExtraIconAmountLabelSpec.Plain(
                ExtraIconAmountLabelCorner.TopRight,
                DelayOneTurn ? "下" : "本"),
        ];

    public override bool ShouldTakeExtraTurn(Player player)
    {
        if (player.Creature != Owner)
            return false;
        if (DelayOneTurn)
        {
            DelayOneTurn = false;
            return false;
        }
        return true;
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player.Creature == Owner)
            return PowerCmd.Remove(this);
        return Task.CompletedTask;
    }
}
