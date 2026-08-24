using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MaidenSuccubus.Core.Control;

namespace MaidenSuccubus.Core.Intents;

public sealed record ControlIntentSpec(
    int BlockRequired,
    ControlType ControlType,
    int EscapeRequired,
    int MaxUsesPerCombat = 1,
    string DisplayName = "拘束",
    string EffectText = "");

public sealed record InvasionIntentSpec(
    int Damage,
    int MaxUsesPerCombat = 1,
    string CurseName = "精液",
    string DisplayName = "侵犯",
    string EffectText = "");

public sealed record DesireIntentSpec(
    int Desire,
    int MaxUsesPerCombat = 1,
    int Damage = 0,
    int Hits = 1,
    string DisplayName = "欲望攻击",
    string EffectText = "");

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
