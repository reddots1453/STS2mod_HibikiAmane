using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Cards;

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

        OverdraftAcceptChoice accept =
            source.CombatState!.CreateCard<OverdraftAcceptChoice>(source.Owner);
        accept.Configure(armorCost);
        OverdraftDeclineChoice decline =
            source.CombatState.CreateCard<OverdraftDeclineChoice>(source.Owner);

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            [accept, decline],
            source.Owner,
            canSkip: false);
        return selected == accept
            && await MagicArmorCmd.TrySpend(
                choiceContext,
                source.Owner,
                armorCost,
                source);
    }
}
