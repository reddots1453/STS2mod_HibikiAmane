using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Core.Powers;

internal static class ExhaustDamageSnapshot
{
    private static readonly WeakInstanceValueScope<CardModel, int> Values = new();

    internal static int ReadCardDamage(CardModel card)
    {
        if (card.Type != CardType.Attack) return 0;
        if (card.DynamicVars.ContainsKey("Damage"))
            return Math.Max(0, card.DynamicVars.Damage.IntValue);
        if (card.DynamicVars.ContainsKey("CalculatedDamage"))
            return Math.Max(0, (int)card.DynamicVars.CalculatedDamage.Calculate(null));
        return 0;
    }

    internal static IDisposable Capture(CardModel card) => Values.Enter(card, ReadCardDamage(card));
    internal static int Get(CardModel card) => Values.TryGet(card, out int damage) ? damage : ReadCardDamage(card);

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
