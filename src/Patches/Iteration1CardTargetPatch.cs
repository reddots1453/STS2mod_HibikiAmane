using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
public static class Iteration1CardTargetPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        CardModel __instance,
        Creature? target,
        ref bool __result)
    {
        bool updated = __result;
        Safe.Run(() =>
        {
            if (__instance is Stigma)
            {
                updated = target?.IsAlive == true;
                return;
            }

            if (!updated || target?.Monster == null)
                return;

            if (__instance is LureDeep)
            {
                updated = target.Monster.NextMove.Intents
                    .Any(intent => intent is ControlIntent);
            }
            else if (__instance is BiteInvader)
            {
                updated = target.Monster.NextMove.Intents
                    .Any(intent => intent is InvasionIntent);
            }
        }, nameof(Iteration1CardTargetPatch));
        __result = updated;
    }
}

[HarmonyPatch]
internal static class StigmaTargetingStartPatch
{
    internal static bool IsActive { get; private set; }

    private static System.Reflection.MethodBase TargetMethod() =>
        AccessTools.Method(
            typeof(NTargetManager),
            nameof(NTargetManager.StartTargeting),
            [
                typeof(TargetType),
                typeof(Control),
                typeof(TargetMode),
                typeof(Func<bool>),
                typeof(Func<Node, bool>)
            ]);

    [HarmonyPrefix]
    private static void Prefix(Control control)
    {
        IsActive = control is NCard { Model: Stigma };
    }

    internal static void Reset()
    {
        IsActive = false;
    }
}

[HarmonyPatch(typeof(NTargetManager), "AllowedToTargetCreature")]
internal static class StigmaAllowedTargetPatch
{
    [HarmonyPostfix]
    private static void Postfix(Creature creature, ref bool __result)
    {
        if (StigmaTargetingStartPatch.IsActive && creature.IsAlive)
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(CombatState), nameof(CombatState.GetOpponentsOf))]
internal static class StigmaControllerTargetListPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        Creature creature,
        ref IReadOnlyList<Creature> __result)
    {
        ICombatState? combatState = creature.CombatState;
        if (StigmaTargetingStartPatch.IsActive
            && creature.IsPlayer
            && combatState != null)
        {
            __result = combatState.Creatures
                .Where(candidate => candidate.IsAlive)
                .ToArray();
        }
    }
}

[HarmonyPatch(typeof(NTargetManager), "FinishTargeting")]
internal static class StigmaTargetingFinishedPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        StigmaTargetingStartPatch.Reset();
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.TryManualPlay))]
public static class LureDeepInvalidTargetFeedbackPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        CardModel __instance,
        Creature? target,
        bool __result)
    {
        Safe.Run(() =>
        {
            if (__result
                || __instance is not LureDeep
                || target?.Monster == null
                || target.Monster.NextMove.Intents.Any(
                    intent => intent is ControlIntent))
            {
                return;
            }
            TalkCmd.Play(
                new LocString(
                    "cards",
                    "MAIDEN_SUCCUBUS_CARD_LURE_DEEP.invalidTarget"),
                __instance.Owner.Creature,
                VfxColor.White,
                VfxDuration.Short);
        }, nameof(LureDeepInvalidTargetFeedbackPatch));
    }
}
