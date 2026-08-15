using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>
/// Applies the one-shot life-payment modifier for framework testing.
/// </summary>
public sealed class DesireHpConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "desire_hp";

    public override string Args => string.Empty;

    public override string Description =>
        "Make the next positive desire card cost HP instead";

    public override bool IsNetworked => false;

    public override CmdResult Process(
        Player? issuingPlayer,
        string[] args)
    {
        if (issuingPlayer?.Creature.CombatState == null)
        {
            return new CmdResult(
                success: false,
                "This command only works during combat.");
        }

        Task task = PowerCmd.Apply<DesirePaidWithHpPower>(
            new ThrowingPlayerChoiceContext(),
            issuingPlayer.Creature,
            1m,
            issuingPlayer.Creature,
            null);
        return new CmdResult(
            task,
            success: true,
            "Next positive fixed desire cost will be paid with HP.");
    }
}
