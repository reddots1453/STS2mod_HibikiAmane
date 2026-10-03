using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Core.Condemnation;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Commands;

public static class CondemnationCmd
{
    private static readonly WeakInstanceValueScope<Creature, JudgmentCapture> Captures = new();

    internal sealed class JudgmentCapture : IDisposable
    {
        private readonly IDisposable _scope;
        internal decimal Damage { get; set; }
        internal Creature? Dealer { get; set; }

        internal JudgmentCapture(Creature target) => _scope = Captures.Enter(target, this);
        public void Dispose()
        {
            _scope.Dispose();
            if (Damage > 0)
                MaidenSuccubusMod.Logger.Info(
                    $"[FinalJudgment] Captured judgment base damage={Damage}.");
        }
    }

    internal static JudgmentCapture CaptureJudgment(Creature target) => new(target);

    // A copied judgment remains status damage, not a second card damage value.
    // Each receiver still uses native block and damage-reduction rules.
    internal static Task DealJudgmentDamage(
        PlayerChoiceContext context, Creature target, decimal amount, Creature? dealer) =>
        CreatureCmd.Damage(context, target, amount,
            ValueProp.Move | ValueProp.Unpowered, dealer, null, null);

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
        decimal damage = layers * CondemnationPower.DamagePerLayer;
        Creature? dealer = power.Applier;
        Captures.TryGet(target, out JudgmentCapture? capture);
        power.FlashForJudgment();
        await DealJudgmentDamage(choiceContext, target, damage, dealer);
        if (capture != null)
        {
            capture.Damage = damage;
            capture.Dealer = dealer;
        }

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
