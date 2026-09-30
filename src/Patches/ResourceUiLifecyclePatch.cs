using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>Hide in the same call as the native top bar/modal, before the next render.</summary>
[HarmonyPatch(typeof(NTopBar), nameof(NTopBar.AnimHide))]
internal static class ResourceUiLifecyclePatch
{
    [HarmonyPostfix]
    internal static void Postfix(NTopBar __instance) => Safe.Run(() => Hide(__instance), nameof(ResourceUiLifecyclePatch));

    internal static void Hide(Node root)
    {
        foreach (Node node in root.GetChildren())
        {
            if (node is MaidenSuccubus.UI.MaidenSidebarRail or MaidenSuccubus.UI.CorruptionMeter
                or MaidenSuccubus.UI.DesireMeter or MaidenSuccubus.UI.TemptationMeter)
                ((Control)node).Visible = false;
        }
    }
}

[HarmonyPatch(typeof(NModalContainer), nameof(NModalContainer.Add))]
internal static class ResourceUiModalPatch
{
    [HarmonyPostfix]
    internal static void Postfix() => Safe.Run(() =>
    {
        if (MegaCrit.Sts2.Core.Nodes.NRun.Instance?.GlobalUi.TopBar is { } topBar)
            ResourceUiLifecyclePatch.Hide(topBar);
    }, nameof(ResourceUiModalPatch));
}
