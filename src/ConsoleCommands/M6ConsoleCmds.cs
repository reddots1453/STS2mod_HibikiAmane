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

        if (!FourthActRunAdapter.EnsureDebugPresent(runState))
            return new CmdResult(false, "Placeholder entry requires a single-player Debug build.");
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
        {
            if (active == FourthRouteQuest.Greed)
                return new CmdResult(FourthRouteProgressService.CheckThresholdQuest(issuingPlayer), true,
                    "Gold trial checks actual held gold; use the game's gold command to reach its target.");
            int trial = FourthRouteTrialRules.Number(FourthRouteProgressService.Trial(runState).Phase);
            return new CmdResult(FourthRouteProgressService.AddProgress(issuingPlayer, active,
                FourthRouteProgressService.TargetFor(active, trial)), true, $"Completing trial {trial} of {active}.");
        }
        if (args[0].Equals("advance", StringComparison.OrdinalIgnoreCase))
        {
            if (!FourthRouteProgressService.HasPendingInitialReward(runState))
                return new CmdResult(false, "Complete the current trial before claiming its reward.");
            return new CmdResult(FourthRouteProgressService.ClaimInitialReward(issuingPlayer), true, $"Claiming {active} reward.");
        }
        return new CmdResult(false, "Expected state, set, complete, or advance.");
    }

    private static async Task SetQuest(Player player, RunState runState, FourthRouteQuest quest)
    {
        foreach (FourthRouteRelic relic in player.Relics.OfType<FourthRouteRelic>().ToList())
            await RelicCmd.Remove(relic);
        foreach (FourthRouteFragmentRelic relic in player.Relics.OfType<FourthRouteFragmentRelic>().ToList())
            await RelicCmd.Remove(relic);
        M5Progress.Handle.Modify(runState, state =>
        {
            state.FourthRouteQuestId = "";
            state.FourthRouteAlignment = "";
            state.FourthRouteQuestProgress = 0;
            state.FourthRouteQuestCompleted = false;
            state.FourthRouteRewardPending = false;
            state.FourthRouteRewardClaimed = false;
            state.FourthRouteRewardCorruptionApplied = false;
            state.FourthRouteRelicStage = 0;
            state.FourthRouteFragmentPending = false;
            state.FourthRouteFragmentOffered = false;
            state.FourthRouteFragmentPurchased = false;
            state.FourthRouteSacrificeCompleted = false;
            state.FourthRouteThirdBossDefeated = false;
            state.FourthRouteEndingChecked = false;
            state.FourthRouteEndingEligible = false;
            state.FourthRouteTrial = null;
        });
        FourthRouteProgressService.SelectQuest(runState, quest);
        await FourthRouteProgressService.EnsureDormantRelic(player);
    }

    private static string Describe(RunState runState)
    {
        M5ProgressState state = M5Progress.Handle.Get(runState);
        return $"quest={state.FourthRouteQuestId}; alignment={state.FourthRouteAlignment}; phase={state.FourthRouteTrial?.Phase}; "
            + $"progress={state.FourthRouteQuestProgress}; complete={state.FourthRouteQuestCompleted}; "
            + $"stage={state.FourthRouteRelicStage}; fragmentPending={state.FourthRouteFragmentPending}; "
            + $"fragmentOffered={state.FourthRouteFragmentOffered}; sacrificed={state.FourthRouteSacrificeCompleted}; "
            + $"bossDefeated={state.FourthRouteThirdBossDefeated}; endingChecked={state.FourthRouteEndingChecked}; "
            + $"endingEligible={state.FourthRouteEndingEligible}; qualifiesNow={FourthRouteProgressService.HasFourthActQualification(runState)}; "
            + $"canEnter={FourthRouteProgressService.CanEnterFourthAct(runState)}";
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
