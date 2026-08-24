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
    private const int Sentinel = 1 << 20;
    private EroticIntentRuntimePower? _carrier;
    private bool _forceStun;
    private bool _controlDisabled;
    private int _desireIntentUses;
    private int _controlIntentUses;
    private int _invasionIntentUses;
    private int _lastNaturalRollTurn = -1;

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
        Decode(carrier.Amount);
    }

    public void AttachNew(EroticIntentRuntimePower carrier)
    {
        _carrier = carrier;
        Sync();
    }

    private int Encode()
    {
        int turn = Math.Clamp(_lastNaturalRollTurn + 1, 0, 511);
        return Sentinel
            | (Math.Clamp(_desireIntentUses, 0, 7) << 0)
            | (Math.Clamp(_controlIntentUses, 0, 7) << 3)
            | (Math.Clamp(_invasionIntentUses, 0, 7) << 6)
            | (turn << 9)
            | (_controlDisabled ? 1 << 18 : 0)
            | (_forceStun ? 1 << 19 : 0);
    }

    private void Decode(int value)
    {
        if ((value & Sentinel) == 0) return;
        _desireIntentUses = (value >> 0) & 0b111;
        _controlIntentUses = (value >> 3) & 0b111;
        _invasionIntentUses = (value >> 6) & 0b111;
        _lastNaturalRollTurn = ((value >> 9) & 0x1ff) - 1;
        _controlDisabled = (value & (1 << 18)) != 0;
        _forceStun = (value & (1 << 19)) != 0;
    }

    private void Sync()
    {
        if (_carrier != null)
        {
            _carrier.SetAmount(Encode(), silent: true);
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
        else if (!state.PersistenceScheduled)
        {
            state.PersistenceScheduled = true;
            TaskHelper.RunSafely(AttachCarrier(monster, state));
        }
        return state;
    }

    private static async Task AttachCarrier(
        MonsterModel monster,
        IntentRuntimeState state)
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
