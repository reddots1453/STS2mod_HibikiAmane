using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MaidenSuccubus.Core.Control;

namespace MaidenSuccubus.Core.Intents;

public sealed record ControlIntentSpec(
    int BlockRequired,
    ControlType ControlType,
    int EscapeRequired,
    int Weight = 1);

public sealed record InvasionIntentSpec(
    int Damage,
    int Weight = 1,
    bool MayControlAgain = false);

public sealed record DesireIntentSpec(
    int Desire,
    int MaxUsesPerCombat = 1,
    int Weight = 1);

public interface IControlIntentProvider
{
    ControlIntentSpec? GetControlIntent(MonsterModel monster);
}

public interface IInvasionIntentProvider
{
    InvasionIntentSpec? GetInvasionIntent(MonsterModel monster);
}

public interface IDesireIntentProvider
{
    DesireIntentSpec? GetDesireIntent(MonsterModel monster);
}

public interface ILowThreatIntentProvider
{
    MoveState? GetLowThreatMove(MonsterModel monster);
}

