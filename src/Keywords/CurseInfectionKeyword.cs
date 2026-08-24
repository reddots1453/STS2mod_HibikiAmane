using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace MaidenSuccubus.Keywords;

[RegisterOwnedCardKeyword(
    LocalStem,
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
public static class CurseInfectionKeyword
{
    public const string LocalStem = "CURSE_INFECTION";

    public static CardKeyword Value => ModKeywordRegistry.GetCardKeyword(
        ModContentRegistry.GetQualifiedKeywordId(MaidenSuccubusMod.ModId, LocalStem));
}
