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
        Subscriptions.Add(
            RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(
                OnCombatStarting,
                replayCurrentState: false));
        Subscriptions.Add(
            RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(
                OnCombatEnded,
                replayCurrentState: false));
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

            DesireAmountState persisted = Data.Desire.AmountHandle.Get(player);
            int restoredByRitsu = SecondaryResourceCmd.Get(
                player,
                DesireResource.Id);

            if (!persisted.HasValue)
            {
                // Migrate runs saved before the non-combat bridge existed.
                Data.Desire.RememberCombatValue(player, restoredByRitsu);
                continue;
            }

            if (restoredByRitsu == persisted.Amount)
            {
                continue;
            }

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
                evt.CombatState,
                snapshot);
        }
    }

    private static void OnCombatEnded(CombatEndedEvent evt)
    {
        if (evt.CombatState == null)
        {
            return;
        }

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
