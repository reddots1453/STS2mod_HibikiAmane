using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Core.Control;
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
            if (ControlQuery.GetProjection(__instance) is not { } projection)
            {
                return;
            }
            result = new LocString("cards", "MAIDENSUCCUBUS_ESCAPE.description");
            result.Add("Escape", Math.Max(0, __instance.EnergyCost.GetAmountToSpend()));
            result.Add("Original", projection.OriginalTitle);
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
            IReadOnlySet<CardKeyword> raw = result;
            if (ControlQuery.GetProjection(__instance, raw) != null)
            {
                result = new HashSet<CardKeyword>();
            }
            if (TransparentOutfitCurse.ProjectsEtherealOnto(__instance)
                && !result.Contains(CardKeyword.Ethereal))
            {
                result = result.ToHashSet();
                ((HashSet<CardKeyword>)result).Add(CardKeyword.Ethereal);
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
    private static readonly object Sync = new();
    private static readonly HashSet<MethodBase> PatchedMethods = [];
    private static Harmony? _harmony;

    public static void Configure(Harmony harmony)
    {
        lock (Sync)
        {
            _harmony = harmony;
        }
    }

    public static void EnsurePatched(CardModel card)
    {
        Harmony? harmony;
        MethodInfo? method = card.GetType().GetMethod(
            "OnPlay",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (method == null || method.ReturnType != typeof(Task))
        {
            throw new MissingMethodException(
                card.GetType().FullName,
                "OnPlay(PlayerChoiceContext, CardPlay)");
        }

        lock (Sync)
        {
            if (PatchedMethods.Contains(method))
            {
                return;
            }
            harmony = _harmony ?? throw new InvalidOperationException(
                "EscapeEffectPatcher was not configured during mod initialization.");
            harmony.Patch(
                method,
                prefix: new HarmonyMethod(
                    typeof(EscapeEffectPatcher),
                    nameof(OnPlayPrefix)));
            PatchedMethods.Add(method);
            MaidenSuccubusMod.Logger.Info(
                $"Escape projection activated for card effect: "
                + $"{method.DeclaringType?.FullName}.OnPlay");
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
