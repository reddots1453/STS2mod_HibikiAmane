using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves.Runs;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

internal static class LayeredEnchantmentPatches
{
    [HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.ClearInternal))]
    private static class Clear
    {
        private static void Prefix(EnchantmentModel __instance) => Safe.Run(() =>
        {
            if (__instance is LayeredEnchantment layered) layered.ClearChildren();
        }, "LayeredEnchantments.Clear");
    }

    [HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.CanEnchant))]
    private static class Eligibility
    {
        private static void Prefix(CardModel card, out IDisposable? __state)
        {
            IDisposable? scope = null;
            Safe.Run(() => { if (LayeredEnchantments.Supports(card)) scope = LayeredEnchantments.HideSlotForEligibility(card); },
                "LayeredEnchantments.Eligibility");
            __state = scope;
        }
        private static Exception? Finalizer(Exception? __exception, IDisposable? __state)
        {
            Safe.Run(() => __state?.Dispose(), "LayeredEnchantments.EligibilityEnd");
            return __exception;
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.Enchantment), MethodType.Getter)]
    private static class Slot
    {
        private static void Postfix(CardModel __instance, ref EnchantmentModel? __result)
        {
            bool hide = false;
            Safe.Run(() => hide = LayeredEnchantments.HidesSlot(__instance), "LayeredEnchantments.Slot");
            if (hide) __result = null;
        }
    }

    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Enchant), [typeof(EnchantmentModel), typeof(CardModel), typeof(decimal)])]
    private static class Apply
    {
        private static bool Prefix(EnchantmentModel enchantment, CardModel card, decimal amount, ref EnchantmentModel? __result)
        {
            if (!LayeredEnchantments.Supports(card)) return true;
            EnchantmentModel? result = null;
            Safe.Run(() =>
            {
                result = LayeredEnchantments.Apply(enchantment, card, amount);
                if (card.Pile != null)
                    card.Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(card.Owner.NetId)
                        .CardsEnchanted.Add(new CardEnchantmentHistoryEntry(card, enchantment.Id));
            }, "LayeredEnchantments.Apply");
            __result = result; // Typed CardCmd.Enchant<T> must receive the child, not the container.
            return false;
        }
    }

    [HarmonyPatch(typeof(CombatState), nameof(CombatState.IterateHookListeners))]
    private static class CombatListeners
    {
        private static void Postfix(ref IEnumerable<AbstractModel> __result)
        {
            var result = __result;
            Safe.Run(() => result = LayeredEnchantments.Expand(result), "LayeredEnchantments.CombatListeners");
            __result = result;
        }
    }

    [HarmonyPatch(typeof(RunState), nameof(RunState.IterateHookListeners))]
    private static class RunListeners
    {
        private static void Postfix(ref IEnumerable<AbstractModel> __result)
        {
            var result = __result;
            Safe.Run(() => result = LayeredEnchantments.Expand(result), "LayeredEnchantments.RunListeners");
            __result = result;
        }
    }

    [HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.DynamicExtraCardText), MethodType.Getter)]
    private static class CardText
    {
        private static void Postfix(EnchantmentModel __instance, ref LocString? __result)
        {
            if (__instance is not LayeredEnchantment layered) return;
            LocString? result = null;
            Safe.Run(() =>
            {
                string text = string.Join("\n", layered.Layers.Select(layer => layer.DynamicExtraCardText?.GetFormattedText())
                    .Where(text => !string.IsNullOrEmpty(text)));
                if (text.Length > 0)
                {
                    result = new LocString("enchantments", layered.Id.Entry + ".extraCardText");
                    result.Add("Layers", text);
                }
            }, "LayeredEnchantments.CardText");
            __result = result;
        }
    }

    [HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.DynamicDescription), MethodType.Getter)]
    private static class Description
    {
        private static void Postfix(EnchantmentModel __instance, ref LocString __result)
        {
            if (__instance is not LayeredEnchantment layered) return;
            var result = __result;
            Safe.Run(() => result.Add("LayerSummary", layered.Summary), "LayeredEnchantments.Description");
            __result = result;
        }
    }
}
