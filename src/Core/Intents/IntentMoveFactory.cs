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
using MaidenSuccubus.Core.Transformation;

namespace MaidenSuccubus.Core.Intents;

public static class IntentMoveFactory
{
    public static bool TryForceControl(MonsterModel monster, Player player, int escape)
    {
        ControlIntentSpec? spec = EroticAttackCatalog.Get(monster)?.Control;
        if (spec == null
            || IsSteadfast(monster)
            || !IsKindLegal(monster, player, EroticIntentKind.Control, false))
        {
            return false;
        }
        IntentAdapterRegistry.GetRuntime(monster)
            .Increment(EroticIntentKind.Control);
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
            EroticIntentVisualEvents.Publish(target, EroticIntentKind.Control);
            ControlResolutionResult result = await ControlCmd.ResolveIntent(
                new BlockingPlayerChoiceContext(),
                source.Creature,
                target,
                spec.BlockRequired,
                spec.ControlType,
                spec.EscapeRequired);
            if (result == ControlResolutionResult.Applied)
            {
                IntentAdapterRegistry.GetRuntime(source)
                    .ControlCooldownThroughTurn =
                        source.CombatState.RoundNumber + 1;
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
            EroticIntentVisualEvents.Publish(target, EroticIntentKind.Invasion);
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
            EroticIntentVisualEvents.Publish(target, EroticIntentKind.Desire);
            if (spec.Damage > 0)
            {
                await DamageCmd.Attack(spec.Damage)
                    .WithHitCount(Math.Max(1, spec.Hits))
                    .FromMonster(source)
                    .WithNoAttackerAnim()
                    .Execute(new BlockingPlayerChoiceContext());
            }
            if (spec.Desire > 0)
            {
                await MaidenSuccubus.Data.Desire.Modify(
                    target.Player, spec.Desire);
            }
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
        if (spec.Desire > 0)
        {
            intents.Add(new DesireGainIntent(
                spec.Desire,
                spec.DisplayName,
                spec.EffectText));
        }
        // A desire attack already exposes its complete effect text through the
        // DesireGainIntent hover. Do not repeat debuff, status-card, buff or
        // healing icons beside it; only the two dedicated desire components
        // have their own supplemental visuals.
        if (spec.EffectText.Contains("撕裂衣服", StringComparison.Ordinal))
        {
            intents.Add(new TearClothingIntent());
        }
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
        List<EroticIntentKind> available = Enum.GetValues<EroticIntentKind>()
            .Where(kind => IsKindLegal(monster, player, kind, false))
            .ToList();
        if (available.Count == 0) return false;
        EroticIntentKind selected = available[
            monster.RunRng.MonsterAi.NextInt(available.Count)];
        MarkSelected(monster, selected);
        SetTransient(monster, CreateMove(monster, spec, selected));
        return true;
    }

    public static bool TryApplyNaturalErotic(MonsterModel monster, Player player)
    {
        EroticMonsterSpec? spec = EroticAttackCatalog.Get(monster);
        if (spec == null
            || IsSteadfast(monster)
            || monster.Creature.IsDead
            || IsEroticMove(monster.NextMove)
            || monster.NextMove.StateId == "STUNNED"
            || monster.NextMove.Intents.Any(intent => intent is StunIntent))
        {
            return false;
        }

        int temptation = Core.Temptation.Temptation.Get(player);
        List<(EroticIntentKind Kind, int Threshold)> candidates =
            Enum.GetValues<EroticIntentKind>()
                .Where(kind => IsKindLegal(monster, player, kind, true))
                .Select(kind => (kind, Threshold(spec, kind)))
                .Where(candidate => temptation >= candidate.Item2)
                .ToList();
        if (candidates.Count == 0)
        {
            return false;
        }

        EroticIntentKind selected = candidates
            .OrderByDescending(candidate => candidate.Threshold)
            .ThenBy(candidate => candidate.Kind)
            .First().Kind;
        MarkSelected(monster, selected);
        SetTransient(monster, CreateMove(monster, spec, selected));
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

    private static bool IsKindLegal(
        MonsterModel monster,
        Player player,
        EroticIntentKind kind,
        bool requireThreshold)
    {
        EroticMonsterSpec? spec = EroticAttackCatalog.Get(monster);
        if (spec == null)
        {
            return false;
        }
        IntentRuntimeState runtime = IntentAdapterRegistry.GetRuntime(monster);
        int round = monster.CombatState.RoundNumber;
        return kind switch
        {
            EroticIntentKind.Desire => spec.Desire is { } desire
                && (!requireThreshold || spec.DesireThreshold > 0)
                && runtime.DesireIntentUses < desire.MaxUsesPerCombat
                && round > runtime.DesireCooldownThroughTurn,
            EroticIntentKind.Control => spec.Control is { } control
                && (!requireThreshold || spec.ControlThreshold > 0)
                && !runtime.ControlDisabled
                && runtime.ControlIntentUses < control.MaxUsesPerCombat
                && round > runtime.ControlCooldownThroughTurn,
            EroticIntentKind.Invasion => spec.Invasion is { } invasion
                && (!requireThreshold || spec.InvasionThreshold > 0)
                && !runtime.ControlDisabled
                && runtime.InvasionIntentUses < invasion.MaxUsesPerCombat
                && ControlQuery.IsControlled(player)
                && TransformationCmd.IsTransformed(player.Creature)
                && TransformationCmd.GetArmor(player.Creature) is { Amount: <= 1 }
                && !player.Creature.HasPower<ChastityDefensePower>(),
            _ => false,
        };
    }

    private static void MarkSelected(MonsterModel monster, EroticIntentKind kind)
    {
        IntentRuntimeState runtime = IntentAdapterRegistry.GetRuntime(monster);
        runtime.Increment(kind);
        if (kind == EroticIntentKind.Desire)
        {
            runtime.DesireCooldownThroughTurn =
                monster.CombatState.RoundNumber + 1;
        }
    }

    private static int Threshold(EroticMonsterSpec spec, EroticIntentKind kind) =>
        kind switch
        {
            EroticIntentKind.Desire => spec.DesireThreshold,
            EroticIntentKind.Control => spec.ControlThreshold,
            _ => spec.InvasionThreshold,
        };

    private static MoveState CreateMove(
        MonsterModel monster,
        EroticMonsterSpec spec,
        EroticIntentKind kind) => kind switch
        {
            EroticIntentKind.Desire => CreateDesire(monster, spec.Desire!),
            EroticIntentKind.Control => CreateControl(monster, spec.Control!),
            _ => CreateInvasion(monster, spec.Invasion!),
        };

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
            move.FollowUpState = current;
        }
        // Keep transient states addressable by a stable id. The state machine
        // is scoped to one monster, so one slot per intent kind is sufficient
        // and can be deterministically recreated after combat restoration.
        monster.MoveStateMachine!.States[move.StateId] = move;
        monster.SetMoveImmediate(move, forceTransition: true);
    }

    private static bool IsEroticMove(MoveState move) =>
        move.StateId.StartsWith("MAIDENSUCCUBUS_DESIRE", StringComparison.Ordinal)
        || move.StateId.StartsWith("MAIDENSUCCUBUS_CONTROL", StringComparison.Ordinal)
        || move.StateId.StartsWith("MAIDENSUCCUBUS_INVASION", StringComparison.Ordinal);

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
