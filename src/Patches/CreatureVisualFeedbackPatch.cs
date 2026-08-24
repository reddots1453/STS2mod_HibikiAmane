using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
public static class CreatureVisualFeedbackPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance, string trigger) =>
        Safe.Run(() =>
        {
            if (__instance.Visuals is MaidenSuccubusCreatureVisuals visuals)
            {
                visuals.PlayFeedback(trigger);
            }
        }, nameof(CreatureVisualFeedbackPatch));
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
public static class CreatureDeathVisualFeedbackPatch
{
    [HarmonyPrefix]
    public static void Prefix(NCreature __instance) =>
        Safe.Run(() =>
        {
            if (__instance.Visuals is MaidenSuccubusCreatureVisuals visuals)
            {
                visuals.PlayFeedback("Dead");
            }
        }, nameof(CreatureDeathVisualFeedbackPatch));
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.AnimShake))]
public static class CreatureHurtVisualFeedbackPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance) =>
        Safe.Run(() =>
        {
            if (__instance.Visuals is MaidenSuccubusCreatureVisuals visuals)
            {
                visuals.PlayFeedback("Hurt");
            }
        }, nameof(CreatureHurtVisualFeedbackPatch));
}
