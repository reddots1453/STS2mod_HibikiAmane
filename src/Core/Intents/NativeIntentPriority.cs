using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MaidenSuccubus.Core.Intents;

/// <summary>Native death, revival and scripted stun callbacks must precede custom recovery.</summary>
internal static class NativeIntentPriority
{
    internal static bool IsCustom(MoveState state) =>
        state.StateId.StartsWith("MAIDENSUCCUBUS_", StringComparison.Ordinal);

    internal static bool IsLifecycleTransition(MoveState state) =>
        state.StateId is "DEAD_MOVE" or "REATTACH_MOVE" or "RESPAWN_MOVE"
            or "REVIVE_MOVE" or "ABOUT_TO_BLOW_MOVE" or "EXPLODE_MOVE"
        // Creature.StunInternal carries native wake-up/phase callbacks in this
        // state, e.g. CeremonialBeast.StunnedMove -> BEAST_CRY_MOVE. It must
        // interrupt an executing transient rather than lose the transition.
        || (state.StateId == "STUNNED" && !string.IsNullOrEmpty(state.FollowUpStateId));

    internal static bool IsProtectedMove(MoveState state) =>
        // Our independent stun slot must also survive natural erotic selection.
        state.Intents.Any(intent => intent is StunIntent)
        || (!IsCustom(state)
            && (IsLifecycleTransition(state)
                || !state.CanTransitionAway
                || state.Intents.Any(intent => intent is SleepIntent
                    or DeathBlowIntent or EscapeIntent or HiddenIntent)));

    internal static bool HasPriority(MonsterModel monster) =>
        monster.Creature.IsDead
        || IsProtectedMove(monster.NextMove);
}
