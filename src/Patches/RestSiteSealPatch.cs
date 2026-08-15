using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Hooks;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Seals;
using MaidenSuccubus.RestSite;
using MaidenSuccubus.Util;
using MaidenSuccubus.Data;

namespace MaidenSuccubus.Patches;

public static class RestSiteSealPatch
{
    [HarmonyPatch(
        typeof(RestSiteOption),
        nameof(RestSiteOption.Generate))]
    [HarmonyPostfix]
    public static void AddRemoveSealedOption(
        Player __0,
        List<RestSiteOption> __result)
    {
        Safe.Run(
            () =>
            {
                if (__0.Character is not MaidenSuccubusCharacter)
                {
                    return;
                }

                if (CombatSealQuery.GetSealedDeckCards(__0).Count > 0)
                {
                    __result.Add(new RemoveSealedCardsRestSiteOption(__0));
                }

                if (Desire.Get(__0) >= 5)
                {
                    __result.Add(new MasturbateRestSiteOption(__0));
                }
            },
            nameof(AddRemoveSealedOption));
    }

    [HarmonyPatch(
        typeof(Hook),
        nameof(Hook.ShouldDisableRemainingRestSiteOptions))]
    [HarmonyPrefix]
    public static bool KeepNormalActionAfterRemoval(
        Player __1,
        ref bool __result)
    {
        bool suppressOriginal = false;
        Safe.Run(() =>
        {
            if (!RestSiteActionPolicy.ConsumePreserveRemainingOptions(__1)) return;
            suppressOriginal = true;
        }, nameof(KeepNormalActionAfterRemoval));
        if (suppressOriginal) __result = false;
        return !suppressOriginal;
    }
}
