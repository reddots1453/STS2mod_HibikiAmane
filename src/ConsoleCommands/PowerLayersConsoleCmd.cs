using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Powers;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class PowerLayersConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "power_layers";
    public override string Args => string.Empty;
    public override string Description =>
        "Report visible buff/debuff Amount totals for MaidenSuccubus";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.Creature.CombatState == null)
        {
            return new CmdResult(false, "Use this during MaidenSuccubus combat.");
        }

        int buffs = PowerLayerQuery.CountBuffLayers(issuingPlayer.Creature);
        int debuffs = PowerLayerQuery.CountDebuffLayers(issuingPlayer.Creature);
        return new CmdResult(
            true,
            $"Visible power layers: buffs={buffs}, debuffs={debuffs}.");
    }
}
