using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Invasion;

namespace MaidenSuccubus.Merchant;

public static class InvasionCurseMerchantConfig
{
    // DesignDoc 尚未给出正式数值；只允许通过此显式 Debug 配置调整。
    public static int DebugRefundGold { get; set; } = 25;
}

public static class InvasionCurseMerchantService
{
    public static bool IsEligible(CardModel card) =>
        card.Pile?.Type == PileType.Deck
        && card.Type == CardType.Curse
        && card is IInvasionSourcedCurse sourced
        && !string.IsNullOrWhiteSpace(sourced.SourceMonsterId);

    public static IReadOnlyList<CardModel> GetEligible(Player player) =>
        PileType.Deck.GetPile(player).Cards.Where(IsEligible).ToList();

    public static async Task<CardModel?> SelectAndRemove(Player player)
    {
        if (player.Character is not MaidenSuccubusCharacter)
        {
            return null;
        }

        var prefs = new CardSelectorPrefs(
            CardSelectorPrefs.RemoveSelectionPrompt,
            minCount: 0,
            maxCount: 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
        };
        CardModel? selected = (await CardSelectCmd.FromDeckForRemoval(
            player,
            prefs,
            IsEligible)).FirstOrDefault();
        if (selected == null)
        {
            return null;
        }

        await CardPileCmd.RemoveFromDeck(selected);
        int refund = Math.Max(0, InvasionCurseMerchantConfig.DebugRefundGold);
        if (refund > 0)
        {
            await PlayerCmd.GainGold(refund, player);
        }
        MaidenSuccubusMod.Logger.Info(
            $"Merchant removed invasion curse {selected.Id.Entry}; "
            + $"refund={refund}; normalRemovalCount="
            + player.ExtraFields.CardShopRemovalsUsed);
        return selected;
    }
}
