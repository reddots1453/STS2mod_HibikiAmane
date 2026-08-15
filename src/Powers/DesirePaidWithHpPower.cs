using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Core.Desire;

namespace MaidenSuccubus.Powers;

/// <summary>
/// Replaces the next positive fixed desire payment with equal unblocked HP
/// loss. The RitsuLib shortfall replacement pipeline performs the commit, so
/// affordability never depends on current HP and no desire is spent.
/// </summary>
[RegisterPower]
public sealed class DesirePaidWithHpPower
    : ModPowerTemplate, ISecondaryResourceHookListener
{
    private bool _committed;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            "res://MaidenSuccubus/images/powers/desire_paid_with_hp.svg",
        BigIconPath:
            "res://MaidenSuccubus/images/powers/desire_paid_with_hp.svg");

    public decimal ModifySecondaryResourceCostLate(
        SecondaryResourceCostContext context,
        decimal amount)
    {
        if (!CanReplace(context.Player, context.Definition)
            || amount <= 0m)
        {
            return amount;
        }

        // Force a shortfall exactly equal to the original fixed cost:
        // effective cost = held desire + original cost.
        return amount + SecondaryResourceCmd.Get(
            context.Player,
            DesireResource.Id);
    }

    public SecondaryResourceInsufficientPayment
        ModifySecondaryResourceInsufficientPayment(
            SecondaryResourceInsufficientPaymentContext context,
            SecondaryResourceInsufficientPayment current)
    {
        if (!CanReplace(context.Player, context.Definition)
            || context.Kind != SecondaryResourceUseKind.RequiredCost
            || context.Shortfall <= 0)
        {
            return current;
        }

        return SecondaryResourceInsufficientPayment.AllowPlayWithReplacement(
            resolutionContext =>
                SecondaryResourceShortfallResolution.Cover(
                    resolutionContext.Shortfall,
                    CommitHpPayment),
            spendAvailable: false);
    }

    internal SecondaryResourcePaymentLine ReplaceXPayment(
        SecondaryResourcePaymentLine line)
    {
        int hpCost = Math.Max(0, line.AmountToSpend);
        if (_committed || hpCost == 0)
        {
            return line;
        }

        return line with
        {
            AmountToSpend = 0,
            OriginalShortfall = hpCost,
            CoveredShortfall = hpCost,
            Shortfall = 0,
            SpendAllowed = true,
            InsufficientPayment =
                SecondaryResourceInsufficientPayment.AllowPlay(
                    spendAvailable: false),
            ShortfallResolution =
                SecondaryResourceShortfallResolution.Cover(
                    hpCost,
                    CommitHpPayment),
        };
    }

    internal async Task CommitHpPayment(
        SecondaryResourceShortfallContext context)
    {
        if (_committed || context.CoveredShortfall <= 0)
        {
            return;
        }

        _committed = true;
        Flash();
        await CreatureCmd.Damage(
            new MegaCrit.Sts2.Core.GameActions.Multiplayer
                .ThrowingPlayerChoiceContext(),
            Owner,
            context.CoveredShortfall,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            Owner,
            context.Card,
            null);
        await PowerCmd.Remove(this);
    }

    private bool CanReplace(
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        SecondaryResourceDefinition definition) =>
        !_committed
        && definition.Id == DesireResource.Id
        && player.Creature == Owner;
}
