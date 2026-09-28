using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Routes;

namespace MaidenSuccubus.Core.Seals;

internal static class SealRules
{
    internal static bool IsSealed(CorruptionBand band, RouteCardKind route) =>
        band switch
        {
            CorruptionBand.Holy => route == RouteCardKind.Corrupt,
            CorruptionBand.Corrupt => route == RouteCardKind.Holy,
            _ => false,
        };

    internal static string? DescriptionKey(bool maidenOwner, bool permanentDeck,
        CorruptionBand band, RouteCardKind route) =>
        !maidenOwner || !permanentDeck || !IsSealed(band, route) ? null
            : route == RouteCardKind.Holy
                ? "MAIDENSUCCUBUS_SEALED_CARD.holy"
                : "MAIDENSUCCUBUS_SEALED_CARD.corrupt";
}
