#if DEBUG
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MaidenSuccubus.Debugging.CardEffects;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>
/// ms_test_cards confirm [all|iteration2|ds27-neutral|ds27-holy|CardTypeName]
/// Destructively normalizes the active combat, so the confirmation token is mandatory.
/// </summary>
public sealed class CardEffectTestConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_test_cards";
    public override string Args => "confirm [all|iteration2|ds27-neutral|ds27-holy|CardTypeName]";
    public override string Description =>
        "Run exact MaidenSuccubus card-effect tests in a disposable combat (destructive)";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer == null)
            return new CmdResult(false, "This command requires an active run.");
        if (args.Length == 0 || !string.Equals(args[0], "confirm", StringComparison.Ordinal))
        {
            return new CmdResult(false,
                "Destructive test: use a disposable run, then enter "
                + "ms_test_cards confirm [all|iteration2|ds27-neutral|ds27-holy|CardTypeName].");
        }

        string requested = args.Length >= 2 ? args[1] : "all";
        Task task = RunAndLog(issuingPlayer, requested);
        return new CmdResult(task, true,
            $"Started exact card-effect tests for {requested}; watch the log and latest.json.");
    }

    private static async Task RunAndLog(Player player, string requested)
    {
        try
        {
            string summary = await CardEffectTestRunner.Run(player, requested);
            MaidenSuccubusMod.Logger.Info("[CardEffectTest] " + summary);
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error(
                "[CardEffectTest] Suite aborted before report completion: " + ex);
        }
    }
}
#endif
