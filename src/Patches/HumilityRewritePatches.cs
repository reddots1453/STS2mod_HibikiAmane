using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Cards;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

internal static class HumilityRewritePatches
{
    [ThreadStatic] private static int _readingRunListeners;

    private static IEnumerable<MethodBase> CardMethods(string name) =>
        new[] { typeof(CardModel).Assembly, typeof(HumilityRewriteCapability).Assembly }
            .SelectMany(assembly => assembly.GetTypes()).Where(type => typeof(CardModel).IsAssignableFrom(type))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(method => method.Name == name).Distinct();

    // Run after Ritsu's owner-capability expansion. Removing only the original card
    // retains its rewrite capability and native enchantment/affliction listeners.
    [HarmonyPatch]
    private static class HookListeners
    {
        private static IEnumerable<MethodBase> TargetMethods() =>
        [AccessTools.Method(typeof(CombatState), nameof(CombatState.IterateHookListeners)),
         AccessTools.Method(typeof(RunState), nameof(RunState.IterateHookListeners))];

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(MethodBase __originalMethod, ref IEnumerable<AbstractModel> __result)
        {
            if (!HumilityRewriteCapability.HasRewrites) return;
            IEnumerable<AbstractModel> result = __result;
            Safe.Run(() => result = __originalMethod.DeclaringType == typeof(RunState)
                ? ReadRunThenFilter(result) : FilterCombat(result), "Humility.HookStream");
            __result = result;
        }

        private static IEnumerable<AbstractModel> ReadRunThenFilter(IEnumerable<AbstractModel> input)
        {
            AbstractModel[] snapshot;
            // RunState enumerates CombatState, then Ritsu expands capabilities again.
            // Keep original cards in that inner stream until outer expansion finishes;
            // otherwise Ritsu drops already-expanded capabilities without their owners.
            _readingRunListeners++;
            try { snapshot = input.ToArray(); }
            finally { _readingRunListeners--; }
            foreach (AbstractModel model in Filter(snapshot)) yield return model;
        }

        private static IEnumerable<AbstractModel> FilterCombat(IEnumerable<AbstractModel> input) =>
            _readingRunListeners > 0 ? input : Filter(input);
        private static IEnumerable<AbstractModel> Filter(IEnumerable<AbstractModel> input)
        {
            foreach (AbstractModel model in input)
            {
                bool suppress = false;
                Safe.Run(() => suppress = model is CardModel card && HumilityRewriteCapability.Find(card) != null,
                    "Humility.HookListener");
                if (!suppress) yield return model;
            }
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnTurnEndInHandWrapper))]
    private static class TurnEnd
    {
        private static bool Prefix(CardModel __instance, ref Task __result)
        {
            bool suppress = false;
            Safe.Run(() => suppress = HumilityRewriteCapability.Find(__instance) != null, "Humility.TurnEnd");
            if (suppress) __result = Task.CompletedTask;
            return !suppress;
        }
    }

    // IsPlayable is a direct virtual property, not an AbstractModel hook. Removing
    // intrinsic text also removes restrictions such as Rest's transformation condition.
    // Do NOT patch CanPlay: resource, keyword, target and external hook checks remain.
    [HarmonyPatch]
    private static class IntrinsicFlags
    {
        private static IEnumerable<MethodBase> TargetMethods() => CardMethods("get_IsPlayable")
            .Concat(CardMethods("get_HasTurnEndInHandEffect"));
        private static bool Prefix(CardModel __instance, MethodBase __originalMethod, ref bool __result)
        {
            bool rewritten = false;
            Safe.Run(() => rewritten = HumilityRewriteCapability.Find(__instance) != null
                && ControlQuery.GetProjection(__instance) == null, "Humility.IntrinsicFlags");
            if (!rewritten) return true;
            __result = __originalMethod.Name == "get_IsPlayable";
            return false;
        }
    }

    [HarmonyPatch]
    private static class Description
    {
        private static IEnumerable<MethodBase> TargetMethods() => CardMethods("get_Description");

        private static bool Prefix(CardModel __instance, ref LocString __result)
        {
            LocString? replacement = null;
            Safe.Run(() =>
            {
                if (HumilityRewriteCapability.Find(__instance) is { } capability && ControlQuery.GetProjection(__instance) == null)
                    replacement = HumilityRewritePresentation.Description(__instance, capability);
            }, "Humility.Description");
            if (replacement == null) return true;
            __result = replacement;
            return false;
        }
    }

    [HarmonyPatch]
    private static class ResultLocation
    {
        private static bool Prepare() => CardMethods("GetResultLocationForCardPlay").Any();
        private static IEnumerable<MethodBase> TargetMethods() => CardMethods("GetResultLocationForCardPlay");
        private static bool Prefix(CardModel __instance, ref CardLocation __result)
        {
            bool suppress = false;
            CardLocation result = default;
            Safe.Run(() =>
            {
                if (HumilityRewriteCapability.Find(__instance) == null) return;
                PileType pile = __instance.IsDupe ? PileType.None
                    : __instance.ExhaustOnNextPlay || __instance.Keywords.Contains(CardKeyword.Exhaust)
                        ? PileType.Exhaust : PileType.Discard;
                if (pile == PileType.Exhaust) __instance.ExhaustOnNextPlay = false;
                result = new CardLocation(__instance.Owner, pile, CardPilePosition.Bottom);
                suppress = true;
            }, "Humility.ResultLocation");
            if (suppress) __result = result;
            return !suppress;
        }
    }

    // v0.107 returns only a pile; newer versions return a CardLocation.
    [HarmonyPatch]
    private static class LegacyResultPile
    {
        private static bool Prepare() => CardMethods("GetResultPileTypeForCardPlay").Any();
        private static IEnumerable<MethodBase> TargetMethods() => CardMethods("GetResultPileTypeForCardPlay");
        private static bool Prefix(CardModel __instance, ref PileType __result)
        {
            bool suppress = false;
            PileType result = default;
            Safe.Run(() =>
            {
                if (HumilityRewriteCapability.Find(__instance) == null) return;
                result = __instance.IsDupe ? PileType.None
                    : __instance.ExhaustOnNextPlay || __instance.Keywords.Contains(CardKeyword.Exhaust)
                        ? PileType.Exhaust : PileType.Discard;
                if (result == PileType.Exhaust) __instance.ExhaustOnNextPlay = false;
                suppress = true;
            }, "Humility.LegacyResultPile");
            if (suppress) __result = result;
            return !suppress;
        }
    }

    [HarmonyPatch]
    private static class TargetPreview
    {
        private static MethodBase TargetMethod() => typeof(CardModel).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(method => method.Name == "GetDescriptionForPile" && method.GetParameters().Length == 3);
        private static void Prefix(CardModel __instance, Creature? __2, out IDisposable? __state)
        {
            IDisposable? scope = null;
            Safe.Run(() =>
            {
                if (HumilityRewriteCapability.Find(__instance) != null)
                    scope = HumilityRewritePresentation.Targets.Enter(__instance, __2);
            }, "Humility.PreviewScope");
            __state = scope;
        }
        private static void Finalizer(IDisposable? __state) => Safe.Run(() => __state?.Dispose(), "Humility.PreviewEnd");
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
    private static class HoverTips
    {
        private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            IEnumerable<IHoverTip> result = __result;
            Safe.Run(() =>
            {
                if (HumilityRewriteCapability.Find(__instance) is not { } capability || ControlQuery.GetProjection(__instance) != null) return;
                List<IHoverTip> tips = [];
                if (capability.Program.Effects.Any(effect => effect.Kind == HumilityEffectKind.Block))
                    tips.Add(HoverTipFactory.Static(StaticHoverTip.Block));
                if (__instance.Enchantment != null) tips.AddRange(__instance.Enchantment.HoverTips);
                if (__instance.Affliction != null) tips.AddRange(__instance.Affliction.HoverTips);
                int replay = __instance.GetEnchantedReplayCount();
                if (replay > 0) tips.Add(HoverTipFactory.Static(StaticHoverTip.ReplayDynamic, new DynamicVar("Times", replay)));
                tips.AddRange(__instance.Keywords.Select(HoverTipFactory.FromKeyword));
                result = tips.Distinct().ToArray();
            }, "Humility.HoverTips");
            __result = result;
        }
    }
}
