using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>Native BeforeDamage is awaited once per hit, including one hit on
/// multiple targets. Preserve existing callbacks without patching MoveNext.</summary>
[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
internal static class BurningPerHitPatch
{
    private static readonly ConditionalWeakTable<AttackCommand, HitCallback> Callbacks = new();

    private static void Prefix(AttackCommand __instance, PlayerChoiceContext? __0,
        ref Func<Task>? ____beforeDamage)
    {
        Func<Task>? callback = ____beforeDamage;
        Safe.Run(() =>
        {
            if (__instance.Attacker?.Side != CombatSide.Enemy) return;
            if (Callbacks.TryGetValue(__instance, out HitCallback? previous)
                && callback != previous.Callback) Callbacks.Remove(__instance);
            HitCallback receipt = Callbacks.GetValue(__instance, command => new HitCallback(command, callback));
            receipt.Context = __0;
            callback = receipt.Callback;
        }, nameof(BurningPerHitPatch));
        ____beforeDamage = callback;
    }

    private sealed class HitCallback
    {
        private readonly AttackCommand _command;
        private readonly Func<Task>? _original;
        internal PlayerChoiceContext? Context;
        internal Func<Task> Callback { get; }
        internal HitCallback(AttackCommand command, Func<Task>? original)
        {
            _command = command;
            _original = original;
            Callback = BeforeHit;
        }
        private async Task BeforeHit()
        {
            if (_original != null) await _original();
            if (_command.Attacker is { IsDead: false } dealer
                && dealer.GetPower<BurningPower>() is { } burning)
                await burning.ResolveBeforeHit(Context ?? new BlockingPlayerChoiceContext());
        }
    }
}

/// <summary>The native hit loop checks death before BeforeDamage. If Burning
/// kills the attacker in that callback, cancel this pending hit as well.</summary>
[HarmonyPatch]
internal static class BurningLethalHitPatch
{
    private static MethodBase TargetMethod() => typeof(CreatureCmd).GetMethods()
        .Single(method => method.Name == nameof(CreatureCmd.Damage)
            && method.GetParameters() is var args && args.Length is 6 or 7
            && args[1].ParameterType == typeof(IEnumerable<Creature>)
            && args[2].ParameterType == typeof(decimal));

    private static bool Prefix(object[] __args, ref Task<IEnumerable<DamageResult>> __result)
    {
        bool cancel = false;
        Safe.Run(() => cancel = __args[4] is Creature { Side: CombatSide.Enemy, IsDead: true }
            && ((ValueProp)__args[3]).IsPoweredAttack(), nameof(BurningLethalHitPatch));
        if (!cancel) return true;
        __result = Task.FromResult<IEnumerable<DamageResult>>([]);
        return false;
    }
}
