using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Core.Cards;

/// <summary>Read native, cloned/saved cost layers rather than maintain a second expiration clock.</summary>
internal static class NativeFreeCardCost
{
    private static readonly AccessTools.FieldRef<CardEnergyCost, List<LocalCostModifier>> Modifiers =
        AccessTools.FieldRefAccess<CardEnergyCost, List<LocalCostModifier>>("_localModifiers");

    internal static bool IsActive(CardModel card) =>
        card.IsMutable && card.CombatState != null && card.Pile?.Type != PileType.Deck
        && ModelCapabilities.TryGet(card, out ModelCapabilitySet? capabilities)
        && capabilities.Get<NativeFreeSecondaryCapability>() is { } entitlement
        && (entitlement.WholeCombat || entitlement.GrantedTurn == card.Owner.PlayerCombatState?.TurnNumber)
        && !card.EnergyCost.CostsX && card.EnergyCost.HasLocalModifiers
        && Modifiers(card.EnergyCost).Any(modifier => modifier.Type == LocalCostType.Absolute && modifier.Amount == 0)
        && card.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0;

    internal static void Grant(CardModel card, bool wholeCombat)
    {
        if (!card.IsMutable || card.CombatState == null || !CardArtAssets.IsMaidenCard(card)) return;
        var entitlement = ModelCapabilities.TryGet(card, out ModelCapabilitySet? capabilities)
            ? capabilities.Get<NativeFreeSecondaryCapability>() : null;
        if (entitlement == null)
        {
            entitlement = ModelCapabilityRegistry.Create<NativeFreeSecondaryCapability>();
            card.AddCapability(entitlement, allowMerge: false);
        }
        entitlement.WholeCombat |= wholeCombat;
        entitlement.GrantedTurn = card.Owner.PlayerCombatState?.TurnNumber ?? -1;
    }
}

/// <summary>Only a native all-resource free API grants this; energy-only reductions do not.</summary>
[RegisterModelCapability(StableEntryStem = "native_free_secondary")]
public sealed class NativeFreeSecondaryCapability : CardCapability
{
    [SavedProperty] public bool WholeCombat { get; set; }
    [SavedProperty] public int GrantedTurn { get; set; } = -1;
    public override Task BeforeCombatStart() { RemoveFromOwner(); return Task.CompletedTask; }
    public override Task AfterCombatEnd(CombatRoom room) { RemoveFromOwner(); return Task.CompletedTask; }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.SetToFreeThisTurn))]
internal static class NativeFreeThisTurnPatch
{
    private static void Postfix(CardModel __instance) => NativeFreeCardCost.Grant(__instance, false);
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.SetToFreeThisCombat))]
internal static class NativeFreeThisCombatPatch
{
    private static void Postfix(CardModel __instance) => NativeFreeCardCost.Grant(__instance, true);
}
