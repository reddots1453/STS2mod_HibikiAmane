using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Core.Condemnation;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Commands;

public static class CondemnationCmd
{
    public static Task<CondemnationPower?> Apply(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource) =>
        PowerCmd.Apply<CondemnationPower>(
            choiceContext,
            target,
            amount,
            applier,
            cardSource);

    public static async Task<bool> Judge(
        PlayerChoiceContext choiceContext,
        Creature target,
        bool force = false)
    {
        CondemnationPower? power = target.GetPower<CondemnationPower>();
        if (power == null || power.Amount <= 0
            || (!force && power.Amount < CondemnationPower.JudgmentThreshold))
        {
            return false;
        }

        int layers = power.Amount;
        power.FlashForJudgment();
        await CreatureCmd.Damage(
            choiceContext,
            target,
            layers * CondemnationPower.DamagePerLayer,
            ValueProp.Move | ValueProp.Unpowered,
            power.Applier,
            null,
            null);

        if (target.HasPower<CondemnationPower>()
            && CondemnationRules.ShouldClear(power, target))
        {
            await PowerCmd.Remove(power);
        }

        return true;
    }

    public static async Task Transfer(
        PlayerChoiceContext choiceContext,
        Creature source,
        Creature target,
        Creature? applier,
        CardModel? cardSource)
    {
        CondemnationPower? sourcePower = source.GetPower<CondemnationPower>();
        if (sourcePower == null || sourcePower.Amount <= 0 || source == target)
        {
            return;
        }

        int amount = sourcePower.Amount;
        await PowerCmd.Remove(sourcePower);
        await Apply(choiceContext, target, amount, applier, cardSource);
    }

    public static async Task<int> ConvertDebuffs(
        PlayerChoiceContext choiceContext,
        Creature target,
        Creature? applier,
        CardModel? cardSource)
    {
        int amount = 0;
        VulnerablePower? vulnerable = target.GetPower<VulnerablePower>();
        WeakPower? weak = target.GetPower<WeakPower>();
        if (vulnerable != null)
        {
            amount += vulnerable.Amount;
            await PowerCmd.Remove(vulnerable);
        }
        if (weak != null)
        {
            amount += weak.Amount;
            await PowerCmd.Remove(weak);
        }
        if (amount > 0)
        {
            await Apply(choiceContext, target, amount, applier, cardSource);
        }
        return amount;
    }
}
