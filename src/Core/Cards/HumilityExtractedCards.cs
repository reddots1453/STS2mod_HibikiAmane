using MegaCrit.Sts2.Core.Models;

namespace MaidenSuccubus.Core.Cards;

internal static class HumilityExtractedCards
{
    private static readonly Lazy<HumilityExtractedCatalog> Catalog = new(() =>
    {
        using Stream stream = typeof(HumilityExtractedCards).Assembly.GetManifestResourceStream("MaidenSuccubus.HumilityExtractedCatalog.json")
            ?? throw new InvalidOperationException("Missing generated humility catalog resource.");
        using var reader = new StreamReader(stream);
        return new HumilityExtractedCatalog(reader.ReadToEnd());
    });

    internal static HumilityExtractionEntry Get(CardModel card)
    {
        Type type = card.GetType();
        // A foreign assembly cannot impersonate a catalogued type by namespace/name.
        if (type.Assembly != typeof(CardModel).Assembly && type.Assembly != typeof(HumilityExtractedCards).Assembly)
            return new(null, "No extracted source for assembly " + type.Assembly.GetName().Name);
        return type.FullName != null && Catalog.Value.Entries.TryGetValue(type.FullName, out var entry)
            ? entry : new(null, "No extracted OnPlay for " + type.FullName);
    }

    internal static HumilityRewriteCapability Apply(CardModel card)
    {
        // A saved/duplicated rewritten instance already carries its complete program.
        if (HumilityRewriteCapability.Find(card) is { } rewrite)
            return HumilityRewriteCapability.Apply(card, rewrite.Program);
        var entry = Get(card);
        if (entry.Program == null) throw new NotSupportedException(entry.Error);
        return HumilityRewriteCapability.Apply(card, entry.Program);
    }
}
