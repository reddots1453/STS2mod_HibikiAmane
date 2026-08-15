using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>
/// condemnation [amount] - applies to the first hittable enemy.
/// condemnation judge - actively judges any positive amount.
/// </summary>
public sealed class CondemnationConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "condemnation";

    public override string Args => "[amount:int|judge|retain]";

    public override string Description =>
        "Apply or actively judge Condemnation on the first enemy";

    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.Creature.CombatState == null)
        {
            return new CmdResult(false, "Use this during MaidenSuccubus combat.");
        }

        var target = issuingPlayer.Creature.CombatState.HittableEnemies.FirstOrDefault();
        if (target == null)
        {
            return new CmdResult(false, "No hittable enemy exists.");
        }

        PlayerChoiceContext context = new ThrowingPlayerChoiceContext();
        if (args.Length > 0
            && args[0].Equals("retain", StringComparison.OrdinalIgnoreCase))
        {
            Task task = PowerCmd.Apply<CondemnationRetentionPower>(
                context,
                issuingPlayer.Creature,
                1m,
                issuingPlayer.Creature,
                null);
            return new CmdResult(
                task,
                true,
                "Condemnation will no longer clear after judgment.");
        }

        if (args.Length > 0
            && args[0].Equals("judge", StringComparison.OrdinalIgnoreCase))
        {
            Task task = CondemnationCmd.Judge(context, target, force: true);
            return new CmdResult(task, true, "Active Condemnation judgment requested.");
        }

        int amount = 7;
        if (args.Length > 0 && (!int.TryParse(args[0], out amount) || amount <= 0))
        {
            return new CmdResult(false, "amount must be a positive int.");
        }

        Task applyTask = CondemnationCmd.Apply(
            context,
            target,
            amount,
            issuingPlayer.Creature,
            null);
        return new CmdResult(applyTask, true, $"Applying {amount} Condemnation.");
    }
}
