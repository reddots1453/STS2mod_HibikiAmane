using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Data;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>
/// desire [value] — reads or sets desire while preserving maximum rules.
/// </summary>
public sealed class DesireConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "desire";

    public override string Args => "[value:int]";

    public override string Description => "Get or set MaidenSuccubus desire (0 to 10)";

    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer == null || issuingPlayer.RunState is not RunState runState)
        {
            return new CmdResult(success: false, "This command only works during a run.");
        }

        if (args.Length == 0)
        {
            return new CmdResult(success: true, $"Desire: {Desire.Get(issuingPlayer)}");
        }

        if (!int.TryParse(args[0], out int value))
        {
            return new CmdResult(success: false, "Argument must be an int.");
        }

        Task task = Desire.Set(issuingPlayer, value);
        return new CmdResult(
            task,
            success: true,
            $"Desire set request: {Math.Max(value, Desire.Min)} (10 normally triggers climax resolution)");
    }
}
