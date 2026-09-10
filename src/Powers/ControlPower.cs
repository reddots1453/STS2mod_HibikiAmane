using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves.Runs;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class ControlPower :
    MaidenSuccubusPowerTemplate,
    IPowerExtraIconAmountLabelSpecsProvider
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType =>
        PowerInstanceType.InstancedPerApplier;

    [SavedProperty]
    public ControlType ControlType { get; set; }

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            description.Add("Amount", Amount);
            description.Add("ControlType", ControlType.LocalizedName());
            description.Add("Source", Applier?.Name ?? "未知来源");
            return description;
        }
    }

    internal ControlBreakReason PendingBreakReason { get; set; } =
        ControlBreakReason.Direct;

    internal void FlashForEscape() => Flash();

    public override PowerAssetProfile AssetProfile => CommonPowerAssets.Corruption;

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext context,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (ReferenceEquals(power, this) && Owner.Player != null)
        {
            MaidenSuccubusMod.Logger.Info(
                $"[ControlProjection] refresh begin type={ControlType} amount={Amount}");
            ControlQuery.Refresh(Owner.Player);
            MaidenSuccubusMod.Logger.Info(
                "[ControlProjection] projection refresh complete");
            EscapeCardVisuals.Refresh(Owner.Player);
            MaidenSuccubusMod.Logger.Info(
                "[ControlProjection] card visual refresh complete");
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (Owner.Player != null && ReferenceEquals(card.Owner, Owner.Player))
        {
            ControlQuery.Refresh(card);
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        MegaCrit.Sts2.Core.Models.AbstractModel? source)
    {
        if (Owner.Player != null && ReferenceEquals(card.Owner, Owner.Player))
        {
            ControlQuery.Refresh(card);
        }
        return Task.CompletedTask;
    }

    public IReadOnlyList<ExtraIconAmountLabelSpec>
        GetPowerExtraIconAmountLabelSpecs() =>
        [
            ExtraIconAmountLabelSpec.Plain(
                ExtraIconAmountLabelCorner.TopRight,
                ControlType.ShortLabel()),
        ];

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
            ControlQuery.Refresh(oldOwner.Player);
            EscapeCardVisuals.Refresh(oldOwner.Player);
        }
        return Task.CompletedTask;
    }
}
