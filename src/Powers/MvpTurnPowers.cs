using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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

    // -1 is the legacy state: advance at the next owner turn-start callback.
    [SavedProperty]
    public int DelayAppliedOnTurn { get; set; } = -1;

    private bool IsActive => IsMutable && Amount > 0 && Owner.IsAlive
        && Owner.CombatState != null && Owner.Powers.Contains(this);

    public void Schedule(bool delayOneTurn)
    {
        if (!IsActive) return;
        DelayAppliedOnTurn = Owner.Player!.PlayerCombatState!.TurnNumber;
        DelayOneTurn = delayOneTurn;
    }

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

    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (IsActive && player.Creature == Owner && DelayOneTurn
            && player.PlayerCombatState!.TurnNumber > DelayAppliedOnTurn)
        {
            // An autoplay earlier in this SAME turn-start must still wait for
            // the next personal turn. Extra turns count as personal turns too.
            DelayOneTurn = false;
        }
        return Task.CompletedTask;
    }

    // Native Hook.ShouldTakeExtraTurn short-circuits: it is a query, not a clock.
    public override bool ShouldTakeExtraTurn(Player player) =>
        IsActive && player.Creature == Owner && !DelayOneTurn;

    public override Task AfterTakingExtraTurn(Player player)
    {
        // The engine broadcasts this even when ANOTHER source grants the turn.
        // Pending next-turn grants must survive that broadcast.
        if (IsActive && player.Creature == Owner && !DelayOneTurn)
            return PowerCmd.Remove(this);
        return Task.CompletedTask;
    }
}
