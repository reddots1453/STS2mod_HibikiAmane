using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Combat.SecondaryResources;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Core.Desire;

public readonly record struct DesireChanged(
    Player Player,
    int OldValue,
    int NewValue);

public static class DesireEvents
{
    public static event Action<DesireChanged>? Changed;

    internal static void Publish(DesireChanged change)
    {
        foreach (Action<DesireChanged> handler in
            Changed?.GetInvocationList().Cast<Action<DesireChanged>>()
            ?? [])
        {
            try
            {
                handler(change);
            }
            catch (Exception ex)
            {
                MaidenSuccubusMod.Logger.Warn(
                    $"Desire Changed listener failed: {ex.Message}");
            }
        }
    }
}

/// <summary>
/// Resolves the special rule at ten desire. The resource framework remains
/// responsible for card affordability, payment, free play, X and replay.
/// </summary>
public sealed class DesireResourceRules : ISecondaryResourceHookListener
{
    private static readonly HashSet<Player> ResolvingPlayers = [];

    public decimal ModifyMaxSecondaryResource(
        SecondaryResourceMaxContext context,
        decimal amount) =>
        context.Definition.Id == DesireResource.Id
            ? DesireRuleModifiers.ModifyCap(context.Player, amount)
            : amount;

    public async Task AfterSecondaryResourceChanged(
        SecondaryResourceChangeContext context)
    {
        if (context.Definition.Id != DesireResource.Id)
        {
            return;
        }

        DesireEvents.Publish(new DesireChanged(
            context.Player,
            context.OldAmount,
            context.NewAmount));

        WetPower? wet = context.Player.Creature.GetPower<WetPower>();
        if (context.NewAmount >= 8 && wet == null)
        {
            await PowerCmd.Apply<WetPower>(
                new ThrowingPlayerChoiceContext(),
                context.Player.Creature,
                1,
                context.Player.Creature,
                null,
                silent: true);
        }
        else if (context.NewAmount < 8 && wet != null)
        {
            await PowerCmd.Remove(wet);
        }

        if (context.NewAmount < Data.Desire.Max
            || !DesireRuleModifiers.ShouldTriggerPenalty(context.Player)
            || !ResolvingPlayers.Add(context.Player))
        {
            return;
        }

        try
        {
            if (context.Player.RunState is not RunState runState)
            {
                return;
            }

            bool inCombat = CombatManager.Instance.IsInProgress
                && !CombatManager.Instance.IsEnding
                && context.Player.Creature.CombatState != null;
            bool isPlayerTurn = inCombat
                && context.Player.Creature.CombatState!.CurrentSide
                    == CombatSide.Player;
            if (!isPlayerTurn)
            {
                Data.Desire.Handle.Modify(
                    runState,
                    state => state.PendingClimaxResolution = true);
                return;
            }

            await SecondaryResourceCmd.Set(
                context.Player,
                DesireResource.Id,
                Data.Desire.ValueAfterOverflow,
                context.Source);
            await PowerCmd.Apply<DesireStunPower>(
                new ThrowingPlayerChoiceContext(),
                context.Player.Creature,
                1m,
                context.Player.Creature,
                null);
            GrantFirstMaximumCorruption(runState);
        }
        finally
        {
            ResolvingPlayers.Remove(context.Player);
        }
    }

    internal static void GrantFirstMaximumCorruption(RunState runState)
    {
        bool grant = false;
        Data.Desire.Handle.Modify(
            runState,
            state =>
            {
                if (state.HasGrantedFirstMaxCorruption)
                {
                    return;
                }

                state.HasGrantedFirstMaxCorruption = true;
                grant = true;
            });
        if (grant)
        {
            CorruptionCmd.Modify(
                runState,
                1,
                CorruptionChangeSource.DesireFirstMaximum);
        }
    }
}
