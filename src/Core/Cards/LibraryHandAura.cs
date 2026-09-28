using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Core.Cards;

internal static class LibraryHandAura
{
    private static readonly WeakInstanceValueScope<CardModel, LibraryAuraEffect> Plays = new();

    internal static LibraryAuraEffect InHand(CardModel? card)
    {
        if (card is not { IsMutable: true } || card.Owner?.Character is not MaidenSuccubusCharacter
            || card.Owner.Creature.CombatState == null || card.HasBeenRemovedFromState
            || card.Pile?.Type != PileType.Hand) return LibraryAuraEffect.None;
        var hand = card.Pile.Cards;
        int index = -1;
        for (int i = 0; i < hand.Count; i++) if (ReferenceEquals(hand[i], card)) { index = i; break; }
        return LibraryNeighbourRules.Evaluate(hand, index, neighbour => neighbour is InsatiableGreed
            && neighbour.Owner == card.Owner && !neighbour.HasBeenRemovedFromState);
    }

    internal static LibraryAuraEffect Query(CardModel card) =>
        card.IsMutable && card.Pile?.Type == PileType.Play && Plays.TryGet(card, out var captured)
            ? captured : InHand(card);

    // Always push, even None: a nested play must not inherit its outer play's aura.
    internal static IDisposable Capture(CardModel card) => Plays.Enter(card, InHand(card));

    internal static async Task Complete(Task original, IDisposable scope)
    {
        try { await original; }
        finally { scope.Dispose(); }
    }
}
