using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>Native fallback when RitsuLib does not replace fake-merchant character creation.</summary>
[HarmonyPatch(typeof(NFakeMerchant), "StartCharacterAnimation")]
internal static class FakeMerchantCharacterScalePatch
{
    [HarmonyPrefix]
    private static bool Prefix(NCreatureVisuals visuals)
    {
        bool matched = false;
        Safe.Run(() => matched = MerchantPortraitPresentation.MatchLegacyFake(visuals),
            nameof(FakeMerchantCharacterScalePatch));
        return !matched;
    }
}
