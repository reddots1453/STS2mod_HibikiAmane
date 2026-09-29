using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Invasion;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.Merchant;

public static class InvasionCurseMerchantConfig
{
    public const int RefundGoldPerCurse = 50;
}

public static class InvasionCurseMerchantService
{
    public static bool IsEligible(CardModel card) =>
        card.Pile?.Type == PileType.Deck
        && card.Type == CardType.Curse
        && card is MSInvasionCurseTemplate;

    public static IReadOnlyList<CardModel> GetEligible(Player player) =>
        PileType.Deck.GetPile(player).Cards.Where(IsEligible).ToList();

    public static int GetEligibleCount(Player player) => GetEligible(player).Count
        + player.Relics.OfType<InternalCondom>().Sum(relic => relic.StoredCount);

    public static async Task<CardModel?> SelectAndRemove(Player player)
    {
        if (player.Character is not MaidenSuccubusCharacter)
        {
            return null;
        }

        CardModel[] selected = GetEligible(player).ToArray();
        InternalCondom[] condoms = player.Relics.OfType<InternalCondom>().ToArray();
        int stored = condoms.Sum(relic => relic.StoredCount);
        if (selected.Length + stored == 0)
        {
            return null;
        }

        foreach (CardModel curse in selected)
        {
            await CardPileCmd.RemoveFromDeck(curse);
        }
        foreach (InternalCondom condom in condoms) condom.StoredCount = 0;
        int refund = (selected.Length + stored)
            * InvasionCurseMerchantConfig.RefundGoldPerCurse;
        await PlayerCmd.GainGold(refund, player);
        MaidenSuccubusMod.Logger.Info(
            $"Merchant removed {selected.Length} deck and {stored} stored invasion curses; "
            + $"refund={refund}; normalRemovalCount="
            + player.ExtraFields.CardShopRemovalsUsed);
        return selected.FirstOrDefault();
    }
}
