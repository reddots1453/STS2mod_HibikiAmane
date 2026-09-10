using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// Presentation getters hide original card additions while Escape is active.
/// Persistence and mutation entry points must still observe the preserved state.
/// </summary>
internal static class EscapeOriginalStateAccessPatches
{
    private static void BeginSafe(
        CardModel card,
        string operation,
        out IDisposable? state)
    {
        IDisposable? result = null;
        Safe.Run(() =>
        {
            if (ControlQuery.GetProjection(card) != null)
            {
                result = ControlQuery.SuppressPresentation();
            }
        }, operation);
        state = result;
    }

    private static Exception? EndSafe(
        Exception? exception,
        IDisposable? state,
        string operation)
    {
        Safe.Run(() => state?.Dispose(), operation);
        return exception;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.ToSerializable))]
    private static class Serialize
    {
        private static void Prefix(CardModel __instance, out IDisposable? __state) =>
            BeginSafe(__instance, "Escape.Serialize.Begin", out __state);

        private static Exception? Finalizer(
            Exception? __exception,
            IDisposable? __state) =>
            EndSafe(__exception, __state, "Escape.Serialize.End");
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.DowngradeInternal))]
    private static class Downgrade
    {
        private static void Prefix(CardModel __instance, out IDisposable? __state) =>
            BeginSafe(__instance, "Escape.Downgrade.Begin", out __state);

        private static Exception? Finalizer(
            Exception? __exception,
            IDisposable? __state) =>
            EndSafe(__exception, __state, "Escape.Downgrade.End");
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.ClearEnchantmentInternal))]
    private static class ClearEnchantment
    {
        private static void Prefix(CardModel __instance, out IDisposable? __state) =>
            BeginSafe(__instance, "Escape.ClearEnchantment.Begin", out __state);

        private static Exception? Finalizer(
            Exception? __exception,
            IDisposable? __state) =>
            EndSafe(__exception, __state, "Escape.ClearEnchantment.End");
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.ClearAfflictionInternal))]
    private static class ClearAffliction
    {
        private static void Prefix(CardModel __instance, out IDisposable? __state) =>
            BeginSafe(__instance, "Escape.ClearAffliction.Begin", out __state);

        private static Exception? Finalizer(
            Exception? __exception,
            IDisposable? __state) =>
            EndSafe(__exception, __state, "Escape.ClearAffliction.End");
    }

    [HarmonyPatch(
        typeof(CardCmd),
        nameof(CardCmd.Enchant),
        [typeof(EnchantmentModel), typeof(CardModel), typeof(decimal)])]
    private static class Enchant
    {
        private static void Prefix(CardModel card, out IDisposable? __state) =>
            BeginSafe(card, "Escape.Enchant.Begin", out __state);

        private static Exception? Finalizer(
            Exception? __exception,
            IDisposable? __state) =>
            EndSafe(__exception, __state, "Escape.Enchant.End");
    }

    [HarmonyPatch(
        typeof(CardCmd),
        nameof(CardCmd.Afflict),
        [typeof(AfflictionModel), typeof(CardModel), typeof(decimal)])]
    private static class Afflict
    {
        private static void Prefix(CardModel card, out IDisposable? __state) =>
            BeginSafe(card, "Escape.Afflict.Begin", out __state);

        private static Exception? Finalizer(
            Exception? __exception,
            IDisposable? __state) =>
            EndSafe(__exception, __state, "Escape.Afflict.End");
    }
}
