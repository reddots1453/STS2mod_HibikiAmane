using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MaidenSuccubus.Core.Intents;

/// <summary>Native death, revival and terminal phases must complete before custom intents.</summary>
internal static class NativeIntentPriority
{
    internal static bool IsCustom(MoveState state) =>
        state.StateId.StartsWith("MAIDENSUCCUBUS_", StringComparison.Ordinal);

    internal static bool IsLifecycleTransition(MoveState state) =>
        state.StateId is "DEAD_MOVE" or "REATTACH_MOVE" or "RESPAWN_MOVE"
            or "REVIVE_MOVE" or "ABOUT_TO_BLOW_MOVE" or "EXPLODE_MOVE";

    internal static bool IsProtectedMove(MoveState state) =>
        !IsCustom(state)
        && (IsLifecycleTransition(state)
            || !state.CanTransitionAway
            || state.Intents.Any(intent => intent is StunIntent or SleepIntent
                or DeathBlowIntent or EscapeIntent or HiddenIntent));

    internal static bool HasPriority(MonsterModel monster) =>
        monster.Creature.IsDead
        || IsProtectedMove(monster.NextMove);
}
