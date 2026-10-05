using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
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
    // These counters represent independent grants. Amount is their total and
    // is managed through PowerCmd so native stacking/save/UI remain intact.
    [SavedProperty] public int ReadyGrants { get; set; }
    [SavedProperty] public int PendingGrants { get; set; }

    // Kept for old saves. When both new counters are absent, Amount and this
    // flag reconstruct the old single grant on the first mutating callback.
    [SavedProperty] public bool DelayOneTurn { get; set; }
    [SavedProperty] public int DelayAppliedOnTurn { get; set; } = -1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Ready", 0), new DynamicVar("Pending", 0)];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool IsActive => IsMutable && Amount > 0 && Owner.IsAlive
        && Owner.CombatState != null && Owner.Powers.Contains(this);

    private int VisibleReady => ReadyGrants + PendingGrants == 0 && Amount > 0 && !DelayOneTurn
        ? Amount : ReadyGrants;
    private int VisiblePending => ReadyGrants + PendingGrants == 0 && Amount > 0 && DelayOneTurn
        ? Amount : PendingGrants;

    private string TimingKey => VisibleReady > 0 && VisiblePending > 0
        ? "mixed" : VisiblePending > 0 ? "nextTurn" : "thisTurn";

    public override LocString Title => new("powers", $"{Id.Entry}.{TimingKey}Title");

    public override LocString Description
    {
        get
        {
            var description = new LocString("powers", $"{Id.Entry}.{TimingKey}Description");
            description.Add("Amount", Amount);
            description.Add("Ready", VisibleReady);
            description.Add("Pending", VisiblePending);
            return description;
        }
    }

    protected override string SmartDescriptionLocKey
    {
        get
        {
            SyncDescriptionVars();
            return $"{Id.Entry}.{TimingKey}SmartDescription";
        }
    }

    public event Action? PowerExtraIconAmountLabelsInvalidated;

    public IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerExtraIconAmountLabelSpecs()
    {
        List<ExtraIconAmountLabelSpec> labels = [];
        if (VisibleReady > 0)
            labels.Add(ExtraIconAmountLabelSpec.Plain(
                ExtraIconAmountLabelCorner.TopRight,
                VisibleReady == 1 ? "本" : $"本{VisibleReady}"));
        if (VisiblePending > 0)
            labels.Add(ExtraIconAmountLabelSpec.Plain(
                ExtraIconAmountLabelCorner.TopLeft,
                VisiblePending == 1 ? "下" : $"下{VisiblePending}"));
        return labels;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this || !IsActive) return Task.CompletedTask;
        int previousAmount = Math.Max(0, Amount - (int)amount);
        MaterializeLegacy(previousAmount);
        ReconcileWithAmount();
        PublishPresentation();
        return Task.CompletedTask;
    }

    // PowerCmd.Apply increases Amount before the card knows whether its
    // Overdraft succeeded. Positive applications start ready; the card moves
    // exactly its newly applied grants into the delayed bucket if necessary.
    public void Schedule(bool delayOneTurn, int newlyAppliedGrants = 1)
    {
        if (!IsActive || newlyAppliedGrants <= 0) return;
        MaterializeLegacy(Amount);
        MaturePendingIfDue();
        if (delayOneTurn)
        {
            int moved = Math.Min(newlyAppliedGrants, ReadyGrants);
            ReadyGrants -= moved;
            PendingGrants += moved;
            DelayAppliedOnTurn = Owner.Player!.PlayerCombatState!.TurnNumber;
        }
        PublishPresentation();
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (IsActive && player.Creature == Owner)
        {
            MaterializeLegacy(Amount);
            MaturePendingIfDue();
        }
        return Task.CompletedTask;
    }

    // The native hook is a repeatable query. It never advances a timer or
    // consumes a grant; one broadcast consumes one ready stack afterward.
    public override bool ShouldTakeExtraTurn(Player player) =>
        IsActive && player.Creature == Owner && VisibleReady > 0;

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (!IsActive || player.Creature != Owner) return Task.CompletedTask;
        MaterializeLegacy(Amount);
        if (ReadyGrants <= 0) return Task.CompletedTask;
        ReadyGrants--;
        PublishPresentation();
        return PowerCmd.Decrement(this);
    }

    private void MaterializeLegacy(int previousAmount)
    {
        if (ReadyGrants + PendingGrants != 0 || previousAmount <= 0) return;
        if (DelayOneTurn) PendingGrants = previousAmount;
        else ReadyGrants = previousAmount;
    }

    private void ReconcileWithAmount()
    {
        int difference = Amount - ReadyGrants - PendingGrants;
        if (difference > 0) ReadyGrants += difference;
        else if (difference < 0)
        {
            int excess = -difference;
            int removeReady = Math.Min(excess, ReadyGrants);
            ReadyGrants -= removeReady;
            PendingGrants = Math.Max(0, PendingGrants - (excess - removeReady));
        }
    }

    private void MaturePendingIfDue()
    {
        if (PendingGrants <= 0 || Owner.Player?.PlayerCombatState is not { } state
            || state.TurnNumber <= DelayAppliedOnTurn) return;
        ReadyGrants += PendingGrants;
        PendingGrants = 0;
        DelayAppliedOnTurn = -1;
        PublishPresentation();
    }

    private void SyncDescriptionVars()
    {
        DynamicVars["Ready"].BaseValue = VisibleReady;
        DynamicVars["Pending"].BaseValue = VisiblePending;
    }

    private void PublishPresentation()
    {
        DelayOneTurn = ReadyGrants == 0 && PendingGrants > 0;
        SyncDescriptionVars();
        PowerExtraIconAmountLabelsInvalidated?.Invoke();
        InvokeDisplayAmountChanged();
    }
}
