using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Core.Cards;

namespace MaidenSuccubus.Core.Transformation;

/// <summary>
/// A card whose Magic Amplification bonus is +100% instead of +50%.
/// </summary>
public interface IDoubleMagicAmplification
{
}

internal static class MagicAmplificationCardRules
{
    // The marker is intrinsic card text, not a bonus supplied by a power/enchantment.
    // A rewritten LightArrow still receives ordinary amplification and external bonuses.
    internal static bool HasIntrinsicDouble(CardModel? card) =>
        card is IDoubleMagicAmplification && HumilityRewriteCapability.Find(card) == null;
}
