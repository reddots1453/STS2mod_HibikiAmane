using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace MaidenSuccubus.Keywords;

/// <summary>
/// Registration and query surface for the Portable card keyword.
/// Future control projection code must use <see cref="IsPortable"/> to exempt
/// only the Escape transformation; Portable does not protect from any other
/// temporary card transformation.
/// </summary>
[RegisterOwnedCardKeyword(
    LocalStem,
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
public static class PortableKeyword
{
    public const string LocalStem = "PORTABLE";

    public static string Id => ModContentRegistry.GetQualifiedKeywordId(
        MaidenSuccubusMod.ModId,
        LocalStem);

    public static CardKeyword Value => ModKeywordRegistry.GetCardKeyword(Id);

    public static bool IsPortable(CardModel? card) =>
        card?.Keywords.Contains(Value) == true;

    public static void Apply(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (!IsPortable(card))
        {
            card.AddKeyword(Value);
        }
    }
}
