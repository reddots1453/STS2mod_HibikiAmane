using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.Core.Cards;

/// <summary>
/// Instance-owned entitlement, copied and saved by RitsuLib with its card.
/// Local zero-cost layers alone cannot override later global cost increases.
/// </summary>
[RegisterModelCapability(StableEntryStem = "free_until_played")]
public sealed class FreeUntilPlayedCapability : CardCapability
{
    internal static bool IsActive(CardModel card) =>
        card.IsMutable && card.CombatState != null && card.Pile?.Type != PileType.Deck
        && ModelCapabilities.TryGet(card, out ModelCapabilitySet? set)
        && set.Get<FreeUntilPlayedCapability>() != null;

    public override Task BeforeCombatStart()
    {
        RemoveFromOwner();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        RemoveFromOwner();
        return Task.CompletedTask;
    }
}
