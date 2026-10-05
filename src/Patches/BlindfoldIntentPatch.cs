using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch]
public static class BlindfoldIntentPatch
{
    private sealed record VisibilityBeforeHide(bool Visible);
    private static readonly ConditionalWeakTable<NCreature, VisibilityBeforeHide> Hidden = new();
    public static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(NCreature), "RevealIntents")
        ?? throw new MissingMethodException(typeof(NCreature).FullName, "RevealIntents");

    public static bool Prefix(NCreature __instance, ref Task __result)
    {
        bool suppressed = false;
        Safe.Run(
            () =>
            {
                if (BlindfoldPresentation.HidesIntents(__instance.Entity))
                {
                    Hidden.GetValue(__instance, node => new(node.IntentContainer.Visible));
                    // Hide the whole Control: alpha=0 alone leaves hover hitboxes active.
                    __instance.IntentContainer.Visible = false;
                    __instance.HideHoverTips();
                    suppressed = true;
                }
                else if (Hidden.TryGetValue(__instance, out var previous))
                {
                    __instance.IntentContainer.Visible = previous.Visible;
                    Hidden.Remove(__instance);
                }
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

[HarmonyPatch(typeof(NIntent), "OnHovered")]
public static class BlindfoldIntentHoverPatch
{
    private static readonly FieldInfo? OwnerField = AccessTools.Field(typeof(NIntent), "_owner");
    private static bool Prepare()
    {
        bool available = OwnerField?.FieldType == typeof(Creature);
        if (!available) MaidenSuccubusMod.Logger.Warn("[Blindfold] Intent owner field unavailable; direct hover guard skipped.");
        return available;
    }

    public static bool Prefix(NIntent __instance)
    {
        bool hide = false;
        Safe.Run(() => hide = BlindfoldPresentation.HidesIntents(
            OwnerField?.GetValue(__instance) as Creature), nameof(BlindfoldIntentHoverPatch));
        return !hide;
    }
}

// Creature focus/controller tooltips otherwise disclose every intent despite hidden icons.
[HarmonyPatch(typeof(Creature), nameof(Creature.HoverTips), MethodType.Getter)]
public static class BlindfoldCreatureHoverPatch
{
    public static bool Prefix(Creature __instance, ref IEnumerable<IHoverTip> __result)
    {
        IEnumerable<IHoverTip>? replacement = null;
        Safe.Run(() =>
        {
            if (BlindfoldPresentation.HidesIntents(__instance))
                replacement = BlindfoldPresentation.PowerTips(__instance);
        }, nameof(BlindfoldCreatureHoverPatch));
        if (replacement == null) return true;
        __result = replacement;
        return false;
    }
}
