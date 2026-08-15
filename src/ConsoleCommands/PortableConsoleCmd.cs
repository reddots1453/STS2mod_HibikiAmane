using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Keywords;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>
/// portable [handIndex] - marks a combat hand card Portable for framework tests.
/// </summary>
public sealed class PortableConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "portable";

    public override string Args => "[handIndex:int]";

    public override string Description =>
        "Give a MaidenSuccubus hand card the Portable keyword (default index 0)";

    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.Creature.CombatState == null)
        {
            return new CmdResult(false, "Use this during MaidenSuccubus combat.");
        }

        int index = 0;
        if (args.Length > 0 && !int.TryParse(args[0], out index))
        {
            return new CmdResult(false, "handIndex must be an int.");
        }

        IReadOnlyList<CardModel> hand = PileType.Hand.GetPile(issuingPlayer).Cards;
        if (index < 0 || index >= hand.Count)
        {
            return new CmdResult(false, $"handIndex must be between 0 and {hand.Count - 1}.");
        }

        CardModel card = hand[index];
        PortableKeyword.Apply(card);
        return new CmdResult(true, $"Portable applied to hand[{index}] {card.Id.Entry}.");
    }
}
