using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Cards;

namespace MaidenSuccubus.Core.Transformation;

public static class TransformationCmd
{
    public const int MaxArmor = 3;

    public static bool IsTransformed(Creature creature)
    {
        foreach (var power in creature.Powers)
        {
            if (power is ImmaculateRobePower)
            {
                return true;
            }
        }
        return false;
    }

    public static MagicArmorPower? GetArmor(Creature creature)
    {
        foreach (var power in creature.Powers)
        {
            if (power is MagicArmorPower armor)
            {
                return armor;
            }
        }
        return null;
    }

    public static MagicAmplificationPower? GetAmplification(Creature creature)
    {
        foreach (var power in creature.Powers)
        {
            if (power is MagicAmplificationPower amplification)
            {
                return amplification;
            }
        }
        return null;
    }

    public static decimal ApplyAmplificationToDelayedValue(
        Creature creature,
        CardModel source,
        decimal value)
    {
        if (GetAmplification(creature)?.IsAmplifying(source) != true)
        {
            return value;
        }
        decimal multiplier = source is IDoubleMagicAmplification
            || (source.Enchantment != null
                && creature.HasPower<TacticalCorePower>())
                ? 2m
                : 1.5m;
        return value * multiplier;
    }

    public static async Task EnterImmaculateRobe(
        PlayerChoiceContext choiceContext,
        Creature creature,
        CardModel? source)
    {
        ImmaculateRobePower? form = creature.Powers
            .OfType<ImmaculateRobePower>()
            .FirstOrDefault();
        if (form == null)
        {
            await PowerCmd.Apply<ImmaculateRobePower>(
                choiceContext, creature, 1m, creature, source);
        }

        MagicArmorPower? oldArmor = GetArmor(creature);
        if (oldArmor != null)
        {
            oldArmor.SuppressFormRemoval = true;
            await PowerCmd.Remove(oldArmor);
        }
        await PowerCmd.Apply<MagicArmorPower>(
            choiceContext, creature, MaxArmor, creature, source);
    }

    public static async Task<bool> GainArmor(
        PlayerChoiceContext choiceContext,
        Creature creature,
        int amount,
        CardModel? source)
    {
        if (amount <= 0 || !IsTransformed(creature))
        {
            return false;
        }

        MagicArmorPower? armor = GetArmor(creature);
        int current = armor?.Amount ?? 0;
        int gain = Math.Min(amount, MaxArmor - current);
        if (gain <= 0)
        {
            return false;
        }

        if (armor == null)
        {
            await PowerCmd.Apply<MagicArmorPower>(
                choiceContext, creature, gain, creature, source);
        }
        else
        {
            await PowerCmd.ModifyAmount(
                choiceContext, armor, gain, creature, source);
        }
        return true;
    }

    public static async Task Exit(
        PlayerChoiceContext choiceContext,
        Creature creature)
    {
        MagicArmorPower? armor = GetArmor(creature);
        if (armor != null)
        {
            armor.SuppressFormRemoval = true;
            await PowerCmd.Remove(armor);
        }
        ImmaculateRobePower? form = creature.Powers
            .OfType<ImmaculateRobePower>()
            .FirstOrDefault();
        if (form != null)
        {
            await PowerCmd.Remove(form);
        }
    }

    public static async Task<bool> PayOverdraft(
        PlayerChoiceContext choiceContext,
        Creature creature,
        CardModel? source)
    {
        MagicAmplificationPower? amplification = GetAmplification(creature);
        if (amplification?.TryReserveForOverdraft(source) == true)
        {
            return true;
        }

        MagicArmorPower? armor = GetArmor(creature);
        if (armor is not { Amount: > 0 } || creature.Player == null)
        {
            return false;
        }

        if (creature.CombatState is not CombatState combatState)
        {
            return false;
        }
        OverdraftAcceptChoice accept = combatState.CreateCard<OverdraftAcceptChoice>(creature.Player);
        accept.Configure(1);
        CardModel decline = combatState.CreateCard<OverdraftDeclineChoice>(creature.Player);
        CardModel? selected;
        try
        {
            selected = await CardSelectCmd.FromChooseACardScreen(
                choiceContext,
                [accept, decline],
                creature.Player,
                false);
        }
        finally
        {
            combatState.RemoveCard(accept);
            combatState.RemoveCard(decline);
        }
        if (!ReferenceEquals(selected, accept))
        {
            return false;
        }
        await PowerCmd.Decrement(armor);
        return true;
    }
}
