using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MaidenSuccubus.Core.Routes;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Core.Desire;

namespace MaidenSuccubus.Powers;

/// <summary>
/// Reusable rule model for the Ancient effect described in DesignDoc.
/// A future content card only needs to apply this power.
/// </summary>
[RegisterPower]
public sealed class UnboundedDesirePower
    : ModPowerTemplate, IDesireRuleModifier
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MaidenSuccubus/images/ui/desire_resource.png",
        BigIconPath: "res://MaidenSuccubus/images/ui/desire_resource.png");

    public decimal ModifyDesireCap(
        Player player,
        decimal currentCap) =>
        player.Creature == Owner
            ? int.MaxValue
            : currentCap;

    public bool ShouldTriggerDesirePenalty(Player player) =>
        player.Creature != Owner;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (cardPlay.IsLastInSeries
            && cardPlay.Card.Owner.Creature == Owner
            && RouteCardQuery.IsCorrupt(cardPlay.Card)
            && Owner.Player != null)
        {
            await Data.Desire.Modify(Owner.Player, 1);
        }
    }
}
