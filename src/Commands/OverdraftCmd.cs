using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Commands;

public static class OverdraftCmd
{
    public static async Task<bool> Offer(
        PlayerChoiceContext choiceContext,
        CardModel source,
        int armorCost)
    {
        if (MagicArmorCmd.GetAmount(source.Owner) < armorCost)
        {
            return false;
        }

        CardModel? accepted = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            [source],
            source.Owner,
            canSkip: true);
        return accepted is not null
            && await MagicArmorCmd.TrySpend(
                choiceContext,
                source.Owner,
                armorCost,
                source);
    }
}
