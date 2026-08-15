using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch]
public static class BlindfoldIntentPatch
{
    public static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(NCreature), "RevealIntents")
        ?? throw new MissingMethodException(typeof(NCreature).FullName, "RevealIntents");

    public static bool Prefix(NCreature __instance, ref Task __result)
    {
        bool hide = false;
        Safe.Run(
            () => hide = __instance.Entity.IsMonster
                && __instance.Entity.CombatState is CombatState state
                && state.Players.Any(
                    player => player.Relics.OfType<Blindfold>().Any()),
            nameof(BlindfoldIntentPatch));

        if (!hide)
        {
            return true;
        }

        bool suppressed = false;
        Safe.Run(
            () =>
            {
                __instance.IntentContainer.Modulate = Colors.Transparent;
                suppressed = true;
            },
            nameof(BlindfoldIntentPatch));
        if (!suppressed)
        {
            return true;
        }
        __result = Task.CompletedTask;
        return false;
    }
}
