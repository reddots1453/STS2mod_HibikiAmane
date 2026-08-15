using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace MaidenSuccubus.Keywords;

/// <summary>
/// Combat-instance ordering for Portable cards. A retained Portable card keeps
/// its slot across turns; leaving the hand releases it, and re-entering the
/// hand assigns a new, later order.
/// </summary>
public static class PortableHandTracker
{
    private sealed class PlayerState
    {
        public long NextSequence;
        public Dictionary<CardModel, long> HandEntries { get; } =
            new(ReferenceEqualityComparer.Instance);
    }

    private static readonly ConditionalWeakTable<Player, PlayerState> States = new();

    public static void ObservePileChange(CardModel card)
    {
        Player? player = card.Owner;
        if (player == null)
        {
            return;
        }

        PlayerState state = States.GetOrCreateValue(player);
        lock (state)
        {
            if (card.Pile?.Type == PileType.Hand && PortableKeyword.IsPortable(card))
            {
                TrackIfNew(state, card);
            }
            else
            {
                state.HandEntries.Remove(card);
            }
        }
    }

    public static CardModel? GetFirstPortableInHand(Player player)
    {
        IReadOnlyList<CardModel> hand = PileType.Hand.GetPile(player).Cards;
        PlayerState state = States.GetOrCreateValue(player);
        lock (state)
        {
            var current = hand.ToHashSet(ReferenceEqualityComparer.Instance);
            foreach (CardModel tracked in state.HandEntries.Keys.ToArray())
            {
                if (!current.Contains(tracked) || !PortableKeyword.IsPortable(tracked))
                {
                    state.HandEntries.Remove(tracked);
                }
            }

            // Fallback for cards placed into hand by an effect that bypasses the
            // normal pile-change hook. Their visible hand order is deterministic.
            foreach (CardModel card in hand)
            {
                if (PortableKeyword.IsPortable(card))
                {
                    TrackIfNew(state, card);
                }
            }

            return state.HandEntries
                .OrderBy(pair => pair.Value)
                .Select(pair => pair.Key)
                .FirstOrDefault();
        }
    }

    private static void TrackIfNew(PlayerState state, CardModel card)
    {
        if (!state.HandEntries.ContainsKey(card))
        {
            state.HandEntries.Add(card, state.NextSequence++);
        }
    }
}
