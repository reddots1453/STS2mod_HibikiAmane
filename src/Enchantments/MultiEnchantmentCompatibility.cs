using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace MaidenSuccubus.Enchantments;

/// <summary>Optional v2 public API bridge; the external mod owns its extra slots,
/// stacking rules, hooks and serialization. Never overwrite its card state.</summary>
internal static class MultiEnchantmentCompatibility
{
    private sealed record Api(MethodInfo Enchant, MethodInfo SuppressDeckSync,
        MethodInfo GetEnchantments, object CombatScope);
    private static Api? _api;
    private static bool _warned;
    internal static bool Active => Resolve() != null;

    private static Api? Resolve()
    {
        if (_api != null) return _api;
        Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(candidate => candidate.GetName().Name == "MultiEnchantmentMod");
        if (assembly == null) return null; // Do not cache absence before all mods load.
        Type? api = assembly.GetType("MultiEnchantmentMod.Api.MultiEnchantmentApi");
        Type? scope = assembly.GetType("MultiEnchantmentMod.Api.EnchantmentScope");
        MethodInfo? enchant = scope == null ? null : api?.GetMethod("Enchant",
            [typeof(CardModel), typeof(EnchantmentModel), typeof(decimal), scope]);
        MethodInfo? suppress = api?.GetMethod("SuppressDeckVersionSync", Type.EmptyTypes);
        // GetEnchantments<T>(CardModel) has the same argument list. Select the
        // non-generic public overload explicitly to avoid AmbiguousMatchException.
        MethodInfo? query = api?.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(method => method.Name == "GetEnchantments" && !method.IsGenericMethod
                && method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual([typeof(CardModel)]));
        object? combatScope = scope?.GetProperty("UntilCombatEnds")?.GetValue(null);
        if (enchant == null || suppress == null || query == null || combatScope == null)
        {
            if (!_warned)
            {
                _warned = true;
                MaidenSuccubusMod.Logger.Warn("[MultiEnchantCompat] Public combat-scope API unavailable; keeping single-slot guards.");
            }
            return null;
        }
        // A merely loaded/disabled assembly does not authorize extra slots.
        var patches = Harmony.GetPatchInfo(AccessTools.Method(typeof(EnchantmentModel), nameof(EnchantmentModel.CanEnchant)));
        if (patches?.Owners.Contains("MultiEnchantmentMod") != true) return null;
        _api = new(enchant, suppress, query, combatScope);
        MaidenSuccubusMod.Logger.Info("[MultiEnchantCompat] Enabled public API bridge; combat scope=UntilCombatEnds; deck sync suppressed.");
        return _api;
    }

    internal static T ApplyCombat<T>(T enchantment, CardModel card, decimal amount)
        where T : EnchantmentModel
    {
        Api api = Resolve() ?? throw new InvalidOperationException("Multi-enchantment API is not active.");
        using var suppression = (IDisposable?)api.SuppressDeckSync.Invoke(null, null)
            ?? throw new InvalidOperationException("Multi-enchantment deck-sync suppression unavailable.");
        try
        {
            return api.Enchant.Invoke(null, [card, enchantment, amount, api.CombatScope]) as T
                ?? throw new InvalidOperationException($"Multi-enchantment did not apply {enchantment.Id} to {card.Id}.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            // No fallback after an external mutation: that could replace or duplicate layers.
            throw new InvalidOperationException($"Multi-enchantment application failed for {card.Id}.", ex.InnerException);
        }
    }

    internal static bool Has<T>(CardModel card) where T : EnchantmentModel =>
        Resolve() is { } api
        && api.GetEnchantments.Invoke(null, [card]) is IEnumerable<EnchantmentModel> enchantments
        && enchantments.Any(enchantment => enchantment is T);
}
