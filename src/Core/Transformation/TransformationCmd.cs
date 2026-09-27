using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Presentation;

namespace MaidenSuccubus.Core.Transformation;

public static class TransformationCmd
{
    public const int InitialArmor = 3;
    public const int MaxArmor = 5;

    public static bool IsTransformed(Creature creature)
    {
        foreach (var power in creature.Powers)
        {
            if (power is ImmaculateRobePower or CorruptRobePower or EternalRobePower)
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

    public static Task EnterImmaculateRobe(
        PlayerChoiceContext choiceContext,
        Creature creature,
        CardModel? source) =>
        Enter<ImmaculateRobePower>(choiceContext, creature, source, 1);

    public static Task EnterCorruptRobe(
        PlayerChoiceContext choiceContext,
        Creature creature,
        CardModel? source) =>
        Enter<CorruptRobePower>(choiceContext, creature, source, 1);

    public static Task EnterEternalRobe(
        PlayerChoiceContext choiceContext,
        Creature creature,
        CardModel? source) =>
        Enter<EternalRobePower>(choiceContext, creature, source, 9);

    private static async Task Enter<T>(
        PlayerChoiceContext choiceContext,
        Creature creature,
        CardModel? source,
        int formAmount) where T : PowerModel
    {
        // Entering the same form must not refresh its armour.
        if (creature.HasPower<T>()) return;
        if (PerformanceAudience.IsLocalMaiden(creature.Player))
        {
            PerformanceAudioService.PlayOneShot(PerformanceAudioCue.TransformationStart);
        }
        bool alreadyUsedThisTurn = GetArmor(creature)?.UsedThisTurn == true;
        await Exit(choiceContext, creature);
        await PowerCmd.Apply<T>(
            choiceContext, creature, formAmount, creature, source);
        await PowerCmd.Apply<MagicArmorPower>(
            choiceContext, creature, InitialArmor, creature, source);
        if (GetArmor(creature) is { } newArmor)
            newArmor.UsedThisTurn = alreadyUsedThisTurn;
        if (PerformanceAudience.IsLocalMaiden(creature.Player))
        {
            PerformanceAudioService.PlayOneShot(PerformanceAudioCue.TransformationComplete);
        }
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
        amount = Math.Min(amount, Math.Max(0, MaxArmor - (int)(armor?.Amount ?? 0)));
        if (amount == 0) return false;
        if (armor == null)
        {
            await PowerCmd.Apply<MagicArmorPower>(
                choiceContext, creature, amount, creature, source);
        }
        else
        {
            await PowerCmd.ModifyAmount(
                choiceContext, armor, amount, creature, source);
        }
        if (PerformanceAudience.IsLocalMaiden(creature.Player))
        {
            PerformanceAudioService.PlayOneShot(PerformanceAudioCue.MagicCast);
        }
        return true;
    }

    public static async Task<bool> LoseArmor(
        PlayerChoiceContext choiceContext,
        Creature creature,
        int amount,
        AbstractModel? source)
    {
        MagicArmorPower? armor = GetArmor(creature);
        if (amount <= 0
            || !IsTransformed(creature))
        {
            return false;
        }

        // An oversized loss is one settlement: stop at zero. Only a later
        // loss attempt at zero exits; it does not produce an amount-change reward.
        if (armor is not { Amount: > 0 })
        {
            await Exit(choiceContext, creature);
            return false;
        }

        await PowerCmd.ModifyAmount(
            choiceContext,
            armor,
            -Math.Min(amount, (int)armor.Amount),
            creature,
            source as CardModel);
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
        foreach (var form in creature.Powers
            .Where(power => power is ImmaculateRobePower or CorruptRobePower or EternalRobePower)
            .ToArray())
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
        // Recheck after an asynchronous choice; zero is not a payable resource.
        return GetArmor(creature) is { Amount: > 0 }
            && await LoseArmor(choiceContext, creature, 1, source);
    }
}
