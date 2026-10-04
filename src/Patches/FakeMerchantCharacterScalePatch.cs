using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>Event-local scaling; combat visuals and their internal animation scale stay intact.</summary>
[HarmonyPatch(typeof(NFakeMerchant), "StartCharacterAnimation")]
internal static class FakeMerchantCharacterScalePatch
{
    private static readonly StringName OriginalScale = "maiden_fake_merchant_original_scale";

    [HarmonyPostfix]
    private static void Postfix(NCreatureVisuals visuals) => Safe.Run(() =>
    {
        if (visuals is not MaidenSuccubusCreatureVisuals) return;
        // This method may be called again by another mod. Always use the first
        // event scale rather than repeatedly halving the current instance.
        if (!visuals.HasMeta(OriginalScale)) visuals.SetMeta(OriginalScale, visuals.Scale);
        visuals.Scale = visuals.GetMeta(OriginalScale).AsVector2() * .5f;
    }, nameof(FakeMerchantCharacterScalePatch));
}
