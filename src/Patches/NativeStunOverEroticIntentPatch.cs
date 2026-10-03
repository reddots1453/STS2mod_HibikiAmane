using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MaidenSuccubus.Core.Intents;

namespace MaidenSuccubus.Patches;

/// <summary>
/// Native recovery requests otherwise fail CanTransitionAway on an unperformed
/// custom intent. Preserve native states and their continuations, not a proxy.
/// </summary>
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.SetMoveImmediate))]
internal static class NativeStunOverEroticIntentPatch
{
    private static void Prefix(
        MonsterModel __instance,
        MoveState state,
        ref bool forceTransition)
    {
        if (forceTransition || !NativeIntentPriority.IsProtectedMove(state)) return;
        MoveState current = __instance.NextMove;
        bool pendingCustomStun = current.StateId == "STUNNED"
            && IntentAdapterRegistry.GetRuntime(__instance).ForceStun;
        if (!NativeIntentPriority.IsCustom(current) && !pendingCustomStun) return;
        // Death/phase transitions can be requested by damage during an active
        // move; ordinary stun keeps the existing rule for an executing move.
        if (__instance.IsPerformingMove
            && !NativeIntentPriority.IsLifecycleTransition(state)) return;
        MaidenSuccubusMod.Logger.Info(
            $"[NativeIntentPriority] {__instance.Id.Entry}: {__instance.NextMove.StateId} -> {state.StateId}");
        if (NativeIntentPriority.IsLifecycleTransition(state))
            IntentAdapterRegistry.GetRuntime(__instance).ForceStun = false;
        forceTransition = true;
    }
}
