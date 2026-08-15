using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class PurificationConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "purification";

    public override string Args => "[amount:int]";

    public override string Description =>
        "Apply permanent Purification to the MaidenSuccubus player";

    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.Creature.CombatState == null)
        {
            return new CmdResult(false, "Use this during MaidenSuccubus combat.");
        }

        int amount = 1;
        if (args.Length > 0
            && (!int.TryParse(args[0], out amount) || amount <= 0))
        {
            return new CmdResult(false, "amount must be a positive int.");
        }

        Task task = PowerCmd.Apply<PurificationPower>(
            new ThrowingPlayerChoiceContext(),
            issuingPlayer.Creature,
            amount,
            issuingPlayer.Creature,
            null);
        return new CmdResult(task, true, $"Applying {amount} Purification.");
    }
}
