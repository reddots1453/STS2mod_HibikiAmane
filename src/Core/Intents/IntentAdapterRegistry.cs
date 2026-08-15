using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MaidenSuccubus.Core.Control;

namespace MaidenSuccubus.Core.Intents;

public sealed class IntentRuntimeState
{
    public bool ForceStun { get; set; }
    public bool HasInvaded { get; set; }
    public bool ControlDisabled { get; set; }
    public int DesireIntentUses { get; set; }
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

    public static IntentRuntimeState GetRuntime(MonsterModel monster) =>
        Runtime.GetOrCreateValue(monster);

    public static void OnControlEnded(
        MonsterModel source,
        ControlBreakReason reason)
    {
        if (reason != ControlBreakReason.Escaped || source.Creature.IsDead)
        {
            return;
        }

        MoveState? lowThreat = GetProvider<ILowThreatIntentProvider>(source)
            ?.GetLowThreatMove(source);
        if (lowThreat != null)
        {
            IntentMoveFactory.SetTransient(source, lowThreat);
            return;
        }

        GetRuntime(source).ForceStun = true;
        IntentMoveFactory.ForceStun(source);
    }
}
