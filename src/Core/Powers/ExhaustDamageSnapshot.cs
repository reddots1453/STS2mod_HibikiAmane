using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MaidenSuccubus.Core.Cards;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Core.Powers;

internal static class ExhaustDamageSnapshot
{
    private readonly record struct AttackSnapshot(int Damage, int Hits);
    private static readonly WeakInstanceValueScope<CardModel, AttackSnapshot> Values = new();

    internal static int ReadCardDamage(CardModel card)
    {
        if (card.Type != CardType.Attack) return 0;
        if (card.DynamicVars.ContainsKey("Damage"))
            return Math.Max(0, card.DynamicVars.Damage.IntValue);
        if (card.DynamicVars.ContainsKey("CalculatedDamage"))
            return Math.Max(0, (int)card.DynamicVars.CalculatedDamage.Calculate(null));
        return 0;
    }

    internal static int ReadCardHits(CardModel card)
    {
        if (card.Type != CardType.Attack) return 0;
        // Extracted OnPlay preserves hard-coded two-hit attacks too; looking only
        // for a Repeat var would incorrectly treat native TwinStrike as one hit.
        int? extracted = null;
        Safe.Run(() =>
        {
            HumilityEffectProgram? program = HumilityRewriteCapability.Find(card)?.Program
                ?? HumilityExtractedCards.Get(card).Program;
            if (program == null) return;
            HumilityXValues x = HumilityNativeEffects.XForPreview(card);
            // OnPlay captured native X survives payment; remaining energy would
            // otherwise turn an exhausted, played Whirlwind into zero hits.
            if (card.EnergyCost.CostsX)
                x = x with { Energy = card.ResolveEnergyXValue() };
            if (card.HasStarCostX)
                x = x with { Stars = card.ResolveStarXValue() };
            decimal Resolve(string name) => HumilityNativeEffects.ResolveValue(card, name, null);
            decimal total = 0;
            foreach (HumilityEffect effect in program.Effects)
            {
                if (effect.Kind != HumilityEffectKind.Damage || effect.Target == HumilityTarget.Self
                    || effect.RequiredCardType != null && Resolve("$cardType:" + effect.RequiredCardType) == 0)
                    continue;
                total += Math.Max(0, decimal.Truncate(effect.Repeats.Evaluate(x, Resolve)));
            }
            extracted = checked((int)total);
        }, "ExhaustDamage.ReadHits");
        if (extracted.HasValue) return extracted.Value;
        // Optional foreign cards do not belong to our extracted source catalog.
        foreach (string name in new[] { "Repeat", "Hits", "CalculatedHits", "Repeats" })
            if (card.DynamicVars.TryGetValue(name, out DynamicVar? value))
                return Math.Max(0, (int)(value is CalculatedVar calculated
                    ? calculated.Calculate(null) : value.BaseValue));
        return 1;
    }

    internal static IDisposable Capture(CardModel card) =>
        Values.Enter(card, new(ReadCardDamage(card), ReadCardHits(card)));
    internal static int Get(CardModel card) => Values.TryGet(card, out AttackSnapshot value)
        ? value.Damage : ReadCardDamage(card);
    internal static int GetHits(CardModel card) => Values.TryGet(card, out AttackSnapshot value)
        ? value.Hits : ReadCardHits(card);

    internal static async Task Complete(Task original, IDisposable scope)
    {
        try { await original; }
        finally { scope.Dispose(); }
    }
}

// Snapshot before any listener can change the exhausted card's own damage.
// Do not release in an ordinary async Postfix: it runs at the first await.
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardExhausted))]
internal static class ExhaustDamageSnapshotPatch
{
    private static void Prefix(CardModel __2, out IDisposable? __state)
    {
        IDisposable? scope = null;
        Safe.Run(() =>
        {
            if (__2.Type == CardType.Attack && __2.Pile?.IsCombatPile == true)
                scope = ExhaustDamageSnapshot.Capture(__2);
        }, "ExhaustDamage.Capture");
        __state = scope;
    }

    private static void Postfix(ref Task __result, IDisposable? __state)
    {
        Task original = __result;
        Task wrapped = original;
        Safe.Run(() =>
        {
            if (__state != null) wrapped = ExhaustDamageSnapshot.Complete(original, __state);
        }, "ExhaustDamage.Complete");
        __result = wrapped;
    }

    private static void Finalizer(Exception? __exception, IDisposable? __state)
    {
        if (__exception != null)
            Safe.Run(() => __state?.Dispose(), "ExhaustDamage.SynchronousFailure");
    }
}
