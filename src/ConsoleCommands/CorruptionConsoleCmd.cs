using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Corruption;

namespace MaidenSuccubus.ConsoleCommands;

// 控制台命令：corruption [value]
// 无参：显示当前堕落值
// 有参：设置堕落值（自动 clamp 到 [-5, +5]）
// 游戏通过 ReflectionHelper.GetSubtypesInMods<AbstractConsoleCmd>() 自动发现，无需手动注册
public class CorruptionConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "corruption";

    public override string Args => "[value:int]";

    public override string Description => "Get or set MaidenSuccubus corruption (-5 to +5)";

    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer == null)
            return new CmdResult(success: false, "This command only works during a run.");

        if (issuingPlayer.RunState is not RunState runState)
            return new CmdResult(success: false, "Could not resolve run state.");

        if (args.Length == 0)
        {
            int current = CorruptionQuery.Get(runState);
            return new CmdResult(success: true, $"Corruption: {current}");
        }

        if (!int.TryParse(args[0], out int value))
            return new CmdResult(success: false, "Argument must be an int.");

        int actual = CorruptionCmd.Set(
            runState,
            value,
            CorruptionChangeSource.Debug);
        return new CmdResult(success: true, $"Corruption set to {actual} (clamped to [-5, +5])");
    }
}
