using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.Acts;

public enum FourthRouteAlignment { Dark, Light }

public enum FourthRouteQuest
{
    Pride, Greed, Lust, Envy, Gluttony, Wrath, Sloth,
    Humility, Generosity, Chastity, Benevolence, Temperance, Patience, Diligence
}

public static class FourthRouteProgressService
{
    public static readonly FourthRouteQuest[] DarkQuests =
        [FourthRouteQuest.Pride, FourthRouteQuest.Greed, FourthRouteQuest.Lust,
         FourthRouteQuest.Envy, FourthRouteQuest.Gluttony, FourthRouteQuest.Wrath,
         FourthRouteQuest.Sloth];
    public static readonly FourthRouteQuest[] LightQuests =
        [FourthRouteQuest.Humility, FourthRouteQuest.Generosity, FourthRouteQuest.Chastity,
         FourthRouteQuest.Benevolence, FourthRouteQuest.Temperance, FourthRouteQuest.Patience,
         FourthRouteQuest.Diligence];

    public static FourthRouteAlignment AlignmentOf(FourthRouteQuest quest) =>
        DarkQuests.Contains(quest) ? FourthRouteAlignment.Dark : FourthRouteAlignment.Light;

    public static bool TryGetQuest(RunState runState, out FourthRouteQuest quest) =>
        Enum.TryParse(M5Progress.Handle.Get(runState).FourthRouteQuestId, out quest);

    public static void SelectQuest(RunState runState, FourthRouteQuest quest)
    {
        M5Progress.Handle.Modify(runState, state =>
        {
            if (!string.IsNullOrEmpty(state.FourthRouteQuestId)) return;
            state.FourthRouteQuestId = quest.ToString();
            state.FourthRouteAlignment = AlignmentOf(quest).ToString();
            state.FourthRouteQuestProgress = 0;
        });
    }

    public static async Task AddProgress(Player player, FourthRouteQuest quest, int amount = 1)
    {
        if (player.RunState is not RunState runState || !TryGetQuest(runState, out FourthRouteQuest active)
            || active != quest || M5Progress.Handle.Get(runState).FourthRouteQuestCompleted)
            return;
        int target = TargetFor(quest);
        bool completed = false;
        M5Progress.Handle.Modify(runState, state =>
        {
            state.FourthRouteQuestProgress = Math.Min(target, state.FourthRouteQuestProgress + amount);
            if (state.FourthRouteQuestProgress >= target)
            {
                state.FourthRouteQuestCompleted = true;
                state.FourthRouteRelicStage = 1;
                state.FourthRouteFragmentPending = true;
                completed = true;
            }
        });
        if (!completed) return;
        CorruptionCmd.Modify(runState, AlignmentOf(quest) == FourthRouteAlignment.Dark ? 1 : -1,
            new CorruptionChangeSource($"fourth_route.{quest.ToString().ToLowerInvariant()}"));
        await RelicCmd.Obtain(CreateRelic(quest), player);
    }

    public static async Task CheckThresholdQuest(Player player)
    {
        if (player.RunState is not RunState runState || !TryGetQuest(runState, out FourthRouteQuest quest)) return;
        if (quest == FourthRouteQuest.Greed && player.Gold >= 300)
            await AddProgress(player, quest, TargetFor(quest));
    }

    public static async Task AdvanceStage(Player player, int expectedCurrentStage)
    {
        if (player.RunState is not RunState runState || !TryGetQuest(runState, out FourthRouteQuest quest)) return;
        M5ProgressState state = M5Progress.Handle.Get(runState);
        if (!state.FourthRouteQuestCompleted || state.FourthRouteRelicStage != expectedCurrentStage) return;
        FourthRouteRelic? relic = player.Relics.OfType<FourthRouteRelic>().FirstOrDefault(r => r.Quest == quest);
        if (relic == null) return;
        int next = Math.Min(4, expectedCurrentStage + 1);
        FourthRouteRelic replacement = CreateRelic(quest);
        replacement.Stage = next;
        if (relic is GreedRouteRelic oldGreed && replacement is GreedRouteRelic newGreed)
        {
            newGreed.FreeShopPending = oldGreed.FreeShopPending;
            newGreed.FreeShopActive = oldGreed.FreeShopActive;
        }
        await RelicCmd.Remove(relic);
        await RelicCmd.Obtain(replacement, player);
        M5Progress.Handle.Modify(runState, data =>
        {
            data.FourthRouteRelicStage = next;
            if (next >= 2) { data.FourthRouteFragmentPending = false; data.FourthRouteFragmentPurchased = true; }
            if (next >= 3) data.FourthRouteSacrificeCompleted = true;
            if (next >= 4) data.FourthRouteThirdBossDefeated = true;
        });
    }

    public static bool CanEnterFourthAct(RunState runState)
    {
        M5ProgressState state = M5Progress.Handle.Get(runState);
        if (state.FourthRouteRelicStage < 4 || !TryGetQuest(runState, out FourthRouteQuest quest)) return false;
        int corruption = CorruptionQuery.Get(runState);
        return AlignmentOf(quest) == FourthRouteAlignment.Dark ? corruption > -3 : corruption < 3;
    }

    public static int TargetFor(FourthRouteQuest quest) => quest switch
    {
        FourthRouteQuest.Greed => 1,
        FourthRouteQuest.Generosity => 1,
        FourthRouteQuest.Envy or FourthRouteQuest.Humility
            or FourthRouteQuest.Temperance or FourthRouteQuest.Sloth => 2,
        FourthRouteQuest.Benevolence or FourthRouteQuest.Patience => 5,
        FourthRouteQuest.Wrath => 4,
        _ => 3
    };

    private static FourthRouteRelic CreateRelic(FourthRouteQuest quest) => quest switch
    {
        FourthRouteQuest.Pride => Create<PrideRouteRelic>(),
        FourthRouteQuest.Greed => Create<GreedRouteRelic>(),
        FourthRouteQuest.Lust => Create<LustRouteRelic>(),
        FourthRouteQuest.Envy => Create<EnvyRouteRelic>(),
        FourthRouteQuest.Gluttony => Create<GluttonyRouteRelic>(),
        FourthRouteQuest.Wrath => Create<WrathRouteRelic>(),
        FourthRouteQuest.Sloth => Create<SlothRouteRelic>(),
        FourthRouteQuest.Humility => Create<HumilityRouteRelic>(),
        FourthRouteQuest.Generosity => Create<GenerosityRouteRelic>(),
        FourthRouteQuest.Chastity => Create<ChastityRouteRelic>(),
        FourthRouteQuest.Benevolence => Create<BenevolenceRouteRelic>(),
        FourthRouteQuest.Temperance => Create<TemperanceRouteRelic>(),
        FourthRouteQuest.Patience => Create<PatienceRouteRelic>(),
        _ => Create<DiligenceRouteRelic>()
    };

    private static FourthRouteRelic Create<T>() where T : FourthRouteRelic =>
        (FourthRouteRelic)ModelDb.Relic<T>().ToMutable();
}
