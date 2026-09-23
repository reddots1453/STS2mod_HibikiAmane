using MegaCrit.Sts2.Core.Logging;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Core.Rewards;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Seals;
using MaidenSuccubus.Core.Desire;
using STS2RitsuLib.Combat.SecondaryResources;
using MaidenSuccubus.Core.Control;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Core.Intents;

namespace MaidenSuccubus.Debugging;

public static class FrameworkSelfTests
{
    public static void Run(Logger logger)
    {
#if DEBUG
        AssertProbabilities(0, 0.10m, 0.10m, 0.80m);
        AssertProbabilities(1, 0.10m, 0.15m, 0.75m);
        AssertProbabilities(-1, 0.15m, 0.10m, 0.75m);
        AssertProbabilities(2, 0.08m, 0.20m, 0.72m);
        AssertProbabilities(-2, 0.20m, 0.08m, 0.72m);
        AssertProbabilities(3, 0.05m, 0.30m, 0.65m);
        AssertProbabilities(-3, 0.30m, 0.05m, 0.65m);
        AssertProbabilities(4, 0.00m, 0.45m, 0.55m);
        AssertProbabilities(-4, 0.45m, 0.00m, 0.55m);
        AssertProbabilities(5, 0.00m, 0.65m, 0.35m);
        AssertProbabilities(-5, 0.65m, 0.00m, 0.35m);
        AssertRouteRolls();
        AssertSealRules();
        AssertDesireDefinition();
        AssertDesireAmountState();
        AssertControlTypes();
        AssertFourthActRoutes();
        AssertFourthRouteQuests();
        AssertStunSchedulingIsIdempotent();

        var normalized = RouteRewardProbabilities.Calculate(
            corruption: 5,
            holyBonus: 0.20m,
            corruptBonus: 0.50m);
        AssertClose(1m, normalized.Total, "normalized.Total");
        if (normalized.Holy < 0m
            || normalized.Corrupt < 0m
            || normalized.Neutral < 0m)
        {
            throw new InvalidOperationException(
                "Route probability normalization produced a negative value.");
        }

        logger.Info(
            "Framework self-tests passed: route rewards, combat seals, desire, control, and Fourth Act routing");
#endif
    }

    private static void AssertFourthActRoutes()
    {
        AssertBoolean(
            true,
            FourthActRouteService.DefaultForBand(CorruptionBand.Holy)
                == FourthActRoute.Holy,
            "fourth act holy route");
        AssertBoolean(
            true,
            FourthActRouteService.DefaultForBand(CorruptionBand.Neutral)
                == FourthActRoute.Neutral,
            "fourth act neutral route");
        AssertBoolean(
            true,
            FourthActRouteService.DefaultForBand(CorruptionBand.Corrupt)
                == FourthActRoute.Corrupt,
            "fourth act corrupt route");
    }

    private static void AssertFourthRouteQuests()
    {
        FourthRouteQuest[] all = FourthRouteProgressService.DarkQuests
            .Concat(FourthRouteProgressService.LightQuests)
            .ToArray();
        AssertBoolean(true, FourthRouteProgressService.DarkQuests.Length == 7,
            "seven dark route quests");
        AssertBoolean(true, FourthRouteProgressService.LightQuests.Length == 7,
            "seven light route quests");
        AssertBoolean(true, all.Distinct().Count() == 14,
            "fourteen unique route quests");
        AssertBoolean(true, Enum.GetValues<FourthRouteQuest>().All(all.Contains),
            "all route quests assigned an alignment");

        IReadOnlyDictionary<FourthRouteQuest, int> targets =
            new Dictionary<FourthRouteQuest, int>
            {
                [FourthRouteQuest.Pride] = 3,
                [FourthRouteQuest.Greed] = 1,
                [FourthRouteQuest.Lust] = 3,
                [FourthRouteQuest.Envy] = 2,
                [FourthRouteQuest.Gluttony] = 3,
                [FourthRouteQuest.Wrath] = 4,
                [FourthRouteQuest.Sloth] = 2,
                [FourthRouteQuest.Humility] = 2,
                [FourthRouteQuest.Generosity] = 1,
                [FourthRouteQuest.Chastity] = 3,
                [FourthRouteQuest.Benevolence] = 5,
                [FourthRouteQuest.Temperance] = 2,
                [FourthRouteQuest.Patience] = 5,
                [FourthRouteQuest.Diligence] = 3,
            };
        foreach ((FourthRouteQuest quest, int target) in targets)
        {
            AssertBoolean(true, FourthRouteProgressService.TargetFor(quest) == target,
                $"{quest} quest target");
        }
    }

    private static void AssertControlTypes()
    {
        AssertBoolean(true, ControlType.Attack.Matches(CardType.Attack), "attack control");
        AssertBoolean(true, ControlType.Skill.Matches(CardType.Skill), "skill control");
        AssertBoolean(true, ControlType.Power.Matches(CardType.Power), "power control");
        AssertBoolean(false, ControlType.Attack.Matches(CardType.Skill), "control orthogonality");
    }

    private static void AssertDesireDefinition()
    {
        var definition = DesireResource.Definition;
        AssertBoolean(
            true,
            !string.IsNullOrWhiteSpace(definition.Id),
            "desire resource id");
        AssertBoolean(
            true,
            definition.DefaultAmount == 0
                && definition.BaseMaxAmount == 10
                && definition.MinAmount == 0
                && definition.PersistencePolicy
                    == SecondaryResourcePersistencePolicy.Run
                && definition.TurnStartPolicy
                    == SecondaryResourceTurnStartPolicy.None,
            "desire resource definition");
    }

    private static void AssertStunSchedulingIsIdempotent()
    {
        var normal = new MoveState("NORMAL", _ => Task.CompletedTask);
        var stun = new MoveState(
            "STUNNED",
            _ => Task.CompletedTask,
            new StunIntent());

        AssertBoolean(
            true,
            IntentMoveFactory.ShouldQueueStun(normal),
            "normal move can schedule stun");
        AssertBoolean(
            false,
            IntentMoveFactory.ShouldQueueStun(stun),
            "stun does not schedule itself");

        normal.FollowUpState = stun;
        AssertBoolean(
            false,
            IntentMoveFactory.ShouldQueueStun(normal),
            "queued stun is not duplicated");

        var stateIdOnly = new MoveState("STATE_ID_ONLY", _ => Task.CompletedTask)
        {
            FollowUpStateId = "STUNNED",
        };
        AssertBoolean(
            false,
            IntentMoveFactory.ShouldQueueStun(stateIdOnly),
            "stun follow-up id is not duplicated");
    }

    private static void AssertDesireAmountState()
    {
        var state = new MaidenSuccubus.Data.DesireAmountState();
        AssertBoolean(false, state.HasValue, "desire amount starts unmigrated");
        AssertBoolean(true, state.Amount == 0, "desire amount defaults to zero");

        state.Amount = 7;
        state.HasValue = true;
        AssertBoolean(
            true,
            state.HasValue && state.Amount == 7,
            "desire amount retains a cross-combat value");
    }

    private static void AssertSealRules()
    {
        AssertBoolean(
            true,
            CombatSealQuery.IsSealed(
                CorruptionBand.Holy,
                RouteCardKind.Corrupt),
            "holy band seals corrupt");
        AssertBoolean(
            false,
            CombatSealQuery.IsSealed(
                CorruptionBand.Holy,
                RouteCardKind.Holy),
            "holy band keeps holy");
        AssertBoolean(
            true,
            CombatSealQuery.IsSealed(
                CorruptionBand.Corrupt,
                RouteCardKind.Holy),
            "corrupt band seals holy");

        foreach (RouteCardKind route in Enum.GetValues<RouteCardKind>())
        {
            AssertBoolean(
                false,
                CombatSealQuery.IsSealed(CorruptionBand.Neutral, route),
                $"neutral band keeps {route}");
        }
    }

    private static void AssertRouteRolls()
    {
        var probabilities = RouteRewardProbabilities.Calculate(0);
        AssertRoute(
            RouteCardKind.Holy,
            RouteCardRewardService.RollRoute(probabilities, 0.05f),
            "route roll holy");
        AssertRoute(
            RouteCardKind.Corrupt,
            RouteCardRewardService.RollRoute(probabilities, 0.15f),
            "route roll corrupt");
        AssertRoute(
            RouteCardKind.Neutral,
            RouteCardRewardService.RollRoute(probabilities, 0.50f),
            "route roll neutral");
    }

    private static void AssertProbabilities(
        int corruption,
        decimal holy,
        decimal corrupt,
        decimal neutral)
    {
        var actual = RouteRewardProbabilities.Calculate(corruption);
        AssertClose(holy, actual.Holy, $"{corruption}.Holy");
        AssertClose(corrupt, actual.Corrupt, $"{corruption}.Corrupt");
        AssertClose(neutral, actual.Neutral, $"{corruption}.Neutral");
        AssertClose(1m, actual.Total, $"{corruption}.Total");
    }

    private static void AssertClose(decimal expected, decimal actual, string name)
    {
        if (Math.Abs(expected - actual) > 0.000001m)
        {
            throw new InvalidOperationException(
                $"Self-test failed for {name}: expected {expected}, actual {actual}.");
        }
    }

    private static void AssertRoute(
        RouteCardKind expected,
        RouteCardKind actual,
        string name)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(
                $"Self-test failed for {name}: expected {expected}, actual {actual}.");
        }
    }

    private static void AssertBoolean(
        bool expected,
        bool actual,
        string name)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(
                $"Self-test failed for {name}: expected {expected}, actual {actual}.");
        }
    }
}
