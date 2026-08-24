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

    // Only non-resource rule flags remain in mod-owned run data.
    public static RunSavedData<DesireState> Handle = null!;

    public static int Get(Player player) =>
        SecondaryResourceCmd.Get(player, DesireResource.Id);

    public static Task Modify(Player player, int delta)
    {
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

    public static Task Set(Player player, int value) =>
        SecondaryResourceCmd.Set(
            player,
            DesireResource.Id,
            Math.Max(Min, value));

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
        int pending = state.PendingClimaxResolutions
            + (state.PendingFirstTurnStun ? 1 : 0);
        if (pending <= 0)
        {
            return;
        }

        Handle.Modify(
            runState,
            saved =>
            {
                saved.PendingFirstTurnStun = false;
                saved.PendingClimaxResolutions = 0;
            });
        await SecondaryResourceCmd.Set(
            player,
            DesireResource.Id,
            ValueAfterOverflow);
        await PowerCmd.Apply<DesireStunPower>(
            choiceContext,
            player.Creature,
            pending,
            player.Creature,
            null);
    }
}
