using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards.Scriptures;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Commands;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class ScriptureConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "scripture";

    public override string Args =>
        "<guardian|nimble|punishment|wisdom|vitality|bliss> [handIndex:int]";

    public override string Description =>
        "Transform a MaidenSuccubus combat hand card into a Scripture";

    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.Creature.CombatState == null)
        {
            return new CmdResult(false, "Use this during MaidenSuccubus combat.");
        }
        if (args.Length == 0)
        {
            return new CmdResult(false, "A Scripture name is required.");
        }

        int index = 0;
        if (args.Length > 1 && !int.TryParse(args[1], out index))
        {
            return new CmdResult(false, "handIndex must be an int.");
        }

        IReadOnlyList<CardModel> hand = PileType.Hand.GetPile(issuingPlayer).Cards;
        if (index < 0 || index >= hand.Count)
        {
            return new CmdResult(
                false,
                $"handIndex must be between 0 and {hand.Count - 1}.");
        }

        CardModel card = hand[index];
        Task task;
        switch (args[0].ToLowerInvariant())
        {
            case "guardian":
                task = ScriptureCmd.TransformCombatCard<GuardianScripture>(card);
                break;
            case "nimble":
                task = ScriptureCmd.TransformCombatCard<NimbleScripture>(card);
                break;
            case "punishment":
                task = ScriptureCmd.TransformCombatCard<PunishmentScripture>(card);
                break;
            case "wisdom":
                task = ScriptureCmd.TransformCombatCard<WisdomScripture>(card);
                break;
            case "vitality":
                task = ScriptureCmd.TransformCombatCard<VitalityScripture>(card);
                break;
            case "bliss":
                task = ScriptureCmd.TransformCombatCard<BlissScripture>(card);
                break;
            default:
                return new CmdResult(false, "Unknown Scripture name.");
        }

        return new CmdResult(
            task,
            true,
            $"Transforming hand[{index}] into {args[0]} Scripture.");
    }
}
