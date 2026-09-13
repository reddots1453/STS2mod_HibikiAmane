using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Data;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Commands;

public static class ControlCmd
{
    public static async Task<ControlResolutionResult> ResolveIntent(
        PlayerChoiceContext choiceContext,
        Creature source,
        Creature target,
        int controlBlock,
        ControlType type,
        int escapeAmount)
    {
        if (target.Player?.Character is not MaidenSuccubusCharacter
            || source.Monster == null
            || controlBlock < 0
            || escapeAmount <= 0)
        {
            return ControlResolutionResult.Ignored;
        }

        bool bypassBlock = Desire.Get(target.Player) >= 8;
        if (!bypassBlock && target.Block >= controlBlock)
        {
            await CreatureCmd.LoseBlock(
                choiceContext,
                target,
                controlBlock,
                source);
            return ControlResolutionResult.Blocked;
        }

        ControlPower? existing = target.Powers
            .OfType<ControlPower>()
            .FirstOrDefault(power => ReferenceEquals(power.Applier, source)
                && power.ControlType == type);
        if (existing != null)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                existing,
                escapeAmount,
                source,
                null);
            return ControlResolutionResult.Applied;
        }

        ControlPower power = (ControlPower)ModelDb
            .Power<ControlPower>()
            .ToMutable();
        power.ControlType = type;
        await PowerCmd.Apply(
            choiceContext,
            power,
            target,
            escapeAmount,
            source,
            null);
        return ControlResolutionResult.Applied;
    }

    public static async Task Escape(
        PlayerChoiceContext choiceContext,
        ControlPower control,
        int amount)
    {
        if (amount <= 0 || control.Amount <= 0)
        {
            return;
        }

        if (amount >= control.Amount)
        {
            control.PendingBreakReason = ControlBreakReason.Escaped;
        }
        await PowerCmd.ModifyAmount(
            choiceContext,
            control,
            -amount,
            control.Owner,
            null);
    }

    public static async Task Release(
        PlayerChoiceContext choiceContext,
        Creature target,
        Creature? source = null)
    {
        foreach (ControlPower power in target.Powers
            .OfType<ControlPower>()
            .Where(power => source == null
                || ReferenceEquals(power.Applier, source))
            .ToArray())
        {
            power.PendingBreakReason = ControlBreakReason.Direct;
            await PowerCmd.Remove(power);
        }
    }
}
