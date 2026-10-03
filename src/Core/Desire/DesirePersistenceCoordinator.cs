using MegaCrit.Sts2.Core.Rooms;
using System.Reflection;
using STS2RitsuLib.RunData;
using MaidenSuccubus.UI;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Data;

namespace MaidenSuccubus.Core.Desire;

/// <summary>
/// Bridges RitsuLib's combat-attached resource state to the per-player run
/// value used between combats. RitsuLib restores first because this listener
/// is registered by the character mod after the resource framework.
/// </summary>
internal static class DesirePersistenceCoordinator
{
    private static bool _initialized;
    private static readonly List<IDisposable> Subscriptions = [];

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        Subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(evt =>
        {
            foreach (Player player in evt.RunState.Players.Where(player => player.Character is MaidenSuccubusCharacter))
            {
                var bridge = Data.Desire.AmountHandle.Get(player);
                if (!bridge.HasValue && TryReadRunSnapshot(player, out int amount))
                    Data.Desire.RememberCombatValue(player, amount);
            }
            // The top bar may have been created before saved mod data was restored.
            RunUiRefreshEvents.PublishCombatVisibility(evt.RunState.CurrentRoom is CombatRoom);
        }, replayCurrentState: false));
        Subscriptions.Add(
            RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(
                OnCombatStarting,
                replayCurrentState: false));
        Subscriptions.Add(
            RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(
                OnCombatEnded,
                replayCurrentState: false));
    }

    private static readonly FieldInfo? SavedResourceField = typeof(SecondaryResourcePersistence)
        .GetField("SavedData", BindingFlags.Static | BindingFlags.NonPublic);

    internal static bool TryReadRunSnapshot(Player player, out int amount)
    {
        amount = 0;
        if (player.RunState is not RunState run
            || SavedResourceField?.GetValue(null) is not RunSavedData<SecondaryResourceRunSaveState> handle
            || !handle.TryGet(run, out var snapshot)
            || !snapshot.PlayerAmounts.TryGetValue(player.NetId, out var resources)
            || !resources.TryGetValue(DesireResource.Id, out amount)) return false;
        amount = Math.Max(0, amount);
        return true;
    }

    private static void OnCombatStarting(CombatStartingEvent evt)
    {
        if (evt.RunState is not RunState || evt.CombatState == null)
        {
            return;
        }

        foreach (var player in evt.CombatState.Players)
        {
            if (player.Character is not MaidenSuccubusCharacter)
            {
                continue;
            }
            RestoreForCombat(player, evt.CombatState);
        }
    }

    internal static void RestoreForCombat(
        Player player,
        ICombatState combatState)
    {
        if (player.Character is not MaidenSuccubusCharacter)
        {
            return;
        }

        DesireAmountState persisted = Data.Desire.AmountHandle.Get(player);
        int restoredByRitsu = SecondaryResourceCmd.Get(
            player,
            DesireResource.Id);

        if (!persisted.HasValue)
        {
            // Migrate runs saved before the non-combat bridge existed.
            Data.Desire.RememberCombatValue(player, restoredByRitsu);
            DesireEvents.Publish(new DesireChanged(
                player,
                restoredByRitsu,
                restoredByRitsu));
            return;
        }

        if (restoredByRitsu != persisted.Amount)
        {
            var snapshot = new SecondaryResourceRunSaveState
            {
                PlayerAmounts = new Dictionary<ulong, Dictionary<string, int>>
                {
                    [player.NetId] = new(StringComparer.OrdinalIgnoreCase)
                    {
                        [DesireResource.Id] = persisted.Amount,
                    },
                },
            };
            SecondaryResourcePersistence.RestoreSnapshot(
                combatState,
                snapshot);
        }

        // RestoreSnapshot intentionally emits no resource-change event. Send
        // an equal-value refresh so the left meter and expression bind to the
        // restored run value without replaying the ten-desire climax effect.
        int finalValue = SecondaryResourceCmd.Get(player, DesireResource.Id);
        DesireEvents.Publish(new DesireChanged(
            player,
            finalValue,
            finalValue));
    }

    private static void OnCombatEnded(CombatEndedEvent evt)
    {
        if (evt.CombatState == null)
        {
            return;
        }

        DesireCombatSpending.Close(evt.CombatState);

        foreach (var player in evt.CombatState.Players)
        {
            if (player.Character is MaidenSuccubusCharacter)
            {
                Data.Desire.RememberCombatValue(
                    player,
                    SecondaryResourceCmd.Get(player, DesireResource.Id));
            }
        }
    }
}
