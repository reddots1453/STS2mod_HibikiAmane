using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Seals;
using MaidenSuccubus.Data;
using MaidenSuccubus.Patches;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class M6Act4ConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_act4";
    public override string Args => "<holy|neutral|corrupt>";
    public override string Description => "Enter the placeholder Fourth Act on a chosen route";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState is not RunState runState)
        {
            return new CmdResult(false, "Use this during a MaidenSuccubus run.");
        }
        if (args.Length != 1
            || !Enum.TryParse(args[0], true, out FourthActRoute route))
        {
            return new CmdResult(false, "Expected holy, neutral, or corrupt.");
        }

        FourthActRunAdapter.EnsurePresent(runState);
        int index = runState.Acts
            .Select((act, i) => (act, i))
            .FirstOrDefault(pair => pair.act is MaidenSuccubusFourthAct).i;
        if (runState.Acts[index] is not MaidenSuccubusFourthAct)
        {
            return new CmdResult(false, "Fourth Act is disabled or not registered.");
        }

        FourthActRouteService.ForceNextDebugRoute(runState, route);
        return new CmdResult(
            RunManager.Instance.EnterAct(index),
            true,
            $"Entering placeholder Fourth Act via {route} route.");
    }
}

public sealed class M6DumpConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_dump";
    public override string Args => string.Empty;
    public override string Description => "Dump the unified MaidenSuccubus framework state";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState is not RunState runState)
        {
            return new CmdResult(false, "Use this during a MaidenSuccubus run.");
        }

        int desire = Desire.Get(issuingPlayer);
        string sealedCards = string.Join(
            ",",
            CombatSealQuery.GetSealedDeckCards(issuingPlayer)
                .Select(card => card.Id.Entry));
        string controls = string.Join(
            ",",
            ControlQuery.GetInstances(issuingPlayer)
                .Select(power =>
                    $"{power.Applier?.Monster?.Id.Entry ?? "unknown"}:"
                    + $"{power.ControlType}/{power.Amount}"));
        string invasionCurses = string.Join(
            ",",
            PileType.Deck.GetPile(issuingPlayer).Cards
                .OfType<Cards.Curses.SemenCurse>()
                .Where(card => !string.IsNullOrWhiteSpace(card.SourceMonsterId))
                .Select(card => card.SourceMonsterId));

        return new CmdResult(
            true,
            $"character={issuingPlayer.Character.Id.Entry}; "
            + $"corruption={CorruptionQuery.Get(runState)}/"
            + $"{CorruptionQuery.GetBand(runState)}; "
            + $"desire={desire}; maxTriggered="
            + $"{Desire.Handle.Get(runState).HasGrantedFirstMaxCorruption}; "
            + $"sealed=[{sealedCards}]; controls=[{controls}]; "
            + $"invasionSources=[{invasionCurses}]; "
            + $"act4Enabled={FourthActRunAdapter.Enabled}; "
            + $"acts={runState.Acts.Count}");
    }
}
