using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Combat.SecondaryResources;
using MaidenSuccubus.Core.Desire;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>
/// Gives the first hand card an X desire cost for framework verification.
/// The temporary layer disappears with the combat card instance.
/// </summary>
public sealed class DesireXConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "desire_x";

    public override string Args => string.Empty;

    public override string Description =>
        "Give the first hand card an X desire cost this combat";

    public override bool IsNetworked => false;

    public override CmdResult Process(
        Player? issuingPlayer,
        string[] args)
    {
        var hand = issuingPlayer?.PlayerCombatState?.Hand;
        var card = hand?.Cards.FirstOrDefault();
        if (card == null)
        {
            return new CmdResult(
                success: false,
                "No card is currently in hand.");
        }

        card.SecondaryCosts().Set(
            DesireResource.Id,
            SecondaryResourceCost.X(),
            SecondaryResourceCostDuration.ThisCombat);
        return new CmdResult(
            success: true,
            $"Applied X desire cost to {card.Title}.");
    }
}
