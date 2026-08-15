using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace MaidenSuccubus.Keywords;

/// <summary>
/// Visible route markers. They are descriptive keywords rather than combat
/// mechanics, so route identity remains visible on cards in every screen.
/// </summary>
[RegisterOwnedCardKeyword(
    CorruptStem,
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(
    HolyStem,
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
public static class RouteCardKeywords
{
    public const string CorruptStem = "CORRUPT_ROUTE";
    public const string HolyStem = "HOLY_ROUTE";

    public static CardKeyword Corrupt => ModKeywordRegistry.GetCardKeyword(
        ModContentRegistry.GetQualifiedKeywordId(MaidenSuccubusMod.ModId, CorruptStem));

    public static CardKeyword Holy => ModKeywordRegistry.GetCardKeyword(
        ModContentRegistry.GetQualifiedKeywordId(MaidenSuccubusMod.ModId, HolyStem));
}
