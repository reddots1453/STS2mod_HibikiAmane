using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Data;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.RollMove))]
internal static class IntentAdapterPatch
{
    [HarmonyPostfix]
    private static void Postfix(MonsterModel __instance) =>
        Safe.Run(() => ApplyPolicy(__instance), "IntentAdapter.RollMove");

    private static void ApplyPolicy(MonsterModel monster)
    {
        if (monster.NextMove.StateId.StartsWith(
            "MAIDENSUCCUBUS_",
            StringComparison.Ordinal))
        {
            return;
        }

        EroticMonsterSpec? spec = EroticAttackCatalog.Get(monster);
        if (monster.Creature.IsDead || spec == null)
        {
            return;
        }
        ArgumentNullException.ThrowIfNull(spec);

        var player = monster.CombatState.Players.FirstOrDefault(candidate =>
            candidate.Character is MaidenSuccubusCharacter
            && candidate.Creature.IsAlive);
        if (player == null) return;

        IntentRuntimeState runtime = IntentAdapterRegistry.GetRuntime(monster);
        if (runtime.ForceStun)
        {
            IntentMoveFactory.ForceStun(monster);
            return;
        }
        if (spec?.Steadfast == true)
        {
            if (!runtime.SteadfastScheduled
                && !monster.Creature.HasPower<SteadfastPower>())
            {
                runtime.SteadfastScheduled = true;
                TaskHelper.RunSafely(PowerCmd.Apply<SteadfastPower>(
                    new ThrowingPlayerChoiceContext(),
                    monster.Creature,
                    1m,
                    monster.Creature,
                    null));
            }
            return;
        }
        EroticMonsterSpec catalog = spec!;

        MagicArmorPower? armor = TransformationCmd.GetArmor(player.Creature);
        if (armor == null || !TransformationCmd.IsTransformed(player.Creature))
        {
            return;
        }

        int intentTurn = monster.CombatState.RoundNumber
            + (monster.CombatState.CurrentSide == CombatSide.Enemy ? 1 : 0);
        if (runtime.LastNaturalRollTurn == intentTurn)
        {
            return;
        }
        runtime.LastNaturalRollTurn = intentTurn;

        float chance = armor.Amount switch
        {
            >= 3 => 0.10f,
            2 => 0.30f,
            1 => 0.50f,
            _ => 0f,
        };
        if (chance <= 0f || monster.RunRng.MonsterAi.NextFloat() >= chance)
        {
            return;
        }

        var candidates = new List<(EroticIntentKind Kind, int Weight)>();
        if (catalog.Desire is { } desire
            && runtime.DesireIntentUses < desire.MaxUsesPerCombat)
        {
            candidates.Add((EroticIntentKind.Desire,
                catalog.Preferred == EroticIntentKind.Desire ? 3 : 1));
        }
        bool alreadyControlsPlayer = ControlQuery.GetInstances(player)
            .Any(power => ReferenceEquals(power.Applier, monster.Creature));
        if (catalog.Control is { } control
            && !runtime.ControlDisabled
            && !alreadyControlsPlayer
            && runtime.ControlIntentUses < control.MaxUsesPerCombat)
        {
            candidates.Add((EroticIntentKind.Control,
                catalog.Preferred == EroticIntentKind.Control ? 3 : 1));
        }
        if (catalog.Invasion is { } invasion
            && ControlQuery.IsControlled(player)
            && !runtime.ControlDisabled
            && runtime.InvasionIntentUses < invasion.MaxUsesPerCombat)
        {
            candidates.Add((EroticIntentKind.Invasion,
                catalog.Preferred == EroticIntentKind.Invasion ? 3 : 1));
        }
        if (candidates.Count == 0)
        {
            return;
        }

        EroticIntentKind selected = candidates.Count == 1
            ? candidates[0].Kind
            : SelectWeighted(monster, candidates);
        MoveState? move = selected switch
        {
            EroticIntentKind.Desire when catalog.Desire != null =>
                IntentMoveFactory.CreateDesire(monster, catalog.Desire),
            EroticIntentKind.Control when catalog.Control != null =>
                IntentMoveFactory.CreateControl(monster, catalog.Control),
            EroticIntentKind.Invasion when catalog.Invasion != null =>
                IntentMoveFactory.CreateInvasion(monster, catalog.Invasion),
            _ => null,
        };
        if (move != null)
        {
            runtime.Increment(selected);
            IntentMoveFactory.SetTransient(monster, move);
        }
    }

    private static EroticIntentKind SelectWeighted(
        MonsterModel monster,
        IReadOnlyList<(EroticIntentKind Kind, int Weight)> candidates)
    {
        int total = candidates.Sum(candidate => candidate.Weight);
        int roll = monster.RunRng.MonsterAi.NextInt(total);
        foreach ((EroticIntentKind kind, int weight) in candidates
            .OrderBy(candidate => candidate.Kind))
        {
            if (roll < weight) return kind;
            roll -= weight;
        }
        return candidates[^1].Kind;
    }

}
