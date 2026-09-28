using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace MaidenSuccubus.Core.Desire;

public interface IDesireRuleModifier
{
    decimal ModifyDesireCap(Player player, decimal currentCap) =>
        currentCap;

    bool ShouldTriggerDesirePenalty(Player player) => true;
}

public static class DesireRuleModifiers
{
    public static decimal ModifyCap(Player player, decimal currentCap)
    {
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
