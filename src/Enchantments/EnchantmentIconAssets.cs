using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.UI;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Enchantments;

internal static class EnchantmentIconAssets
{
    private const string Fallback = "res://images/powers/strength_power.png";
    private static readonly Dictionary<string, EnchantmentAssetProfile> Profiles =
        new(StringComparer.Ordinal);

    internal static EnchantmentAssetProfile For(string assetName)
    {
        if (Profiles.TryGetValue(assetName, out EnchantmentAssetProfile? profile)) return profile;
        string path = RuntimeTextureAssets.PrepareResource(
            $"enchantments/{assetName}.png",
            $"user://maiden_enchantment_{assetName}.tres",
            Fallback);
        profile = new EnchantmentAssetProfile(IconPath: path);
        Profiles[assetName] = profile;
        return profile;
    }
}
