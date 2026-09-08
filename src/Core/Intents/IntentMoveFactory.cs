using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Data;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Core.Intents;

public static class IntentMoveFactory
{
    public static bool TryForceControl(MonsterModel monster, Player player, int escape)
    {
        ControlIntentSpec? spec = EroticAttackCatalog.Get(monster)?.Control;
        if (spec == null || IsSteadfast(monster)) return false;
        SetTransient(
            monster,
            CreateControl(monster, spec with { EscapeRequired = escape }));
        return true;
    }

    public static MoveState CreateControl(
        MonsterModel source,
        ControlIntentSpec spec)
    {
        return NewMove(source, "CONTROL", async targets =>
        {
            Creature? target = FindMaidenSuccubus(targets);
            if (target == null) return;
            ControlResolutionResult result = await ControlCmd.ResolveIntent(
                new BlockingPlayerChoiceContext(),
                source.Creature,
                target,
                spec.BlockRequired,
                spec.ControlType,
                spec.EscapeRequired);
            if (result == ControlResolutionResult.Applied)
            {
                await EroticEffectCmd.Resolve(
                    new BlockingPlayerChoiceContext(),
                    source,
                    target,
                    spec.EffectText);
            }
        }, BuildControlIntents(spec));
    }

    public static MoveState CreateInvasion(
        MonsterModel source,
        InvasionIntentSpec spec)
    {
        return NewMove(source, "INVASION", async targets =>
        {
            Creature? target = FindMaidenSuccubus(targets);
            if (target?.Player == null) return;
            bool succeeded = await InvasionCmd.Resolve(
                new BlockingPlayerChoiceContext(),
                source,
                target.Player,
                spec);
            if (succeeded)
            {
                await EroticEffectCmd.Resolve(
                    new BlockingPlayerChoiceContext(),
                    source,
                    target,
                    spec.EffectText,
                    applyDesireFromText: true);
            }
        }, BuildInvasionIntents(spec));
    }

    public static MoveState CreateDesire(
        MonsterModel source,
        DesireIntentSpec spec)
    {
        return NewMove(source, "DESIRE", async targets =>
        {
            Creature? target = FindMaidenSuccubus(targets);
            if (target?.Player == null) return;
            if (spec.Damage > 0)
            {
                await DamageCmd.Attack(spec.Damage)
                    .WithHitCount(Math.Max(1, spec.Hits))
                    .FromMonster(source)
                    .WithNoAttackerAnim()
                    .Execute(new BlockingPlayerChoiceContext());
            }
            await MaidenSuccubus.Data.Desire.Modify(target.Player, spec.Desire);
            await EroticEffectCmd.Resolve(
                new BlockingPlayerChoiceContext(),
                source,
                target,
                spec.EffectText);
        }, BuildDesireIntents(spec));
    }

    private static AbstractIntent[] BuildDesireIntents(DesireIntentSpec spec)
    {
        var intents = new List<AbstractIntent>();
        if (spec.Damage > 0)
        {
            intents.Add(spec.Hits > 1
                ? new MultiAttackIntent(spec.Damage, spec.Hits)
                : new SingleAttackIntent(spec.Damage));
        }
        intents.Add(new DesireGainIntent(
            spec.Desire,
            spec.DisplayName,
            spec.EffectText));
        intents.AddRange(EroticEffectCmd.BuildSupplementalIntents(
            spec.EffectText,
            EroticIntentKind.Desire));
        return intents.ToArray();
    }

    private static AbstractIntent[] BuildControlIntents(ControlIntentSpec spec)
    {
        var intents = new List<AbstractIntent>
        {
            new ControlIntent(
                spec.BlockRequired,
                spec.ControlType,
                spec.EscapeRequired,
                spec.DisplayName,
                spec.EffectText),
        };
        intents.AddRange(EroticEffectCmd.BuildSupplementalIntents(
            spec.EffectText,
            EroticIntentKind.Control));
        return intents.ToArray();
    }

    private static AbstractIntent[] BuildInvasionIntents(InvasionIntentSpec spec)
    {
        var intents = new List<AbstractIntent>
        {
            new InvasionIntent(
                spec.Damage,
                spec.DisplayName,
                spec.EffectText),
        };
        intents.AddRange(EroticEffectCmd.BuildSupplementalIntents(
            spec.EffectText,
            EroticIntentKind.Invasion));
        return intents.ToArray();
    }

    public static void ForceStun(MonsterModel monster)
    {
        IntentRuntimeState runtime = IntentAdapterRegistry.GetRuntime(monster);
        runtime.ForceStun = true;
        SetTransient(
            monster,
            new MoveState("STUNNED", _ =>
            {
                runtime.ForceStun = false;
                return Task.CompletedTask;
            }, new StunIntent())
            {
                MustPerformOnceBeforeTransitioning = true,
            });
    }

    public static async Task Stun(Creature creature)
    {
        ArgumentNullException.ThrowIfNull(creature.Monster);
        await CreatureCmd.Stun(creature);
        if (creature.IsAlive && !creature.IsStunned)
        {
            // CreatureCmd respects CanTransitionAway and therefore cannot replace
            // one of our unperformed transient erotic moves. Cards that say they
            // stun must still replace that intent immediately.
            ForceStun(creature.Monster);
        }
    }

    public static bool TryForceErotic(MonsterModel monster, Player player)
    {
        EroticMonsterSpec? spec = EroticAttackCatalog.Get(monster);
        if (spec == null || IsSteadfast(monster))
            return false;
        IntentRuntimeState runtime = IntentAdapterRegistry.GetRuntime(monster);
        var available = new List<(EroticIntentKind Kind, MoveState Move, int Weight)>();
        if (spec.Desire != null)
        {
            available.Add((
                EroticIntentKind.Desire,
                CreateDesire(monster, spec.Desire),
                spec.Preferred == EroticIntentKind.Desire ? 3 : 1));
        }
        if (spec.Control != null && !runtime.ControlDisabled
            && !ControlQuery.GetInstances(player).Any(x => ReferenceEquals(x.Applier, monster.Creature)))
        {
            available.Add((
                EroticIntentKind.Control,
                CreateControl(monster, spec.Control),
                spec.Preferred == EroticIntentKind.Control ? 3 : 1));
        }
        if (spec.Invasion != null && !runtime.ControlDisabled && ControlQuery.IsControlled(player))
        {
            available.Add((
                EroticIntentKind.Invasion,
                CreateInvasion(monster, spec.Invasion),
                spec.Preferred == EroticIntentKind.Invasion ? 3 : 1));
        }
        if (available.Count == 0) return false;
        (EroticIntentKind Kind, MoveState Move, int Weight) selected =
            available.Count == 1
                ? available[0]
                : SelectWeighted(monster, available);
        // Player-forced changes never consume the per-combat natural-use cap.
        SetTransient(monster, selected.Move);
        return true;
    }

    public static void SetRecoveryTransient(
        MonsterModel monster,
        MoveState recovery)
    {
        MoveState proxy = NewMove(
            monster,
            "RECOVERY",
            targets => recovery.PerformMove(targets),
            recovery.Intents.ToArray());
        SetTransient(monster, proxy);
    }

    private static (EroticIntentKind Kind, MoveState Move, int Weight)
        SelectWeighted(
            MonsterModel monster,
            IReadOnlyList<(EroticIntentKind Kind, MoveState Move, int Weight)> candidates)
    {
        int roll = monster.RunRng.MonsterAi.NextInt(
            candidates.Sum(candidate => candidate.Weight));
        foreach (var candidate in candidates.OrderBy(candidate => candidate.Kind))
        {
            if (roll < candidate.Weight) return candidate;
            roll -= candidate.Weight;
        }
        return candidates[^1];
    }

    private static bool IsSteadfast(MonsterModel monster) =>
        EroticAttackCatalog.Get(monster)?.Steadfast == true
        || monster.Creature.HasPower<SteadfastPower>();

    public static void SetTransient(MonsterModel monster, MoveState move)
    {
        if (monster.Creature.IsDead) return;
        MoveState current = monster.NextMove;
        if (current.StateId.StartsWith(
                "MAIDENSUCCUBUS_",
                StringComparison.Ordinal))
        {
            move.FollowUpState = current.FollowUpState
                ?? throw new InvalidOperationException(
                    $"Transient move {current.StateId} has no continuation.");
        }
        else
        {
            string successorId = current.GetNextState(
                monster.Creature,
                monster.RunRng.MonsterAi);
            if (!monster.MoveStateMachine!.States.TryGetValue(
                    successorId,
                    out MonsterState? successor))
            {
                throw new InvalidOperationException(
                    $"Original move {current.StateId} has invalid continuation "
                    + $"{successorId} for {monster.Id.Entry}.");
            }
            // Continue after the replaced action. Pointing back to `current`
            // would force the monster to perform the action that the erotic
            // intent replaced, contrary to SYS-TRF-003.
            move.FollowUpState = successor;
        }
        // Keep transient states addressable by a stable id. The state machine
        // is scoped to one monster, so one slot per intent kind is sufficient
        // and can be deterministically recreated after combat restoration.
        monster.MoveStateMachine!.States[move.StateId] = move;
        monster.SetMoveImmediate(move, forceTransition: true);
    }

    private static MoveState NewMove(
        MonsterModel source,
        string kind,
        Func<IReadOnlyList<Creature>, Task> action,
        params AbstractIntent[] intents)
    {
        return new MoveState(
            $"MAIDENSUCCUBUS_{kind}",
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
