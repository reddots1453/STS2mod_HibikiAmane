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
        if (context.Definition.Id != DesireResource.Id
            || context.NewAmount < Data.Desire.Max
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

            GrantFirstMaximumCorruption(runState);

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
                    state => state.PendingClimaxResolutions++);
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
        }
        finally
        {
            ResolvingPlayers.Remove(context.Player);
        }
    }

    private static void GrantFirstMaximumCorruption(RunState runState)
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
