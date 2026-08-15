using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace MaidenSuccubus.Enchantments;

/// <summary>
/// Captures hand adjacency before CardModel.OnPlayWrapper moves the played
/// card to the Play pile.
/// </summary>
public static class InfectionHandSnapshot
{
    private static readonly Dictionary<CardModel, IReadOnlyList<CardModel>>
        Snapshots = new(ReferenceEqualityComparer.Instance);

    public static void Capture(CardModel card)
    {
        if (card.Enchantment is not InfectionEnchantment
            || card.Pile?.Type != PileType.Hand)
        {
            Snapshots.Remove(card);
            return;
        }

        IReadOnlyList<CardModel> hand = card.Pile.Cards;
        int index = -1;
        for (int i = 0; i < hand.Count; i++)
        {
            if (ReferenceEquals(hand[i], card))
            {
                index = i;
                break;
            }
        }
        if (index < 0)
        {
            Snapshots.Remove(card);
            return;
        }

        List<CardModel> adjacent = new(2);
        if (index > 0)
        {
            adjacent.Add(hand[index - 1]);
        }
        if (index + 1 < hand.Count)
        {
            adjacent.Add(hand[index + 1]);
        }
        Snapshots[card] = adjacent;
    }

    public static IReadOnlyList<CardModel> Consume(CardModel card)
    {
        if (!Snapshots.Remove(card, out IReadOnlyList<CardModel>? adjacent))
        {
            return Array.Empty<CardModel>();
        }

        return adjacent
            .Where(candidate => candidate.Pile?.Type == PileType.Hand)
            .ToArray();
    }
}
