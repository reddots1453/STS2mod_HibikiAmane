using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using MegaCrit.Sts2.Core.Localization;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class ControlPower :
    ModPowerTemplate,
    IPowerExtraIconAmountLabelSpecsProvider
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType =>
        PowerInstanceType.InstancedPerApplier;

    public ControlType ControlType { get; set; }

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            description.Add("ControlType", ControlType.LocalizedName());
            description.Add("Source", Applier?.Name ?? "未知来源");
            return description;
        }
    }

    internal ControlBreakReason PendingBreakReason { get; set; } =
        ControlBreakReason.Direct;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MaidenSuccubus/images/ui/corruption_meter.svg",
        BigIconPath: "res://MaidenSuccubus/images/ui/corruption_meter.svg");

    public IReadOnlyList<ExtraIconAmountLabelSpec>
        GetPowerExtraIconAmountLabelSpecs() =>
        [
            ExtraIconAmountLabelSpec.Plain(
                ExtraIconAmountLabelCorner.TopRight,
                ControlType.ShortLabel()),
        ];

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        EscapeProjection? projection = ControlQuery.GetProjection(cardPlay.Card);
        if (projection != null
            && ReferenceEquals(projection.Control, this)
            && cardPlay.IsFirstInSeries)
        {
            EscapeProjectionTracker.Begin(cardPlay, this);
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (EscapeProjectionTracker.TryTake(cardPlay, this, out int amount))
        {
            Flash();
            await ControlCmd.Escape(choiceContext, this, amount);
        }
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (!wasRemovalPrevented
            && Applier != null
            && ReferenceEquals(creature, Applier))
        {
            PendingBreakReason = ControlBreakReason.SourceDied;
            await PowerCmd.Remove(this);
        }
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        if (Applier?.Monster != null)
        {
            IntentAdapterRegistry.OnControlEnded(
                Applier.Monster,
                PendingBreakReason);
        }
        if (oldOwner.Player != null)
        {
            EscapeCardVisuals.Refresh(oldOwner.Player);
        }
        return Task.CompletedTask;
    }
}
