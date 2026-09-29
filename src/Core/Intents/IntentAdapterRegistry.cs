using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Core.Intents;

public sealed class IntentRuntimeState
{
    private EroticIntentRuntimePower? _carrier;
    private bool _forceStun;
    private bool _controlDisabled;
    private int _desireIntentUses;
    private int _controlIntentUses;
    private int _invasionIntentUses;
    private int _lastNaturalRollTurn = -1;
    private int _desireCooldownThroughTurn = -1;
    private int _controlCooldownThroughTurn = -1;
    private int _lastEroticSelectionRound = -1;
    private int _consecutiveEroticSelectionRounds;

    public bool ForceStun
    {
        get => _forceStun;
        set { _forceStun = value; Sync(); }
    }

    public bool ControlDisabled
    {
        get => _controlDisabled;
        set { _controlDisabled = value; Sync(); }
    }

    public int DesireIntentUses
    {
        get => _desireIntentUses;
        set { _desireIntentUses = value; Sync(); }
    }

    public int ControlIntentUses
    {
        get => _controlIntentUses;
        set { _controlIntentUses = value; Sync(); }
    }

    public int InvasionIntentUses
    {
        get => _invasionIntentUses;
        set { _invasionIntentUses = value; Sync(); }
    }

    public int LastNaturalRollTurn
    {
        get => _lastNaturalRollTurn;
        set { _lastNaturalRollTurn = value; Sync(); }
    }

    public int DesireCooldownThroughTurn
    {
        get => _desireCooldownThroughTurn;
        set { _desireCooldownThroughTurn = value; Sync(); }
    }

    public int ControlCooldownThroughTurn
    {
        get => _controlCooldownThroughTurn;
        set { _controlCooldownThroughTurn = value; Sync(); }
    }

    public int LastEroticSelectionRound
    {
        get => _lastEroticSelectionRound;
        set { _lastEroticSelectionRound = value; Sync(); }
    }

    public int ConsecutiveEroticSelectionRounds
    {
        get => _consecutiveEroticSelectionRounds;
        set { _consecutiveEroticSelectionRounds = value; Sync(); }
    }

    public bool SteadfastScheduled { get; set; }
    public bool PersistenceScheduled { get; set; }

    public int Uses(EroticIntentKind kind) => kind switch
    {
        EroticIntentKind.Desire => DesireIntentUses,
        EroticIntentKind.Control => ControlIntentUses,
        _ => InvasionIntentUses,
    };

    public void Increment(EroticIntentKind kind)
    {
        switch (kind)
        {
            case EroticIntentKind.Desire: DesireIntentUses++; break;
            case EroticIntentKind.Control: ControlIntentUses++; break;
            case EroticIntentKind.Invasion: InvasionIntentUses++; break;
        }
    }

    public void AttachExisting(EroticIntentRuntimePower carrier)
    {
        if (ReferenceEquals(_carrier, carrier)) return;
        _carrier = carrier;
        _forceStun = carrier.ForceStun;
        _controlDisabled = carrier.ControlDisabled;
        _desireIntentUses = carrier.DesireIntentUses;
        _controlIntentUses = carrier.ControlIntentUses;
        _invasionIntentUses = carrier.InvasionIntentUses;
        _lastNaturalRollTurn = carrier.LastNaturalRollTurn;
        _desireCooldownThroughTurn = carrier.DesireCooldownThroughTurn;
        _controlCooldownThroughTurn = carrier.ControlCooldownThroughTurn;
        _lastEroticSelectionRound = carrier.LastEroticSelectionRound;
        _consecutiveEroticSelectionRounds = carrier.ConsecutiveEroticSelectionRounds;
    }

    public void AttachNew(EroticIntentRuntimePower carrier)
    {
        _carrier = carrier;
        Sync();
    }

    private void Sync()
    {
        if (_carrier != null)
        {
            _carrier.ForceStun = _forceStun;
            _carrier.ControlDisabled = _controlDisabled;
            _carrier.DesireIntentUses = _desireIntentUses;
            _carrier.ControlIntentUses = _controlIntentUses;
            _carrier.InvasionIntentUses = _invasionIntentUses;
            _carrier.LastNaturalRollTurn = _lastNaturalRollTurn;
            _carrier.DesireCooldownThroughTurn = _desireCooldownThroughTurn;
            _carrier.ControlCooldownThroughTurn = _controlCooldownThroughTurn;
            _carrier.LastEroticSelectionRound = _lastEroticSelectionRound;
            _carrier.ConsecutiveEroticSelectionRounds = _consecutiveEroticSelectionRounds;
        }
    }
}

public static class IntentAdapterRegistry
{
    private static readonly Dictionary<Type, object> Adapters = [];
    private static readonly ConditionalWeakTable<MonsterModel, IntentRuntimeState> Runtime = new();

    public static void Register<TMonster>(object adapter)
        where TMonster : MonsterModel
    {
        ArgumentNullException.ThrowIfNull(adapter);
        Adapters[typeof(TMonster)] = adapter;
    }

    public static object? GetAdapter(MonsterModel monster)
    {
        Type type = monster.GetType();
        return Adapters.TryGetValue(type, out object? exact)
            ? exact
            : Adapters.FirstOrDefault(pair => pair.Key.IsAssignableFrom(type)).Value;
    }

    public static TProvider? GetProvider<TProvider>(MonsterModel monster)
        where TProvider : class =>
        monster as TProvider ?? GetAdapter(monster) as TProvider;

    public static IntentRuntimeState GetRuntime(MonsterModel monster)
    {
        IntentRuntimeState state = Runtime.GetOrCreateValue(monster);
        EroticIntentRuntimePower? existing =
            monster.Creature.GetPower<EroticIntentRuntimePower>();
        if (existing != null)
        {
            state.AttachExisting(existing);
        }
        return state;
    }

#if DEBUG
    internal static void ResetRuntimeForTests(MonsterModel monster) =>
        Runtime.Remove(monster);
#endif

    public static async Task Initialize(MonsterModel monster)
    {
        IntentRuntimeState state = GetRuntime(monster);
        if (state.PersistenceScheduled)
        {
            return;
        }

        if (monster.Creature.GetPower<EroticIntentRuntimePower>() == null)
        {
            state.PersistenceScheduled = true;
            try
            {
                EroticIntentRuntimePower? carrier =
                    await PowerCmd.Apply<EroticIntentRuntimePower>(
                        new ThrowingPlayerChoiceContext(),
                        monster.Creature,
                        1m,
                        monster.Creature,
                        null,
                        silent: true);
                if (carrier != null)
                {
                    state.AttachNew(carrier);
                }
            }
            finally
            {
                state.PersistenceScheduled = false;
            }
        }

        if (EroticAttackCatalog.Get(monster)?.Steadfast == true
            && !monster.Creature.HasPower<SteadfastPower>())
        {
            await PowerCmd.Apply<SteadfastPower>(
                new ThrowingPlayerChoiceContext(),
                monster.Creature,
                1m,
                monster.Creature,
                null,
                silent: true);
        }

        if (EroticAttackCatalog.Get(monster) is { Steadfast: false } spec)
        {
            await EroticIntentThresholdPower.ApplyAll(monster, spec);
        }
    }

    public static void OnControlEnded(
        MonsterModel source,
        ControlBreakReason reason)
    {
        if (reason != ControlBreakReason.Escaped || source.Creature.IsDead)
        {
            return;
        }

        EroticMonsterSpec? catalog = EroticAttackCatalog.Get(source);
        MoveState? lowThreat = EroticAttackCatalog.GetRecoveryMove(source)
            ?? GetProvider<ILowThreatIntentProvider>(source)
                ?.GetLowThreatMove(source);
        if (lowThreat != null)
        {
            IntentMoveFactory.SetRecoveryTransient(source, lowThreat);
            return;
        }

        if (catalog?.StunAfterEscape == true || lowThreat == null)
        {
            IntentMoveFactory.ForceStun(source);
        }
    }
}
