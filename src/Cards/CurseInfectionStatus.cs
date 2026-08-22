using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Cards;

/// <summary>
/// Combat-only card annotation for 咒印传染. This deliberately does not use
/// CardModel.Enchantment, so it can coexist with a card's one enchantment slot.
/// </summary>
public static class CurseInfectionStatus
{
    private sealed class Marker;

    private static readonly ConditionalWeakTable<CardModel, Marker> Marked = new();

    public static bool Has(CardModel card) => Marked.TryGetValue(card, out _);

    public static bool TryApply(CardModel card)
    {
        if (card is CurseInfection || Has(card)) return false;
        Marked.Add(card, new Marker());
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

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardExhausted))]
public static class CurseInfectionExhaustPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref Task __result, PlayerChoiceContext __1, CardModel __2)
    {
        Task original = __result;
        __result = ResolveAfterOriginal(original, __1, __2);
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
        if (!CurseInfectionStatus.Has(__instance)) return;
        string extra = new LocString(
            "static_hover_tips",
            "MAIDENSUCCUBUS_CURSE_INFECTION.extraCardText").GetFormattedText();
        __result = string.Join('\n', __result, $"[purple]{extra}[/purple]");
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
public static class CurseInfectionHoverTipPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (CurseInfectionStatus.Has(__instance))
            __result = __result.Append(
                CardHoverTipSupport.Static("MAIDENSUCCUBUS_CURSE_INFECTION"));
    }
}
