using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// Registers minimal, progression-safe dialogue for every ancient until the
/// character-specific writing is complete.  PopulateLocKeys is the common
/// registration point used by normal ancients and The Architect.
/// </summary>
[HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.PopulateLocKeys))]
public static class AncientDialoguePatch
{
    [HarmonyPrefix]
    public static void Prefix(AncientDialogueSet __instance, string ancientEntry)
    {
        Safe.Run(() =>
        {
            string characterEntry = ModelDb.Character<MaidenSuccubusCharacter>().Id.Entry;
            if (__instance.CharacterDialogues.ContainsKey(characterEntry))
            {
                return;
            }

            __instance.CharacterDialogues[characterEntry] = ancientEntry == "THE_ARCHITECT"
                ? ArchitectDialogues()
                : StandardDialogues();
        }, "AncientDialogue.RegisterPlaceholder");
    }

    private static IReadOnlyList<AncientDialogue> StandardDialogues() =>
    [
        new AncientDialogue("", "") { VisitIndex = 0 },
        new AncientDialogue("") { VisitIndex = 1 },
        new AncientDialogue("", "", "") { VisitIndex = 2 },
    ];

    private static IReadOnlyList<AncientDialogue> ArchitectDialogues() =>
    [
        new AncientDialogue("", "", "")
        {
            VisitIndex = 0,
            EndAttackers = ArchitectAttackers.Both,
        },
        new AncientDialogue("", "", "")
        {
            VisitIndex = 1,
            EndAttackers = ArchitectAttackers.Both,
        },
        new AncientDialogue("", "", "")
        {
            VisitIndex = 2,
            EndAttackers = ArchitectAttackers.Both,
        },
    ];
}

/// <summary>
/// Acheron's DustyTome compatibility patch currently throws for this custom
/// character while Darv builds the optional tome reward.  Keep Darv playable
/// by falling back to three ordinary options only when that foreign exception
/// affects MaidenSuccubus; other characters and successful rolls are untouched.
/// </summary>
[HarmonyPatch(typeof(Darv), "GenerateInitialOptions")]
public static class DarvOptionCompatibilityPatch
{
    [HarmonyFinalizer]
    public static Exception? Finalizer(
        Darv __instance,
        ref IReadOnlyList<EventOption> __result,
        Exception? __exception)
    {
        if (__exception == null
            || __instance.Owner?.Character is not MaidenSuccubusCharacter)
        {
            return __exception;
        }

        IReadOnlyList<EventOption>? fallback = null;
        Safe.Run(() =>
        {
            MaidenSuccubusMod.Logger.Warn(
                $"[Darv.Compatibility] Suppressed option-generation exception: {__exception.Message}");
            fallback = __instance.AllPossibleOptions.Take(3).ToArray();
        }, "Darv.CompatibilityFallback");
        if (fallback == null)
        {
            return __exception;
        }

        __result = fallback;
        return null;
    }
}
