using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch]
internal static class EscapeCardProjectionPatches
{
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
    [HarmonyPostfix]
    private static void TitlePostfix(CardModel __instance, ref string __result)
    {
        string result = __result;
        Safe.Run(() =>
        {
            if (ControlQuery.GetProjection(__instance) != null)
            {
                result = new LocString("cards", "MAIDENSUCCUBUS_ESCAPE.title")
                    .GetFormattedText();
            }
        }, "Escape.Title");
        __result = result;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
    [HarmonyPostfix]
    private static void DescriptionPostfix(CardModel __instance, ref LocString __result)
    {
        LocString result = __result;
        Safe.Run(() =>
        {
            EscapeProjection? projection = ControlQuery.GetProjection(__instance);
            if (projection == null) return;
            result = new LocString("cards", "MAIDENSUCCUBUS_ESCAPE.description");
            result.Add("Escape", Math.Max(0, __instance.EnergyCost.GetAmountToSpend()));
            result.Add("Original", projection.OriginalCard.Title);
            result.Add("Source", projection.Control.Applier?.Name ?? "Unknown");
        }, "Escape.Description");
        __result = result;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.TargetType), MethodType.Getter)]
    [HarmonyPostfix]
    private static void TargetPostfix(CardModel __instance, ref TargetType __result)
    {
        TargetType result = __result;
        Safe.Run(() =>
        {
            if (ControlQuery.GetProjection(__instance) != null) result = TargetType.None;
        }, "Escape.Target");
        __result = result;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.Keywords), MethodType.Getter)]
    [HarmonyPostfix]
    private static void KeywordsPostfix(
        CardModel __instance,
        ref IReadOnlySet<CardKeyword> __result)
    {
        IReadOnlySet<CardKeyword> result = __result;
        Safe.Run(() =>
        {
            if (__instance is IMaidenSuccubusRouteCard routeCard
                && routeCard.RouteKind != RouteCardKind.Neutral)
            {
                var decorated = result.ToHashSet();
                decorated.Add(routeCard.RouteKind == RouteCardKind.Corrupt
                    ? RouteCardKeywords.Corrupt
                    : RouteCardKeywords.Holy);
                result = decorated;
            }

            IReadOnlySet<CardKeyword> raw = result;
            if (ControlQuery.GetProjection(__instance, raw) != null)
            {
                result = new HashSet<CardKeyword>();
            }
        }, "Escape.Keywords");
        __result = result;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.Enchantment), MethodType.Getter)]
    [HarmonyPostfix]
    private static void EnchantmentPostfix(
        CardModel __instance,
        ref EnchantmentModel? __result)
    {
        EnchantmentModel? result = __result;
        Safe.Run(() =>
        {
            if (ControlQuery.GetProjection(__instance) != null) result = null;
        }, "Escape.Enchantment");
        __result = result;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.Affliction), MethodType.Getter)]
    [HarmonyPostfix]
    private static void AfflictionPostfix(
        CardModel __instance,
        ref AfflictionModel? __result)
    {
        AfflictionModel? result = __result;
        Safe.Run(() =>
        {
            if (ControlQuery.GetProjection(__instance) != null) result = null;
        }, "Escape.Affliction");
        __result = result;
    }

    [HarmonyPatch(typeof(CardModel), "GetResultLocationForCardPlay")]
    [HarmonyPostfix]
    private static void ResultPilePostfix(
        CardModel __instance,
        ref CardLocation __result)
    {
        CardLocation result = __result;
        Safe.Run(() =>
        {
            if (ControlQuery.GetProjection(__instance) != null)
            {
                result = new CardLocation(
                    __instance.Owner,
                    PileType.Discard,
                    CardPilePosition.Bottom);
            }
        }, "Escape.ResultPile");
        __result = result;
    }
}

public static class EscapeEffectPatcher
{
    public static int Install(Harmony harmony)
    {
        HarmonyMethod prefix = new(typeof(EscapeEffectPatcher), nameof(OnPlayPrefix));
        int count = 0;
        foreach (Type type in AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(type => !type.IsAbstract && typeof(CardModel).IsAssignableFrom(type)))
        {
            MethodInfo? method = type.GetMethod(
                "OnPlay",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (method == null || method.ReturnType != typeof(Task)) continue;
            harmony.Patch(method, prefix);
            count++;
        }
        return count;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    private static bool OnPlayPrefix(CardModel __instance, ref Task __result)
    {
        bool runOriginal = true;
        Task result = __result;
        Safe.Run(() =>
        {
            if (ControlQuery.GetProjection(__instance) != null)
            {
                result = Task.CompletedTask;
                runOriginal = false;
            }
        }, "Escape.OnPlay");
        __result = result;
        return runOriginal;
    }
}
