using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MaidenSuccubus.Core.Cards;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

internal static class LibraryHandAuraPatches
{
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.GetKeywordsWithSources))]
    private static class Keywords
    {
        private static void Postfix(CardModel __instance, KeywordSources __0, ref IReadOnlySet<CardKeyword> __result)
        {
            // Native cloning asks for Local only. The aura is a global contribution,
            // including callers that bypass the convenience Keywords property.
            if (!__0.HasFlag(KeywordSources.Global)) return;
            var original = __result;
            IReadOnlySet<CardKeyword> result = original;
            Safe.Run(() =>
            {
                if ((LibraryHandAura.Query(__instance) & LibraryAuraEffect.Exhaust) != 0 && !original.Contains(CardKeyword.Exhaust))
                {
                    var copy = original.ToHashSet();
                    copy.Add(CardKeyword.Exhaust);
                    result = copy;
                }
            }, "LibraryAura.Keywords");
            __result = result;
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.GetEnchantedReplayCount))]
    private static class Replay
    {
        private static void Postfix(CardModel __instance, ref int __result)
        {
            bool add = false;
            Safe.Run(() => add = (LibraryHandAura.Query(__instance) & LibraryAuraEffect.Replay) != 0, "LibraryAura.Replay");
            if (add) __result++;
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    private static class Play
    {
        private static void Prefix(CardModel __instance, out IDisposable? __state)
        {
            IDisposable? scope = null;
            Safe.Run(() => scope = LibraryHandAura.Capture(__instance), "LibraryAura.Capture");
            __state = scope;
        }
        private static void Postfix(ref Task __result, IDisposable? __state)
        {
            if (__state == null) return;
            Task original = __result;
            Task result = original;
            Safe.Run(() => result = LibraryHandAura.Complete(original, __state), "LibraryAura.Complete");
            __result = result;
        }
        private static Exception? Finalizer(Exception? __exception, IDisposable? __state)
        {
            if (__exception != null) Safe.Run(() => __state?.Dispose(), "LibraryAura.Exception");
            return __exception;
        }
    }

    [HarmonyPatch(typeof(NCard), "Reload")]
    private static class Visual
    {
        private static void Postfix(NCard __instance) => Safe.Run(() => LibraryAuraOverlay.Attach(__instance), "LibraryAura.Visual");
    }
}
