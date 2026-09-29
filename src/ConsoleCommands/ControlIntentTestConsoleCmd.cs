#if DEBUG
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MaidenSuccubus.Debugging.ControlIntents;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>
/// ms_test_control confirm
/// Destructively normalizes the active combat, so confirmation is mandatory.
/// </summary>
public sealed class ControlIntentTestConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_test_control";
    public override string Args => "confirm";
    public override string Description =>
        "Run exact MaidenSuccubus control-intent tests in a disposable combat";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer == null)
            return new CmdResult(false, "This command requires an active run.");
        if (args.Length != 1
            || !string.Equals(args[0], "confirm", StringComparison.Ordinal))
        {
            return new CmdResult(false,
                "Destructive test: enter a disposable combat, then use "
                + "ms_test_control confirm.");
        }

        Task task = RunAndLog(issuingPlayer);
        return new CmdResult(task, true,
            $"Started {ControlIntentTestRunner.ScenarioCount} exact control-intent scenarios; "
            + "watch the log and control-intent-test-results/latest.json.");
    }

    private static async Task RunAndLog(Player player)
    {
        try
        {
            string summary = await ControlIntentTestRunner.Run(player);
            MaidenSuccubusMod.Logger.Info("[ControlIntentTest] " + summary);
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error(
                "[ControlIntentTest] Suite aborted before report completion: " + ex);
        }
    }
}
#endif
