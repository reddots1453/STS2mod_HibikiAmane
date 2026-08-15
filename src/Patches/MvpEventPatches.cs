using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

internal static class MvpEventRules
{
    internal static bool Applies(EventModel model) =>
        model.Owner?.Character is MaidenSuccubusCharacter
        && model.Owner.RunState is RunState;

    internal static Task ModifyCorruption(EventModel model, int delta)
    {
        if (model.Owner?.RunState is RunState runState)
        {
            CorruptionCmd.Modify(
                runState,
                delta,
                new CorruptionChangeSource($"event.{model.Id.Entry}"));
        }
        return Task.CompletedTask;
    }

    internal static async Task ModifyBoth(
        EventModel model,
        int corruption,
        int desire)
    {
        await ModifyCorruption(model, corruption);
        if (model.Owner != null)
        {
            await Data.Desire.Modify(model.Owner, desire);
        }
    }

    internal static async Task PurifyWhisperingHollow(WhisperingHollow model)
    {
        if (model.Owner == null)
        {
            return;
        }
        await PlayerCmd.GainGold(300, model.Owner);
        await CardPileCmd.AddCurseToDeck<Decay>(model.Owner);
        Finish(model, "WHISPERING_HOLLOW.pages.MS_PURIFY.description");
    }

    internal static async Task AbsorbWhisperingHollow(WhisperingHollow model)
    {
        if (model.Owner == null)
        {
            return;
        }
        await RelicCmd.Obtain<WitheredTreeSoul>(model.Owner);
        Finish(model, "WHISPERING_HOLLOW.pages.MS_ABSORB.description");
    }

    private static void Finish(EventModel model, string locKey)
    {
        AccessTools.Method(typeof(EventModel), "SetEventFinished")?.Invoke(
            model,
            [new LocString("events", locKey)]);
    }
}

[HarmonyPatch(typeof(DoorsOfLightAndDark), "GenerateInitialOptions")]
public static class DoorsOfLightAndDarkMvpPatch
{
    public static void Postfix(
        DoorsOfLightAndDark __instance,
        IReadOnlyList<EventOption> __result)
    {
        Safe.Run(
            () =>
            {
                if (!MvpEventRules.Applies(__instance))
                {
                    return;
                }
                foreach (EventOption option in __result)
                {
                    if (option.TextKey.EndsWith(".LIGHT", StringComparison.Ordinal))
                    {
                        option.BeforeChosen += _ =>
                            MvpEventRules.ModifyCorruption(__instance, -1);
                    }
                    else if (option.TextKey.EndsWith(".DARK", StringComparison.Ordinal))
                    {
                        option.BeforeChosen += _ =>
                            MvpEventRules.ModifyCorruption(__instance, 1);
                    }
                }
            },
            nameof(DoorsOfLightAndDarkMvpPatch));
    }
}

[HarmonyPatch(typeof(AromaOfChaos), "GenerateInitialOptions")]
public static class AromaOfChaosMvpPatch
{
    public static void Postfix(
        AromaOfChaos __instance,
        IReadOnlyList<EventOption> __result)
    {
        Safe.Run(
            () =>
            {
                if (!MvpEventRules.Applies(__instance))
                {
                    return;
                }
                foreach (EventOption option in __result)
                {
                    if (option.TextKey.EndsWith(".LET_GO", StringComparison.Ordinal))
                    {
                        option.BeforeChosen += _ =>
                            MvpEventRules.ModifyBoth(__instance, 1, 1);
                    }
                    else if (option.TextKey.EndsWith(".MAINTAIN_CONTROL", StringComparison.Ordinal))
                    {
                        option.BeforeChosen += _ =>
                            MvpEventRules.ModifyBoth(__instance, -1, -1);
                    }
                }
            },
            nameof(AromaOfChaosMvpPatch));
    }
}

[HarmonyPatch(typeof(WhisperingHollow), "GenerateInitialOptions")]
public static class WhisperingHollowMvpPatch
{
    public static void Postfix(
        WhisperingHollow __instance,
        ref IReadOnlyList<EventOption> __result)
    {
        IReadOnlyList<EventOption> result = __result;
        Safe.Run(
            () =>
            {
                if (!MvpEventRules.Applies(__instance))
                {
                    return;
                }

                List<EventOption> options = result.ToList();
                EventOption? hug = options.FirstOrDefault(option =>
                    option.TextKey.EndsWith(".HUG", StringComparison.Ordinal));
                if (hug != null)
                {
                    hug.BeforeChosen += _ =>
                        MvpEventRules.ModifyCorruption(__instance, 1);
                }

                bool canPurify = __instance.Owner?.RunState is RunState run
                    && CorruptionQuery.Get(run) <= -2;
                options.Add(new EventOption(
                    __instance,
                    canPurify
                        ? () => MvpEventRules.PurifyWhisperingHollow(__instance)
                        : null,
                    canPurify
                        ? "WHISPERING_HOLLOW.pages.INITIAL.options.MS_PURIFY"
                        : "WHISPERING_HOLLOW.pages.INITIAL.options.MS_PURIFY_LOCKED"));

                bool canAbsorb = __instance.Owner?.RunState is RunState run2
                    && CorruptionQuery.Get(run2) >= 4;
                options.Add(new EventOption(
                    __instance,
                    canAbsorb
                        ? () => MvpEventRules.AbsorbWhisperingHollow(__instance)
                        : null,
                    canAbsorb
                        ? "WHISPERING_HOLLOW.pages.INITIAL.options.MS_ABSORB"
                        : "WHISPERING_HOLLOW.pages.INITIAL.options.MS_ABSORB_LOCKED"));
                result = options;
            },
            nameof(WhisperingHollowMvpPatch));
        __result = result;
    }
}
