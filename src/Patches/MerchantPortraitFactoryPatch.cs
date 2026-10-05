using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MaidenSuccubus.Characters;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;
using STS2RitsuLib.Scaffolding.Characters.Visuals;

namespace MaidenSuccubus.Patches;

// This shared RitsuLib factory is used by both the native shop and its fake-merchant replacement.
// A direct ImageTexture avoids a scene depending on a previously saved user:// texture.
[HarmonyPatch(typeof(ModWorldSceneVisualNodeFactory), nameof(ModWorldSceneVisualNodeFactory.TryInstantiateMerchantCharacter))]
internal static class MerchantPortraitFactoryPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CharacterModel __0, ref NMerchantCharacter? __result)
    {
        if (__0 is not MaidenSuccubusCharacter) return true;
        MaidenSuccubusMerchantCharacter? portrait = null;
        Safe.Run(() => portrait = MerchantPortraitPresentation.TryCreate(), nameof(MerchantPortraitFactoryPatch));
        if (portrait == null) return true;
        __result = portrait;
        return false;
    }
}

[HarmonyPatch(typeof(NFakeMerchant), "AfterRoomIsLoaded")]
internal static class FakeMerchantPortraitLayoutPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(NFakeMerchant __instance) => Safe.Run(() =>
    {
        var container = __instance.GetNodeOrNull<Godot.Control>("%CharacterContainer");
        if (container == null) return;
        foreach (Godot.Node child in container.GetChildren())
        {
            if (child is MaidenSuccubusMerchantCharacter merchant)
                MerchantPortraitPresentation.MatchFakeScale(__instance, merchant);
            else if (child is MaidenSuccubusCreatureVisuals legacy)
                MerchantPortraitPresentation.MatchLegacyFake(legacy);
        }
    }, nameof(FakeMerchantPortraitLayoutPatch));
}

// Static portraits have no Spine animation to start. All other characters keep native/library playback.
[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
internal static class MaidenMerchantStaticAnimationPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First + 1)]
    private static bool Prefix(NMerchantCharacter __instance)
    {
        bool runNative = true;
        Safe.Run(() => runNative = __instance is not MaidenSuccubusMerchantCharacter,
            nameof(MaidenMerchantStaticAnimationPatch));
        return runNative;
    }
}
