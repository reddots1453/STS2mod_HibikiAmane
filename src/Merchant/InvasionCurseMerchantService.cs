using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Invasion;
using MaidenSuccubus.Cards.Curses;

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

    public static async Task<CardModel?> SelectAndRemove(Player player)
    {
        if (player.Character is not MaidenSuccubusCharacter)
        {
            return null;
        }

        CardModel[] selected = GetEligible(player).ToArray();
        if (selected.Length == 0)
        {
            return null;
        }

        foreach (CardModel curse in selected)
        {
            await CardPileCmd.RemoveFromDeck(curse);
        }
        int refund = selected.Length
            * InvasionCurseMerchantConfig.RefundGoldPerCurse;
        await PlayerCmd.GainGold(refund, player);
        MaidenSuccubusMod.Logger.Info(
            $"Merchant removed {selected.Length} invasion curses; "
            + $"refund={refund}; normalRemovalCount="
            + player.ExtraFields.CardShopRemovalsUsed);
        return selected[0];
    }
}
