using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Data;
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

        bool hasIntentCapability =
            IntentAdapterRegistry.GetAdapter(monster) != null
            || monster is IControlIntentProvider
            || monster is IInvasionIntentProvider
            || monster is IDesireIntentProvider;
        if (monster.Creature.IsDead || !hasIntentCapability)
        {
            return;
        }

        var player = monster.CombatState.Players.FirstOrDefault(candidate =>
            candidate.Character is MaidenSuccubusCharacter
            && candidate.Creature.IsAlive);
        if (player == null) return;

        IntentRuntimeState runtime = IntentAdapterRegistry.GetRuntime(monster);

        // Invasion has priority whenever this character is controlled, including
        // control applied by another monster.
        IInvasionIntentProvider? invasion =
            IntentAdapterRegistry.GetProvider<IInvasionIntentProvider>(monster);
        if (ControlQuery.IsControlled(player)
            && !runtime.HasInvaded
            && invasion?.GetInvasionIntent(monster) is { } invasionSpec)
        {
            IntentMoveFactory.SetTransient(
                monster,
                IntentMoveFactory.CreateInvasion(monster, invasionSpec));
            return;
        }

        IControlIntentProvider? control =
            IntentAdapterRegistry.GetProvider<IControlIntentProvider>(monster);
        bool alreadyControlsPlayer = ControlQuery.GetInstances(player)
            .Any(power => ReferenceEquals(power.Applier, monster.Creature));
        if (Desire.Get(player) >= 5
            && !runtime.ControlDisabled
            && !alreadyControlsPlayer
            && control?.GetControlIntent(monster) is { } controlSpec)
        {
            IntentMoveFactory.SetTransient(
                monster,
                IntentMoveFactory.CreateControl(monster, controlSpec));
            return;
        }

        IDesireIntentProvider? desire =
            IntentAdapterRegistry.GetProvider<IDesireIntentProvider>(monster);
        if (desire?.GetDesireIntent(monster) is { } desireSpec
            && runtime.DesireIntentUses < desireSpec.MaxUsesPerCombat)
        {
            IntentMoveFactory.SetTransient(
                monster,
                IntentMoveFactory.CreateDesire(monster, desireSpec));
        }
    }
}
