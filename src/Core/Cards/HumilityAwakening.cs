using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Core.Control;

namespace MaidenSuccubus.Core.Cards;

internal static class HumilityAwakening
{
    internal static bool IsPure(CardModel card)
    {
        if (card.Type is not (CardType.Attack or CardType.Skill) || ControlQuery.GetProjection(card) != null) return false;
        var rewrite = HumilityRewriteCapability.Find(card);
        bool pureBody = rewrite != null ? rewrite.Program.HasDamageOrBlock
            : HumilityExtractedCards.Get(card).OnlyDamageAndBlock;
        if (!pureBody) return false;
        var enchantedKeywords = HumilityRewriteCapability.EnchantmentKeywords(card.Enchantment);
        // On unmodified cards, an intrinsic keyword cannot be excused merely
        // because an enchantment happens to grant the same keyword too.
        if (rewrite == null && card.CanonicalKeywords.Any()) return false;
        if (card.Keywords.Any(keyword => !enchantedKeywords.Contains(keyword))) return false;
        if (card.ShouldRetainThisTurn && !enchantedKeywords.Contains(CardKeyword.Retain)) return false;
        return card.Affliction?.DynamicExtraCardText == null;
    }
}
