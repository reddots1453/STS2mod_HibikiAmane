using System.Reflection;
using System.Runtime.CompilerServices;
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
    private static readonly IReadOnlyDictionary<string, int> CorruptionByOption =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["ABYSSAL_BATHS.pages.INITIAL.options.IMMERSE"] = 1,
            ["ABYSSAL_BATHS.pages.INITIAL.options.ABSTAIN"] = -1,
            ["AROMA_OF_CHAOS.pages.INITIAL.options.LET_GO"] = 1,
            ["AROMA_OF_CHAOS.pages.INITIAL.options.MAINTAIN_CONTROL"] = -1,
            ["DOORS_OF_LIGHT_AND_DARK.pages.INITIAL.options.DARK"] = 1,
            ["DOORS_OF_LIGHT_AND_DARK.pages.INITIAL.options.LIGHT"] = -1,
            ["FIELD_OF_MAN_SIZED_HOLES.pages.INITIAL.options.ENTER_YOUR_HOLE"] = 1,
            ["FIELD_OF_MAN_SIZED_HOLES.pages.INITIAL.options.RESIST"] = -1,
            ["SPIRIT_GRAFTER.pages.INITIAL.options.LET_IT_IN"] = 1,
            ["SPIRIT_GRAFTER.pages.INITIAL.options.REJECTION"] = -1,
            ["SYMBIOTE.pages.INITIAL.options.APPROACH"] = 1,
            ["SYMBIOTE.pages.INITIAL.options.KILL_WITH_FIRE"] = -1,
            ["WATERLOGGED_SCRIPTORIUM.pages.INITIAL.options.TENTACLE_QUILL"] = 1,
            ["WELLSPRING.pages.INITIAL.options.BATHE"] = -1,
            ["WHISPERING_HOLLOW.pages.INITIAL.options.HUG"] = 1,
        };

    internal static bool TryGetRun(out RunState runState)
    {
        runState = RunManager.Instance.DebugOnlyGetState()!;
        return runState is not null
            && runState.Players.Any(player =>
                player.Character is MaidenSuccubusCharacter);
    }

    internal static Task ApplyAfterSuccessfulOption(string optionKey)
    {
        if (CorruptionByOption.TryGetValue(optionKey, out int delta)
            && TryGetRun(out RunState runState))
        {
            CorruptionCmd.Modify(
                runState,
                delta,
                new CorruptionChangeSource($"vanilla_event.{optionKey}"));
        }
        return Task.CompletedTask;
    }

    internal static bool Applies(EventModel model) =>
        model.Owner?.Character is MaidenSuccubusCharacter
        && model.Owner.RunState is RunState;

    internal static async Task PurifyWhisperingHollow(WhisperingHollow model)
    {
        if (model.Owner == null) return;
        await PlayerCmd.GainGold(300, model.Owner);
        Finish(model, "WHISPERING_HOLLOW.pages.MS_PURIFY.description");
    }

    internal static async Task AbsorbWhisperingHollow(WhisperingHollow model)
    {
        if (model.Owner == null) return;
        await RelicCmd.Obtain<WitheredTreeSoul>(model.Owner);
        await CardPileCmd.AddCurseToDeck<Decay>(model.Owner);
        Finish(model, "WHISPERING_HOLLOW.pages.MS_ABSORB.description");
    }

    private static void Finish(EventModel model, string locKey) =>
        AccessTools.Method(typeof(EventModel), "SetEventFinished")?.Invoke(
            model,
            [new LocString("events", locKey)]);
}

[HarmonyPatch(typeof(EventOption), nameof(EventOption.Chosen))]
public static class VanillaEventCorruptionCompletionPatch
{
    private static readonly FieldInfo OnChosenField =
        AccessTools.Field(typeof(EventOption), "<OnChosen>k__BackingField");
    private static readonly ConditionalWeakTable<EventOption, object> Wrapped = new();

    [HarmonyPrefix]
    public static void Prefix(EventOption __instance) => Safe.Run(
        () => Wrap(__instance),
        nameof(VanillaEventCorruptionCompletionPatch));

    private static void Wrap(EventOption option)
    {
        if (Wrapped.TryGetValue(option, out _)
            || OnChosenField.GetValue(option) is not Func<Task> original)
        {
            return;
        }

        string optionKey = option.TextKey;
        Func<Task> wrapped = async () =>
        {
            await original();
            await MvpEventRules.ApplyAfterSuccessfulOption(optionKey);
        };
        OnChosenField.SetValue(option, wrapped);
        Wrapped.Add(option, new object());
    }
}

[HarmonyPatch(typeof(WhisperingHollow), "GenerateInitialOptions")]
public static class WhisperingHollowMvpPatch
{
    public static void Postfix(
        WhisperingHollow __instance,
        ref IReadOnlyList<EventOption> __result)
    {
        if (!MvpEventRules.Applies(__instance)) return;
        List<EventOption> options = __result.ToList();
        int corruption = CorruptionQuery.Get((RunState)__instance.Owner!.RunState);
        options.Add(new EventOption(
            __instance,
            corruption <= -2
                ? () => MvpEventRules.PurifyWhisperingHollow(__instance)
                : null,
            corruption <= -2
                ? "WHISPERING_HOLLOW.pages.INITIAL.options.MS_PURIFY"
                : "WHISPERING_HOLLOW.pages.INITIAL.options.MS_PURIFY_LOCKED"));
        options.Add(new EventOption(
            __instance,
            corruption >= 4
                ? () => MvpEventRules.AbsorbWhisperingHollow(__instance)
                : null,
            corruption >= 4
                ? "WHISPERING_HOLLOW.pages.INITIAL.options.MS_ABSORB"
                : "WHISPERING_HOLLOW.pages.INITIAL.options.MS_ABSORB_LOCKED"));
        __result = options;
    }
}
