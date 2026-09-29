using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Cards;

/// <summary>
/// Combat-only card annotation for 咒印传染. This deliberately does not use
/// CardModel.Enchantment, so it can coexist with a card's one enchantment slot.
/// </summary>
public static class CurseInfectionStatus
{
    internal const string SaveKey = nameof(CurseInfection.CurseInfectionAnnotationMarker);
    public static bool Has(CardModel card) =>
        card.Keywords.Contains(CurseInfectionKeyword.Value);

    public static bool TryApply(CardModel card)
    {
        if (card is CurseInfection || Has(card)) return false;
        card.AddKeyword(CurseInfectionKeyword.Value);
        return true;
    }

    public static bool TryApplyToRandomHandCard(Player player)
    {
        CardModel? target = PileType.Hand.GetPile(player).Cards
            .Where(card => card is not CurseInfection && !Has(card))
            .ToList()
            .StableShuffle(player.RunState.Rng.CombatCardSelection)
            .FirstOrDefault();
        return target != null && TryApply(target);
    }

    public static async Task ResolveAfterExhaust(
        PlayerChoiceContext context,
        CardModel card)
    {
        if (!Has(card)) return;
        await CardPileCmd.Draw(context, 2, card.Owner);
        TryApplyToRandomHandCard(card.Owner);
    }
}

// CardModel does not serialize arbitrary runtime keywords. Store this one
// combat-only annotation in the native saved-properties payload so combat
// saves and multiplayer packets restore the same exhaust behavior.
[HarmonyPatch]
internal static class CurseInfectionSerializationPatch
{
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.ToSerializable))]
    [HarmonyPostfix]
    private static void Save(CardModel __instance, SerializableCard __result) =>
        Safe.Run(() =>
        {
            using (ControlQuery.SuppressPresentation())
            {
                if (!CurseInfectionStatus.Has(__instance)) return;
            }
            __result.Props ??= new SavedProperties();
            __result.Props.bools ??= [];
            if (__result.Props.bools.Any(entry => entry.name == CurseInfectionStatus.SaveKey)) return;
            __result.Props.bools.Add(new SavedProperties.SavedProperty<bool>(
                CurseInfectionStatus.SaveKey, true));
        }, "CurseInfection.Save");

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.FromSerializable))]
    [HarmonyPostfix]
    private static void Restore(SerializableCard __0, CardModel __result) =>
        Safe.Run(() =>
        {
            if (__0.Props?.bools?.Any(entry =>
                    entry.name == CurseInfectionStatus.SaveKey && entry.value) == true)
                CurseInfectionStatus.TryApply(__result);
        }, "CurseInfection.Restore");
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardExhausted))]
public static class CurseInfectionExhaustPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref Task __result, PlayerChoiceContext __1, CardModel __2)
    {
        Task original = __result;
        Task wrapped = original;
        Safe.Run(() => wrapped = ResolveAfterOriginal(original, __1, __2),
            nameof(CurseInfectionExhaustPatch));
        __result = wrapped;
    }

    private static async Task ResolveAfterOriginal(
        Task original,
        PlayerChoiceContext context,
        CardModel card)
    {
        await original;
        try
        {
            await CurseInfectionStatus.ResolveAfterExhaust(context, card);
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Warn(
                $"[CurseInfection.Resolve] {ex.Message}\n{ex.StackTrace}");
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile),
    [typeof(PileType), typeof(MegaCrit.Sts2.Core.Entities.Creatures.Creature)])]
public static class CurseInfectionCardTextPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref string __result)
    {
        string result = __result;
        Safe.Run(() =>
        {
            if (!CurseInfectionStatus.Has(__instance)) return;
            string extra = new LocString(
                "static_hover_tips",
                "MAIDENSUCCUBUS_CURSE_INFECTION.extraCardText").GetFormattedText();
            result = string.Join('\n', result, $"[purple]{extra}[/purple]");
        }, nameof(CurseInfectionCardTextPatch));
        __result = result;
    }
}

