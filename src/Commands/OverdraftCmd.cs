using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Transformation;

namespace MaidenSuccubus.Commands;

public static class OverdraftCmd
{
    public static async Task<bool> Offer(
        PlayerChoiceContext choiceContext,
        CardModel source,
        int armorCost)
    {
        if (armorCost != 1)
            throw new ArgumentOutOfRangeException(
                nameof(armorCost),
                "魔力解放固定支付1层魔力增幅或魔装耐久。");
        return await TransformationCmd.PayOverdraft(
            choiceContext, source.Owner.Creature, source);
    }
}
