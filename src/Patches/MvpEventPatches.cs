using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Bootstrap;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

internal static class MvpEventRules
{
    private static readonly MethodInfo? SetEventFinishedMethod =
        ModCompatibility.FindInstanceMethod(
            typeof(EventModel),
            "SetEventFinished",
            typeof(void),
            typeof(LocString));
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

    internal static bool TryGetCorruptionDelta(
        string optionKey,
        out int delta) => CorruptionByOption.TryGetValue(optionKey, out delta);

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

    internal static int Corruption(EventModel model) =>
        CorruptionQuery.Get((RunState)model.Owner!.RunState);

    internal static async Task CalmLuminousChoir(LuminousChoir model)
    {
        Player owner = model.Owner!;
        int desire = Data.Desire.Get(owner);
        if (desire > 0) await Data.Desire.Modify(owner, -desire);
        CardModel card = owner.RunState.CreateCard<CalmMind>(owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 1.2f, CardPreviewStyle.EventLayout);
        Finish(model, "LUMINOUS_CHOIR.pages.MS_CALM.description");
    }

    internal static async Task SeizeLuminousChoir(LuminousChoir model)
    {
        List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2),
            player: model.Owner!)).ToList();
        await CardPileCmd.RemoveFromDeck(cards);
        Finish(model, "LUMINOUS_CHOIR.pages.MS_SEIZE.description");
    }

    internal static async Task AcceptReflection(Reflections model)
    {
        List<CardModel> cards = model.Owner!.Deck.Cards.Where(c => c.IsUpgradable).ToList();
        cards.StableShuffle(model.Owner.RunState.Rng.Niche);
        foreach (CardModel card in cards.Take(4))
        {
            CardCmd.Upgrade(card, CardPreviewStyle.MessyLayout);
            await Cmd.CustomScaledWait(0.2f, 0.4f);
        }
        Finish(model, "REFLECTIONS.pages.MS_ACCEPT.description");
    }

    internal static async Task SeizeReflection(Reflections model)
    {
        Player owner = model.Owner!;
        CardModel? selected = (await CardSelectCmd.FromDeckGeneric(
            prefs: new CardSelectorPrefs(
                new LocString("events", "REFLECTIONS.pages.MS_COPY_PROMPT"), 1),
            player: owner)).FirstOrDefault();
        if (selected != null)
        {
            for (int i = 0; i < 2; i++)
            {
                CardModel copy = owner.RunState.CloneCard(selected);
                CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(copy, PileType.Deck), 1.2f, CardPreviewStyle.EventLayout);
            }
        }
        Finish(model, "REFLECTIONS.pages.MS_SEIZE.description");
    }

    internal static async Task SeeThroughTreasury(SunkenTreasury model)
    {
        Player owner = model.Owner!;
        await PlayerCmd.GainGold(model.DynamicVars["LargeChestGold"].BaseValue, owner);
        RelicModel relic = RelicFactory.PullNextRelicFromFront(owner).ToMutable();
        await RelicCmd.Obtain(relic, owner);
        Finish(model, "SUNKEN_TREASURY.pages.MS_SEE_THROUGH.description");
    }

    internal static async Task TakeAllTreasury(SunkenTreasury model)
    {
        Player owner = model.Owner!;
        decimal total = model.DynamicVars["SmallChestGold"].BaseValue + model.DynamicVars["LargeChestGold"].BaseValue;
        await PlayerCmd.GainGold(total, owner);
        await CardPileCmd.AddCurseToDeck<Greed>(owner);
        Finish(model, "SUNKEN_TREASURY.pages.MS_TAKE_ALL.description");
    }

    internal static async Task PurifySymbiote(Symbiote model)
    {
        List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1),
            player: model.Owner!)).ToList();
        await CardPileCmd.RemoveFromDeck(cards);
        Finish(model, "SYMBIOTE.pages.MS_PURIFY.description");
    }

    internal static async Task FullySymbiose(Symbiote model)
    {
        EnchantmentModel enchantment = ModelDb.Enchantment<Corrupted>();
        List<CardModel> cards = (await CardSelectCmd.FromDeckForEnchantment(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 2),
            player: model.Owner!, enchantment: enchantment, amount: 1)).ToList();
        foreach (CardModel card in cards)
            CardCmd.Enchant<Corrupted>(card, 1);
        Finish(model, "SYMBIOTE.pages.MS_SYMBIOSE.description");
    }

    private static void Finish(EventModel model, string locKey) =>
        SetEventFinishedMethod?.Invoke(
            model,
            [new LocString("events", locKey)]);
}

[HarmonyPatch]
public static class VanillaEventCorruptionDescriptionPatch
{
    private static readonly PropertyInfo? DescriptionProperty =
        ModCompatibility.FindWritableProperty(
            typeof(EventOption), nameof(EventOption.Description), typeof(LocString));

    public static MethodBase TargetMethod() => AccessTools.Constructor(
        typeof(EventOption),
        [
            typeof(EventModel),
            typeof(Func<Task>),
            typeof(string),
            typeof(IEnumerable<IHoverTip>),
        ]);

    [HarmonyPostfix]
    public static void Postfix(
        EventOption __instance,
        EventModel __0,
        string __2) => Safe.Run(
        () => Decorate(__instance, __0, __2),
        nameof(VanillaEventCorruptionDescriptionPatch));

    private static void Decorate(
        EventOption option,
        EventModel eventModel,
        string optionKey)
    {
        if (!MvpEventRules.Applies(eventModel)
            || !MvpEventRules.TryGetCorruptionDelta(optionKey, out int delta)
            || DescriptionProperty == null
            || DescriptionProperty.GetValue(option) is not LocString original)
        {
            return;
        }

        var decorated = new LocString(
            "events",
            delta > 0
                ? "MAIDENSUCCUBUS_EVENT_CORRUPTION_GAIN.description"
                : "MAIDENSUCCUBUS_EVENT_CORRUPTION_LOSE.description");
        // LocString.Add(LocString) formats immediately.  Populate the original
        // option with the event vars first, otherwise placeholders such as
        // {MaxHp}, {Damage}, and {Heal} are frozen into the wrapper verbatim.
        eventModel.DynamicVars.AddTo(original);
        decorated.Add("Original", original);
        DescriptionProperty.SetValue(option, decorated);
    }
}

internal static class ThresholdEventOptionFactory
{
    internal static EventOption LockedOr(EventModel model, bool unlocked, Func<Task> action, string key) =>
        new(model, unlocked ? action : null, unlocked ? key : key + "_LOCKED");
}

[HarmonyPatch(typeof(LuminousChoir), "GenerateInitialOptions")]
public static class LuminousChoirMvpPatch
{
    public static void Postfix(LuminousChoir __instance, ref IReadOnlyList<EventOption> __result)
    {
        IReadOnlyList<EventOption>? updated = null;
        IReadOnlyList<EventOption> original = __result;
        Safe.Run(() => updated = Append(__instance, original), nameof(LuminousChoirMvpPatch));
        if (updated != null) __result = updated;
    }

    private static IReadOnlyList<EventOption> Append(LuminousChoir __instance, IReadOnlyList<EventOption> original)
    {
        if (!MvpEventRules.Applies(__instance)) return original;
        int c = MvpEventRules.Corruption(__instance);
        List<EventOption> options = original.ToList();
        options.Add(ThresholdEventOptionFactory.LockedOr(__instance, c <= -3,
            () => MvpEventRules.CalmLuminousChoir(__instance), "LUMINOUS_CHOIR.pages.INITIAL.options.MS_CALM"));
        options.Add(ThresholdEventOptionFactory.LockedOr(__instance, c >= 3,
            () => MvpEventRules.SeizeLuminousChoir(__instance), "LUMINOUS_CHOIR.pages.INITIAL.options.MS_SEIZE"));
        return options;
    }
}

[HarmonyPatch(typeof(Reflections), "GenerateInitialOptions")]
public static class ReflectionsMvpPatch
{
    public static void Postfix(Reflections __instance, ref IReadOnlyList<EventOption> __result)
    {
        IReadOnlyList<EventOption>? updated = null;
        IReadOnlyList<EventOption> original = __result;
        Safe.Run(() => updated = Append(__instance, original), nameof(ReflectionsMvpPatch));
        if (updated != null) __result = updated;
    }

    private static IReadOnlyList<EventOption> Append(Reflections __instance, IReadOnlyList<EventOption> original)
    {
        if (!MvpEventRules.Applies(__instance)) return original;
        int c = MvpEventRules.Corruption(__instance);
        List<EventOption> options = original.ToList();
        options.Add(ThresholdEventOptionFactory.LockedOr(__instance, c <= -4,
            () => MvpEventRules.AcceptReflection(__instance), "REFLECTIONS.pages.INITIAL.options.MS_ACCEPT"));
        options.Add(ThresholdEventOptionFactory.LockedOr(__instance, c >= 4,
            () => MvpEventRules.SeizeReflection(__instance), "REFLECTIONS.pages.INITIAL.options.MS_SEIZE"));
        return options;
    }
}

[HarmonyPatch(typeof(SunkenTreasury), "GenerateInitialOptions")]
public static class SunkenTreasuryMvpPatch
{
    public static void Postfix(SunkenTreasury __instance, ref IReadOnlyList<EventOption> __result)
    {
        IReadOnlyList<EventOption>? updated = null;
        IReadOnlyList<EventOption> original = __result;
        Safe.Run(() => updated = Append(__instance, original), nameof(SunkenTreasuryMvpPatch));
        if (updated != null) __result = updated;
    }

    private static IReadOnlyList<EventOption> Append(SunkenTreasury __instance, IReadOnlyList<EventOption> original)
    {
        if (!MvpEventRules.Applies(__instance)) return original;
        int c = MvpEventRules.Corruption(__instance);
        List<EventOption> options = original.ToList();
        options.Add(ThresholdEventOptionFactory.LockedOr(__instance, c <= -4,
            () => MvpEventRules.SeeThroughTreasury(__instance), "SUNKEN_TREASURY.pages.INITIAL.options.MS_SEE_THROUGH"));
        options.Add(ThresholdEventOptionFactory.LockedOr(__instance, c >= 2,
            () => MvpEventRules.TakeAllTreasury(__instance), "SUNKEN_TREASURY.pages.INITIAL.options.MS_TAKE_ALL"));
        return options;
    }
}

[HarmonyPatch(typeof(Symbiote), "GenerateInitialOptions")]
public static class SymbioteMvpPatch
{
    public static void Postfix(Symbiote __instance, ref IReadOnlyList<EventOption> __result)
    {
        IReadOnlyList<EventOption>? updated = null;
        IReadOnlyList<EventOption> original = __result;
        Safe.Run(() => updated = Append(__instance, original), nameof(SymbioteMvpPatch));
        if (updated != null) __result = updated;
    }

    private static IReadOnlyList<EventOption> Append(Symbiote __instance, IReadOnlyList<EventOption> original)
    {
        if (!MvpEventRules.Applies(__instance)) return original;
        int c = MvpEventRules.Corruption(__instance);
        bool canEnchantTwo = __instance.Owner!.Deck.Cards.Count(card =>
            card.Enchantment == null && ModelDb.Enchantment<Corrupted>().CanEnchant(card)) >= 2;
        List<EventOption> options = original.ToList();
        options.Add(ThresholdEventOptionFactory.LockedOr(__instance, c <= -3,
            () => MvpEventRules.PurifySymbiote(__instance), "SYMBIOTE.pages.INITIAL.options.MS_PURIFY"));
        options.Add(ThresholdEventOptionFactory.LockedOr(__instance, c >= 3 && canEnchantTwo,
            () => MvpEventRules.FullySymbiose(__instance), "SYMBIOTE.pages.INITIAL.options.MS_SYMBIOSE"));
        return options;
    }
}

[HarmonyPatch(typeof(EventOption), nameof(EventOption.Chosen))]
public static class VanillaEventCorruptionCompletionPatch
{
    private static readonly FieldInfo? OnChosenField =
        ModCompatibility.FindField(
            typeof(EventOption),
            "<OnChosen>k__BackingField",
            typeof(Func<Task>));
    private static readonly ConditionalWeakTable<EventOption, object> Wrapped = new();

    [HarmonyPrefix]
    public static void Prefix(EventOption __instance) => Safe.Run(
        () => Wrap(__instance),
        nameof(VanillaEventCorruptionCompletionPatch));

    private static void Wrap(EventOption option)
    {
        if (OnChosenField == null
            || Wrapped.TryGetValue(option, out _)
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
        IReadOnlyList<EventOption>? updated = null;
        IReadOnlyList<EventOption> original = __result;
        Safe.Run(() => updated = Append(__instance, original), nameof(WhisperingHollowMvpPatch));
        if (updated != null) __result = updated;
    }

    private static IReadOnlyList<EventOption> Append(WhisperingHollow __instance, IReadOnlyList<EventOption> original)
    {
        if (!MvpEventRules.Applies(__instance)) return original;
        List<EventOption> options = original.ToList();
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
        return options;
    }
}
