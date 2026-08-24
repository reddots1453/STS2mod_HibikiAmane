using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
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
