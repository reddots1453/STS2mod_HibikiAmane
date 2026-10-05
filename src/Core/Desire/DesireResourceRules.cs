using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.SecondaryResources;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Presentation;

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
/// Resolves the special rule at the current desire maximum. The resource framework remains
/// responsible for card affordability, payment, free play, X and replay.
/// </summary>
public sealed class DesireResourceRules : ISecondaryResourceHookListener
{
    private static readonly HashSet<Player> ResolvingPlayers = [];

    public decimal ModifySecondaryResourceGain(SecondaryResourceContext context, decimal amount) =>
        context.Definition.Id == DesireResource.Id
            ? DesireRuleModifiers.ModifyGain(context.Player, amount)
            : amount;

    public Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context)
    {
        DesireCombatSpending.Record(context);
        return Task.CompletedTask;
    }

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

        Data.Desire.RememberCombatValue(
            context.Player,
            context.NewAmount);
        DesireEvents.Publish(new DesireChanged(
            context.Player,
            context.OldAmount,
            context.NewAmount));

        await SyncWetPower(
            new ThrowingPlayerChoiceContext(), context.Player, "resource-change");

        await RecheckMaximum(context.Player, context.Source);
    }

    /// <summary>
    /// Wet is derived from persistent desire, but native powers are combat-only.
    /// A silent resource restore must re-establish it without inventing a gain.
    /// </summary>
    internal static async Task SyncWetPower(
        PlayerChoiceContext choiceContext, Player player, string reason)
    {
        if (player.Creature.CombatState == null || player.PlayerCombatState == null
            || !CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding
            || player.Creature.IsDead) return;

        int amount = Data.Desire.Get(player);
        WetPower? wet = player.Creature.GetPower<WetPower>();
        if (amount >= 8 && wet == null)
        {
            await PowerCmd.Apply<WetPower>(
                choiceContext, player.Creature, 1, player.Creature, null, silent: true);
            if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding) return;
            if (player.Creature.HasPower<WetPower>())
                MaidenSuccubusMod.Logger.Info(
                    $"[DesireStatus] Wet applied: desire={amount}, reason={reason}");
            else
                MaidenSuccubusMod.Logger.Warn(
                    $"[DesireStatus] Wet missing after native application: desire={amount}, reason={reason}");
        }
        else if (amount < 8 && wet != null)
        {
            await PowerCmd.Remove(wet);
            MaidenSuccubusMod.Logger.Info(
                $"[DesireStatus] Wet removed: desire={amount}, reason={reason}");
        }
    }

    internal static async Task RecheckMaximum(Player player, AbstractModel? source)
    {
        if (Data.Desire.Get(player) < Data.Desire.GetMaximum(player)
            || !DesireRuleModifiers.ShouldTriggerPenalty(player)
            || !ResolvingPlayers.Add(player))
        {
            return;
        }

        try
        {
            if (player.RunState is not RunState runState)
            {
                return;
            }

            bool inCombat = CombatManager.Instance.IsInProgress
                && !CombatManager.Instance.IsEnding
                && player.Creature.CombatState != null;
            bool isPlayerTurn = inCombat
                && player.Creature.CombatState!.CurrentSide
                    == CombatSide.Player;
            if (!isPlayerTurn)
            {
                int threshold = Data.Desire.GetMaximum(player);
                Data.Desire.Handle.Modify(
                    runState,
                    state =>
                    {
                        if (!state.PendingClimaxResolution) state.PendingClimaxThreshold = threshold;
                        state.PendingClimaxResolution = true;
                    });
                PlayDesireFull(player);
                return;
            }

            Data.Desire.ClearPendingResolutions(runState);
            PlayDesireFull(player);
            await SecondaryResourceCmd.Set(
                player,
                DesireResource.Id,
                Data.Desire.ValueAfterOverflow,
                source);
            await PowerCmd.Apply<DesireStunPower>(
                new ThrowingPlayerChoiceContext(),
                player.Creature,
                1m,
                player.Creature,
                null);
            GrantFirstMaximumCorruption(runState);
        }
        finally
        {
            ResolvingPlayers.Remove(player);
        }
    }

    private static void PlayDesireFull(Player player)
    {
        bool firstAtMaximum = false;
        int maximum = Data.Desire.GetMaximum(player);
        Data.Desire.AmountHandle.Modify(player, state =>
        {
            int playedAt = state.MaximumAudioThreshold > 0 ? state.MaximumAudioThreshold : Data.Desire.Max;
            if (state.MaximumAudioTriggered && playedAt >= maximum) return;
            state.MaximumAudioTriggered = true;
            state.MaximumAudioThreshold = maximum;
            firstAtMaximum = true;
        });
        if (firstAtMaximum && PerformanceAudience.IsLocalMaiden(player))
        {
            PerformanceAudioService.PlayDesireMaximum();
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
