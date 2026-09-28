using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.RunData;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Data;

/// <summary>
/// Domain facade for the RitsuLib run-persistent secondary resource.
/// Gameplay code should use this facade instead of maintaining a second amount.
/// </summary>
public static class Desire
{
    public const int Min = 0;
    public const int Max = 10;
    public const int ValueAfterOverflow = 3;

    // Special rule flags remain run-wide; the amount bridge is per player so
    // multiplayer runs do not merge two Maiden players' desire values.
    public static RunSavedData<DesireState> Handle = null!;
    public static PlayerRunSavedData<DesireAmountState> AmountHandle = null!;

    public static int Get(Player player)
    {
        if (HasCombatState(player))
        {
            return SecondaryResourceCmd.Get(player, DesireResource.Id);
        }

        DesireAmountState state = AmountHandle.Get(player);
        return state.HasValue ? state.Amount : Min;
    }

    public static Task Modify(Player player, int delta)
    {
        if (!HasCombatState(player))
        {
            SetNonCombatValue(player, Get(player) + delta);
            return Task.CompletedTask;
        }

        if (delta > 0)
        {
            return SecondaryResourceCmd.Gain(
                player,
                DesireResource.Id,
                delta);
        }

        if (delta < 0)
        {
            return SecondaryResourceCmd.Lose(
                player,
                DesireResource.Id,
                -delta);
        }

        return Task.CompletedTask;
    }

    public static Task Modify(
        PlayerChoiceContext _,
        Player player,
        int delta) =>
        Modify(player, delta);

    public static Task Set(Player player, int value)
    {
        if (!HasCombatState(player))
        {
            SetNonCombatValue(player, value);
            return Task.CompletedTask;
        }

        return SecondaryResourceCmd.Set(
            player,
            DesireResource.Id,
            Math.Max(Min, value));
    }

    public static Task Set(
        PlayerChoiceContext _,
        Player player,
        int value) =>
        Set(player, value);

    public static async Task ResolvePendingFirstTurnStun(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player.RunState is not RunState runState)
        {
            return;
        }

        DesireState state = Handle.Get(runState);
        bool pending = state.PendingClimaxResolution
            || state.PendingClimaxResolutions > 0
            || state.PendingFirstTurnStun;
        if (!pending || !DesireRuleModifiers.ShouldTriggerPenalty(player))
        {
            return;
        }

        ClearPendingResolutions(runState);
        await SecondaryResourceCmd.Set(
            player,
            DesireResource.Id,
            ValueAfterOverflow);
        await PowerCmd.Apply<DesireStunPower>(
            choiceContext,
            player.Creature,
            1,
            player.Creature,
            null);
        DesireResourceRules.GrantFirstMaximumCorruption(runState);
    }

    internal static void ClearPendingResolutions(RunState runState) =>
        Handle.Modify(
            runState,
            saved =>
            {
                saved.PendingFirstTurnStun = false;
                saved.PendingClimaxResolution = false;
                saved.PendingClimaxResolutions = 0;
            });

    internal static void RememberCombatValue(Player player, int value)
    {
        int normalized = Math.Max(Min, value);
        AmountHandle.Modify(
            player,
            state =>
            {
                state.Amount = normalized;
                state.HasValue = true;
            });
    }

    private static void SetNonCombatValue(Player player, int value)
    {
        int oldValue = Get(player);
        int newValue = Math.Max(Min, value);
        RememberCombatValue(player, newValue);
        if (oldValue != newValue)
        {
            DesireEvents.Publish(new DesireChanged(
                player,
                oldValue,
                newValue));
        }
    }

    private static bool HasCombatState(Player player) =>
        player.Creature?.CombatState != null
        && player.PlayerCombatState != null;
}
