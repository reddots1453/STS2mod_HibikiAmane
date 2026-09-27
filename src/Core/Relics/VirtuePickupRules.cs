namespace MaidenSuccubus.Core.Relics;

internal readonly record struct DiligenceRewardRule(int Count, bool Upgrade, bool Enchant);

internal static class VirtuePickupRules
{
    public static int BenevolencePickupCount(int stage) => stage is 1 or 2 ? 2 : 0;
    // Stage3 exists only for legacy compatibility; Stage4 is the new awakened reward.
    public static bool BenevolenceOnAdded(int stage, bool sameOwner, bool wasDeck, bool isDeck) =>
        stage is 3 or 4 && sameOwner && !wasDeck && isDeck;

    public static DiligenceRewardRule Diligence(int stage) => stage switch
    {
        1 => new(2, false, false), 2 => new(2, true, false),
        3 or 4 => new(3, true, true), _ => default
    };

    public static (int Min, int Max) EnchantmentRange(string typeName) => typeName switch
    {
        // Kifuda/木札 applies Adroit/伶俐. Nimble is 灵巧, not 伶俐.
        "Sharp" or "Nimble" => (1, 5), "Adroit" => (2, 4),
        "Momentum" => (3, 8), "Sown" or "Swift" => (1, 2),
        "Vigorous" => (6, 12), _ => (1, 1)
    };

    public static int RollEnchantmentAmount(string typeName, Func<int, int, int> nextInt)
    {
        var (min, max) = EnchantmentRange(typeName);
        return min == max ? min : nextInt(min, max + 1);
    }
}
