using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>Keep the native callback and phase continuation when interrupting our transient.</summary>
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.SetMoveImmediate))]
internal static class NativeStunOverEroticIntentPatch
{
    private static void Prefix(MonsterModel __instance, MoveState state, ref bool forceTransition)
    {
        bool force = forceTransition;
        Safe.Run(() =>
        {
            if (NativeIntentPriority.IsCustom(state) || !NativeIntentPriority.IsProtectedMove(state)) return;
            MoveState current = __instance.NextMove;
            // Keep recognition of stuns scheduled by previous versions.
            bool legacyCustomStun = current.StateId == "STUNNED"
                && IntentAdapterRegistry.GetRuntime(__instance).ForceStun;
            if (!NativeIntentPriority.IsCustom(current) && !legacyCustomStun) return;
            bool lifecycle = NativeIntentPriority.IsLifecycleTransition(state);
            if (__instance.IsPerformingMove && !lifecycle) return;
            if (lifecycle) IntentAdapterRegistry.GetRuntime(__instance).ForceStun = false;
            MaidenSuccubusMod.Logger.Info(
                $"[NativeIntentPriority] {__instance.Id.Entry}: {current.StateId} -> {state.StateId}; continuation={state.FollowUpState?.Id ?? state.FollowUpStateId}");
            // Even explicitly forced native revival requests must clear our
            // abandoned stun receipt; don't return early merely because true.
            force = true;
        }, "NativeIntentPriority.PreserveTransition");
        forceTransition = force;
    }
}
