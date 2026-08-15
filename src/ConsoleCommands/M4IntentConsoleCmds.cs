using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Intents;

namespace MaidenSuccubus.ConsoleCommands;

internal static class M4ConsoleTarget
{
    public static bool TryGet(
        Player? player,
        string[] args,
        int indexArg,
        out MonsterModel? monster,
        out string error)
    {
        monster = null;
        error = "";
        if (player?.Character is not MaidenSuccubusCharacter
            || player.Creature.CombatState == null)
        {
            error = "Use this during MaidenSuccubus combat.";
            return false;
        }

        int index = 0;
        if (args.Length > indexArg
            && (!int.TryParse(args[indexArg], out index) || index < 0))
        {
            error = "enemyIndex must be a non-negative integer.";
            return false;
        }

        var enemies = player.Creature.CombatState.HittableEnemies.ToArray();
        if (index >= enemies.Length || enemies[index].Monster == null)
        {
            error = $"enemyIndex must be between 0 and {enemies.Length - 1}.";
            return false;
        }

        monster = enemies[index].Monster;
        return true;
    }
}

public sealed class M4ControlIntentConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_control_intent";
    public override string Args =>
        "<attack|skill|power> [block:int=5] [escape:int=3] [enemyIndex:int=0]";
    public override string Description => "Replace one enemy's next move with Control";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length == 0
            || !Enum.TryParse(args[0], true, out ControlType type))
        {
            return new CmdResult(false, "Expected attack, skill, or power.");
        }
        if (!TryPositive(args, 1, 5, out int block)
            || !TryPositive(args, 2, 3, out int escape))
        {
            return new CmdResult(false, "block and escape must be positive integers.");
        }
        if (!M4ConsoleTarget.TryGet(
            issuingPlayer, args, 3, out MonsterModel? monster, out string error))
        {
            return new CmdResult(false, error);
        }

        IntentMoveFactory.SetTransient(
            monster!,
            IntentMoveFactory.CreateControl(
                monster!,
                new ControlIntentSpec(block, type, escape)));
        return new CmdResult(
            true,
            $"{monster!.Id.Entry} next move: {type} control {block}/{escape}.");
    }

    private static bool TryPositive(
        string[] args,
        int index,
        int fallback,
        out int value)
    {
        if (args.Length <= index)
        {
            value = fallback;
            return true;
        }
        return int.TryParse(args[index], out value) && value > 0;
    }
}

public sealed class M4ControlApplyConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_control_apply";
    public override string Args =>
        "<attack|skill|power> [block:int=5] [escape:int=3] [enemyIndex:int=0]";
    public override string Description => "Resolve Control immediately for block testing";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer == null
            || args.Length == 0
            || !Enum.TryParse(args[0], true, out ControlType type)
            || !TryInt(args, 1, 5, out int block)
            || !TryInt(args, 2, 3, out int escape)
            || block < 0
            || escape <= 0)
        {
            return new CmdResult(false, "Expected type and valid block/escape.");
        }
        if (!M4ConsoleTarget.TryGet(
            issuingPlayer, args, 3, out MonsterModel? monster, out string error))
        {
            return new CmdResult(false, error);
        }
        Task<ControlResolutionResult> task = ControlCmd.ResolveIntent(
            new ThrowingPlayerChoiceContext(),
            monster!.Creature,
            issuingPlayer.Creature,
            block,
            type,
            escape);
        return new CmdResult(
            task,
            true,
            $"Resolving {type} control from {monster.Id.Entry}.");
    }

    private static bool TryInt(
        string[] args,
        int index,
        int fallback,
        out int value)
    {
        if (args.Length <= index)
        {
            value = fallback;
            return true;
        }
        return int.TryParse(args[index], out value);
    }
}

public sealed class M4InvasionIntentConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_invasion_intent";
    public override string Args => "[damage:int=6] [enemyIndex:int=0]";
    public override string Description => "Replace one enemy's next move with Invasion";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        int damage = 6;
        if (args.Length > 0
            && (!int.TryParse(args[0], out damage) || damage < 0))
        {
            return new CmdResult(false, "damage must be a non-negative integer.");
        }
        if (!M4ConsoleTarget.TryGet(
            issuingPlayer, args, 1, out MonsterModel? monster, out string error))
        {
            return new CmdResult(false, error);
        }
        IntentMoveFactory.SetTransient(
            monster!,
            IntentMoveFactory.CreateInvasion(
                monster!,
                new InvasionIntentSpec(damage)));
        return new CmdResult(true, $"{monster!.Id.Entry} next move: Invasion {damage}.");
    }
}

public sealed class M4DesireIntentConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_desire_intent";
    public override string Args => "[amount:int=1] [enemyIndex:int=0]";
    public override string Description => "Replace one enemy's next move with Desire gain";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        int amount = 1;
        if (args.Length > 0
            && (!int.TryParse(args[0], out amount) || amount <= 0))
        {
            return new CmdResult(false, "amount must be a positive integer.");
        }
        if (!M4ConsoleTarget.TryGet(
            issuingPlayer, args, 1, out MonsterModel? monster, out string error))
        {
            return new CmdResult(false, error);
        }
        IntentMoveFactory.SetTransient(
            monster!,
            IntentMoveFactory.CreateDesire(
                monster!,
                new DesireIntentSpec(amount)));
        return new CmdResult(true, $"{monster!.Id.Entry} next move: Desire +{amount}.");
    }
}

public sealed class M4ControlReleaseConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_control_release";
    public override string Args => "[enemyIndex:int|all]";
    public override string Description => "Directly release one or every Control instance";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.Creature.CombatState == null)
        {
            return new CmdResult(false, "Use this during MaidenSuccubus combat.");
        }
        if (args.Length == 0 || args[0].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            Task task = ControlCmd.Release(
                new ThrowingPlayerChoiceContext(),
                issuingPlayer.Creature);
            return new CmdResult(task, true, "Releasing every Control instance.");
        }
        if (!M4ConsoleTarget.TryGet(
            issuingPlayer, args, 0, out MonsterModel? monster, out string error))
        {
            return new CmdResult(false, error);
        }
        Task release = ControlCmd.Release(
            new ThrowingPlayerChoiceContext(),
            issuingPlayer.Creature,
            monster!.Creature);
        return new CmdResult(release, true, $"Releasing {monster.Id.Entry} Control.");
    }
}

public sealed class M4StateConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_m4_state";
    public override string Args => string.Empty;
    public override string Description => "Dump Control and intent adapter runtime state";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.Creature.CombatState == null)
        {
            return new CmdResult(false, "Use this during MaidenSuccubus combat.");
        }

        var controls = ControlQuery.GetInstances(issuingPlayer)
            .Select(power =>
                $"{power.Applier?.Monster?.Id.Entry ?? "unknown"}:"
                + $"{power.ControlType}/{power.Amount}");
        var enemies = issuingPlayer.Creature.CombatState.HittableEnemies
            .Where(creature => creature.Monster != null)
            .Select((creature, index) =>
            {
                MonsterModel monster = creature.Monster!;
                IntentRuntimeState state = IntentAdapterRegistry.GetRuntime(monster);
                return $"{index}:{monster.Id.Entry} move={monster.NextMove.Id} "
                    + $"adapted={IntentAdapterRegistry.GetAdapter(monster) != null} "
                    + $"invaded={state.HasInvaded} controlDisabled={state.ControlDisabled} "
                    + $"desireUses={state.DesireIntentUses}";
            });
        var curses = PileType.Deck.GetPile(issuingPlayer).Cards
            .OfType<Cards.Curses.SemenCurse>()
            .Select(curse => curse.SourceMonsterId);

        string report =
            $"controls=[{string.Join(", ", controls)}]; "
            + $"curses=[{string.Join(", ", curses)}]; "
            + $"enemies=[{string.Join(" | ", enemies)}]";
        return new CmdResult(true, report);
    }
}
