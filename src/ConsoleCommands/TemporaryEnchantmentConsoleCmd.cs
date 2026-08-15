using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Enchantments;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>
/// temp_enchant sharp|nimble [handIndex] [amount]
/// </summary>
public sealed class TemporaryEnchantmentConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "temp_enchant";

    public override string Args =>
        "<sharp|nimble|infection> [handIndex:int] [amount:int]";

    public override string Description =>
        "Temporarily enchant a MaidenSuccubus combat hand card";

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
            return new CmdResult(false, "Expected sharp or nimble.");
        }

        if (!TryInt(args, 1, 0, out int index)
            || !TryInt(args, 2, 1, out int amount)
            || amount <= 0)
        {
            return new CmdResult(false, "handIndex and amount must be valid positive integers.");
        }

        IReadOnlyList<CardModel> hand = PileType.Hand.GetPile(issuingPlayer).Cards;
        if (index < 0 || index >= hand.Count)
        {
            return new CmdResult(false, $"handIndex must be between 0 and {hand.Count - 1}.");
        }

        CardModel card = hand[index];
        try
        {
            EnchantmentModel applied = args[0].ToLowerInvariant() switch
            {
                "sharp" => CombatEnchantmentCmd.ApplyVanilla<Sharp>(card, amount),
                "nimble" => CombatEnchantmentCmd.ApplyVanilla<Nimble>(card, amount),
                "infection" => CombatEnchantmentCmd.ApplyAudited<InfectionEnchantment>(
                    card,
                    amount),
                _ => throw new InvalidOperationException(
                    "Expected sharp, nimble, or infection."),
            };
            return new CmdResult(
                true,
                $"Temporary {applied.Id.Entry} +{amount} applied to hand[{index}] {card.Id.Entry}.");
        }
        catch (InvalidOperationException ex)
        {
            return new CmdResult(false, ex.Message);
        }
    }

    private static bool TryInt(string[] args, int index, int fallback, out int value)
    {
        if (args.Length <= index)
        {
            value = fallback;
            return true;
        }

        return int.TryParse(args[index], out value);
    }
}
