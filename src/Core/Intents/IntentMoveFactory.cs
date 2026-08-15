using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Data;

namespace MaidenSuccubus.Core.Intents;

public static class IntentMoveFactory
{
    private static long _sequence;

    public static MoveState CreateControl(
        MonsterModel source,
        ControlIntentSpec spec)
    {
        return NewMove(source, "CONTROL", async targets =>
        {
            Creature? target = FindMaidenSuccubus(targets);
            if (target == null) return;
            await ControlCmd.ResolveIntent(
                new BlockingPlayerChoiceContext(),
                source.Creature,
                target,
                spec.BlockRequired,
                spec.ControlType,
                spec.EscapeRequired);
        }, new ControlIntent(
            spec.BlockRequired,
            spec.ControlType,
            spec.EscapeRequired));
    }

    public static MoveState CreateInvasion(
        MonsterModel source,
        InvasionIntentSpec spec)
    {
        return NewMove(source, "INVASION", async targets =>
        {
            Creature? target = FindMaidenSuccubus(targets);
            if (target?.Player == null) return;
            await InvasionCmd.Resolve(
                new BlockingPlayerChoiceContext(),
                source,
                target.Player,
                spec);
        }, new InvasionIntent(spec.Damage));
    }

    public static MoveState CreateDesire(
        MonsterModel source,
        DesireIntentSpec spec)
    {
        return NewMove(source, "DESIRE", async targets =>
        {
            Creature? target = FindMaidenSuccubus(targets);
            if (target?.Player == null) return;
            IntentRuntimeState runtime = IntentAdapterRegistry.GetRuntime(source);
            if (runtime.DesireIntentUses >= spec.MaxUsesPerCombat) return;
            runtime.DesireIntentUses++;
            await MaidenSuccubus.Data.Desire.Modify(target.Player, spec.Desire);
        }, new DesireGainIntent(spec.Desire));
    }

    public static void ForceStun(MonsterModel monster) =>
        SetTransient(
            monster,
            NewMove(monster, "STUN", _ => Task.CompletedTask, new StunIntent()));

    public static void SetTransient(MonsterModel monster, MoveState move)
    {
        if (monster.Creature.IsDead) return;
        MoveState current = monster.NextMove;
        move.FollowUpState = current.StateId.StartsWith(
            "MAIDENSUCCUBUS_",
            StringComparison.Ordinal)
            && current.FollowUpState is MoveState preserved
                ? preserved
                : current;
        monster.SetMoveImmediate(move, forceTransition: true);
    }

    private static MoveState NewMove(
        MonsterModel source,
        string kind,
        Func<IReadOnlyList<Creature>, Task> action,
        params AbstractIntent[] intents)
    {
        return new MoveState(
            $"MAIDENSUCCUBUS_{kind}_{source.Creature.CombatId}_{Interlocked.Increment(ref _sequence)}",
            action,
            intents)
        {
            MustPerformOnceBeforeTransitioning = true,
        };
    }

    private static Creature? FindMaidenSuccubus(
        IReadOnlyList<Creature> targets) =>
        targets.FirstOrDefault(creature =>
            creature.IsAlive
            && creature.Player?.Character is MaidenSuccubusCharacter);
}
