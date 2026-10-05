using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;

namespace MaidenSuccubus.Core.Desire;

public interface IDesireRuleModifier
{
    decimal ModifyDesireGain(Player player, decimal amount) => amount;

    decimal ModifyDesireCap(Player player, decimal currentCap) =>
        currentCap;

    bool ShouldTriggerDesirePenalty(Player player) => true;
}

public static class DesireRuleModifiers
{
    public static decimal ModifyGain(Player player, decimal amount)
    {
        if (amount <= 0) return amount;
        foreach (var modifier in Enumerate(player))
            amount = modifier.ModifyDesireGain(player, amount);
        return amount;
    }

    public static decimal ModifyCap(Player player, decimal currentCap)
    {
        // Route capacity is the base bonus; content modifiers (including unbounded) follow it.
        if (player.Character is MaidenSuccubusCharacter
            && player.RunState is RunState run && CorruptionQuery.IsMaxHoly(run))
            currentCap += 5m;
        foreach (var modifier in Enumerate(player))
        {
            currentCap = modifier.ModifyDesireCap(player, currentCap);
        }

        return Math.Max(0m, currentCap);
    }

    public static bool ShouldTriggerPenalty(Player player) =>
        Enumerate(player).All(
            modifier => modifier.ShouldTriggerDesirePenalty(player));

    private static IEnumerable<IDesireRuleModifier> Enumerate(Player player)
    {
        if (player.PlayerCombatState != null)
        {
            foreach (var card in PileType.Hand.GetPile(player).Cards)
            {
                if (card is IDesireRuleModifier modifier)
                    yield return modifier;
            }
        }

        foreach (var relic in player.Relics)
        {
            if (relic is IDesireRuleModifier modifier)
            {
                yield return modifier;
            }
        }

        foreach (var power in player.Creature.Powers)
        {
            if (power is IDesireRuleModifier modifier)
            {
                yield return modifier;
            }
        }
    }
}
