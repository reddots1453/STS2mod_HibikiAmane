using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesireUnboundConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "desire_unbound";

    public override string Args => string.Empty;

    public override string Description =>
        "Disable the desire cap and climax penalty this combat";

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

        Task task = PowerCmd.Apply<UnboundedDesirePower>(
            new ThrowingPlayerChoiceContext(),
            issuingPlayer.Creature,
            1m,
            issuingPlayer.Creature,
            null);
        return new CmdResult(
            task,
            success: true,
            "Desire cap and climax penalty disabled for this combat.");
    }
}
