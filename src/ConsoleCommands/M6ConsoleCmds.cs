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
using MaidenSuccubus.Relics;
using MegaCrit.Sts2.Core.Commands;

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

        FourthActRunAdapter.Enabled = true;
        try
        {
            FourthActRunAdapter.EnsurePresent(runState);
        }
        finally
        {
            FourthActRunAdapter.Enabled = false;
        }
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

public sealed class M6RouteConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_route";
    public override string Args => "<state|set|complete|advance> [quest]";
    public override string Description => "Inspect or advance the fourteen-route MVP flow";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState is not RunState runState)
            return new CmdResult(false, "Use this during a MaidenSuccubus run.");
        if (args.Length == 0 || args[0].Equals("state", StringComparison.OrdinalIgnoreCase))
            return new CmdResult(true, Describe(runState));
        if (args[0].Equals("set", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length != 2 || !Enum.TryParse(args[1], true, out FourthRouteQuest quest))
                return new CmdResult(false, $"Expected one of: {string.Join(", ", Enum.GetNames<FourthRouteQuest>())}");
            return new CmdResult(SetQuest(issuingPlayer, runState, quest), true, $"Resetting route to {quest}.");
        }
        if (!FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest active))
            return new CmdResult(false, "No route quest has been selected.");
        if (args[0].Equals("complete", StringComparison.OrdinalIgnoreCase))
            return new CmdResult(FourthRouteProgressService.AddProgress(
                issuingPlayer, active, FourthRouteProgressService.TargetFor(active)), true, $"Completing {active}.");
        if (args[0].Equals("advance", StringComparison.OrdinalIgnoreCase))
        {
            int stage = M5Progress.Handle.Get(runState).FourthRouteRelicStage;
            if (stage is < 1 or >= 4) return new CmdResult(false, $"Cannot advance from stage {stage}.");
            return new CmdResult(FourthRouteProgressService.AdvanceStage(issuingPlayer, stage), true,
                $"Advancing {active} from stage {stage}.");
        }
        return new CmdResult(false, "Expected state, set, complete, or advance.");
    }

    private static async Task SetQuest(Player player, RunState runState, FourthRouteQuest quest)
    {
        foreach (FourthRouteRelic relic in player.Relics.OfType<FourthRouteRelic>().ToList())
            await RelicCmd.Remove(relic);
        M5Progress.Handle.Modify(runState, state =>
        {
            state.FourthRouteQuestId = "";
            state.FourthRouteAlignment = "";
            state.FourthRouteQuestProgress = 0;
            state.FourthRouteQuestCompleted = false;
            state.FourthRouteRelicStage = 0;
            state.FourthRouteFragmentPending = false;
            state.FourthRouteFragmentOffered = false;
            state.FourthRouteFragmentPurchased = false;
            state.FourthRouteSacrificeCompleted = false;
            state.FourthRouteThirdBossDefeated = false;
        });
        FourthRouteProgressService.SelectQuest(runState, quest);
    }

    private static string Describe(RunState runState)
    {
        M5ProgressState state = M5Progress.Handle.Get(runState);
        return $"quest={state.FourthRouteQuestId}; alignment={state.FourthRouteAlignment}; "
            + $"progress={state.FourthRouteQuestProgress}; complete={state.FourthRouteQuestCompleted}; "
            + $"stage={state.FourthRouteRelicStage}; fragmentPending={state.FourthRouteFragmentPending}; "
            + $"fragmentOffered={state.FourthRouteFragmentOffered}; sacrificed={state.FourthRouteSacrificeCompleted}; "
            + $"bossDefeated={state.FourthRouteThirdBossDefeated}; canEnter={FourthRouteProgressService.CanEnterFourthAct(runState)}";
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
        M5ProgressState route = M5Progress.Handle.Get(runState);

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
            + $"route={route.FourthRouteQuestId}:{route.FourthRouteQuestProgress}/stage{route.FourthRouteRelicStage}; "
            + $"acts={runState.Acts.Count}");
    }
}
