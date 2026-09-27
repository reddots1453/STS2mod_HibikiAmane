using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Characters;
using System.Runtime.CompilerServices;

namespace MaidenSuccubus.Acts;

public static class FourthRouteProgressService
{
    private static readonly ConditionalWeakTable<RunState, ClaimGate> Claims = new();
    private sealed class ClaimGate { public bool Busy; }
    public static readonly FourthRouteQuest[] DarkQuests =
        [FourthRouteQuest.Pride, FourthRouteQuest.Greed, FourthRouteQuest.Lust,
         FourthRouteQuest.Envy, FourthRouteQuest.Gluttony, FourthRouteQuest.Wrath,
         FourthRouteQuest.Sloth];
    public static readonly FourthRouteQuest[] LightQuests =
        [FourthRouteQuest.Humility, FourthRouteQuest.Generosity, FourthRouteQuest.Chastity,
         FourthRouteQuest.Benevolence, FourthRouteQuest.Temperance, FourthRouteQuest.Patience,
         FourthRouteQuest.Diligence];

    public static FourthRouteAlignment AlignmentOf(FourthRouteQuest quest) =>
        FourthActEntryRules.AlignmentOf(quest) ?? throw new ArgumentOutOfRangeException(nameof(quest));

    public static bool TryGetQuest(RunState runState, out FourthRouteQuest quest) =>
        Enum.TryParse(M5Progress.Handle.Get(runState).FourthRouteQuestId, out quest) && Enum.IsDefined(quest);

    public static FourthRouteTrialState Trial(RunState run)
    {
        M5ProgressState state = M5Progress.Handle.Get(run);
        if (state.FourthRouteTrial is null)
            M5Progress.Handle.Modify(run, data =>
            {
                var migrated = FourthRouteTrialRules.FromLegacy(
                    data.FourthRouteRelicStage, data.FourthRouteQuestProgress, data.FourthRouteRewardPending);
                if (migrated.Phase == FourthTrialPhase.First && TryGetQuest(run, out var quest)
                    && quest != FourthRouteQuest.Greed)
                {
                    int recorded = migrated.Progress;
                    migrated.Progress = 0;
                    FourthRouteTrialRules.Add(migrated, quest, recorded);
                }
                if (FourthRouteTrialRules.Pending(migrated.Phase) && TryGetQuest(run, out var pendingQuest))
                    migrated.Progress = TargetFor(pendingQuest, FourthRouteTrialRules.Number(migrated.Phase));
                data.FourthRouteTrial = migrated;
            });
        return M5Progress.Handle.Get(run).FourthRouteTrial!;
    }

    private static void ChangeTrial(RunState run, Action<FourthRouteTrialState> change)
    {
        Trial(run);
        M5Progress.Handle.Modify(run, state =>
        {
            change(state.FourthRouteTrial!);
            FourthTrialPhase phase = state.FourthRouteTrial!.Phase;
            state.FourthRouteQuestProgress = state.FourthRouteTrial.Progress;
            state.FourthRouteQuestCompleted = !FourthRouteTrialRules.Active(phase);
            state.FourthRouteRewardPending = FourthRouteTrialRules.Pending(phase);
            state.FourthRouteRewardClaimed = phase is FourthTrialPhase.Fragment or FourthTrialPhase.Sacrifice or FourthTrialPhase.Complete;
        });
    }

    public static void SelectQuest(RunState runState, FourthRouteQuest quest)
    {
        M5Progress.Handle.Modify(runState, state =>
        {
            if (!string.IsNullOrEmpty(state.FourthRouteQuestId)) return;
            state.FourthRouteQuestId = quest.ToString();
            state.FourthRouteAlignment = AlignmentOf(quest).ToString();
            state.FourthRouteQuestProgress = 0;
            state.FourthRouteTrial = new();
        });
    }

    public static async Task EnsureDormantRelic(Player player)
    {
        if (player.Character is not MaidenSuccubusCharacter || player.RunState is not RunState run
            || !TryGetQuest(run, out FourthRouteQuest quest) || M5Progress.Handle.Get(run).FourthRouteRelicStage != 0
            || player.Relics.OfType<FourthRouteRelic>().Any(relic => relic.Quest == quest)) return;
        FourthRouteRelic relic = CreateRelic(quest);
        relic.Stage = 0;
        await RelicCmd.Obtain(relic, player);
        await CheckThresholdQuest(player);
    }

    public static Task AddProgress(Player player, FourthRouteQuest quest, int amount = 1)
    {
        if (player.Character is not MaidenSuccubusCharacter || player.RunState is not RunState runState
            || !TryGetQuest(runState, out FourthRouteQuest active) || active != quest)
            return Task.CompletedTask;
        ChangeTrial(runState, state => FourthRouteTrialRules.Add(state, quest, amount));
        return Task.CompletedTask;
    }

    public static bool HasPendingInitialReward(RunState runState)
    {
        return TryGetQuest(runState, out _) && FourthRouteTrialRules.Pending(Trial(runState).Phase);
    }

    public static async Task ClaimInitialReward(Player player)
    {
        if (player.Character is not MaidenSuccubusCharacter || player.RunState is not RunState runState
            || !HasPendingInitialReward(runState)
            || !TryGetQuest(runState, out FourthRouteQuest quest))
            return;

        ClaimGate gate = Claims.GetValue(runState, _ => new ClaimGate());
        if (gate.Busy) return;
        gate.Busy = true;
        try
        {
            int targetStage = FourthRouteTrialRules.RewardStage(Trial(runState).Phase);
            M5ProgressState before = M5Progress.Handle.Get(runState);
            if (!before.FourthRouteRewardCorruptionApplied)
            {
                M5Progress.Handle.Modify(runState,
                    state => state.FourthRouteRewardCorruptionApplied = true);
                CorruptionCmd.Modify(runState,
                    AlignmentOf(quest) == FourthRouteAlignment.Dark ? 1 : -1,
                    new CorruptionChangeSource($"fourth_route.{quest.ToString().ToLowerInvariant()}"));
            }

            M5Progress.Handle.Modify(runState, state => state.FourthRouteRelicStage = targetStage);
            foreach (FourthRouteRelic old in player.Relics.OfType<FourthRouteRelic>()
                .Where(relic => relic.Quest == quest && relic.Stage != targetStage).ToList())
                await RelicCmd.Remove(old);
            if (targetStage == 2)
                foreach (FourthRouteFragmentRelic fragment in player.Relics.OfType<FourthRouteFragmentRelic>().ToList())
                    await RelicCmd.Remove(fragment);
            if (!player.Relics.OfType<FourthRouteRelic>().Any(relic => relic.Quest == quest && relic.Stage == targetStage))
            {
                FourthRouteRelic relic = CreateRelic(quest);
                relic.Stage = targetStage;
                await RelicCmd.Obtain(relic, player);
            }

            ChangeTrial(runState, state => FourthRouteTrialRules.Claim(state));
            M5Progress.Handle.Modify(runState, state => state.FourthRouteFragmentPending = targetStage == 1);
        }
        finally { gate.Busy = false; }
    }

    public static Task CheckThresholdQuest(Player player)
    {
        if (player.Character is not MaidenSuccubusCharacter || player.RunState is not RunState runState
            || !TryGetQuest(runState, out FourthRouteQuest quest)) return Task.CompletedTask;
        if (quest == FourthRouteQuest.Greed)
            ChangeTrial(runState, state => FourthRouteTrialRules.SetGold(state, player.Gold));
        return Task.CompletedTask;
    }

    public static async Task UnlockSecondTrial(Player player)
    {
        if (player.Character is not MaidenSuccubusCharacter || player.RunState is not RunState runState
            || !TryGetQuest(runState, out _) || Trial(runState).Phase != FourthTrialPhase.Fragment) return;
        ChangeTrial(runState, state => FourthRouteTrialRules.UnlockSecond(state));
        M5Progress.Handle.Modify(runState, state =>
        {
            state.FourthRouteFragmentPending = false;
            state.FourthRouteFragmentPurchased = true;
            state.FourthRouteRewardCorruptionApplied = false;
        });
        await CheckThresholdQuest(player);
    }

    public static async Task UnlockThirdTrial(Player player, int removedCount, bool matchingDirection)
    {
        if (player.Character is not MaidenSuccubusCharacter || player.RunState is not RunState runState
            || !TryGetQuest(runState, out _) || Trial(runState).Phase != FourthTrialPhase.Sacrifice
            || removedCount <= 0 || !matchingDirection) return;
        ChangeTrial(runState, state => FourthRouteTrialRules.UnlockThird(state, removedCount, matchingDirection));
        M5Progress.Handle.Modify(runState, state =>
        {
            state.FourthRouteSacrificeCompleted = true;
            state.FourthRouteRewardCorruptionApplied = false;
        });
        await CheckThresholdQuest(player);
    }

    public static bool HasFourthActQualification(RunState runState)
    {
        M5ProgressState state = M5Progress.Handle.Get(runState);
        FourthRouteAlignment? alignment = TryGetQuest(runState, out FourthRouteQuest quest)
            && Enum.IsDefined(quest) ? AlignmentOf(quest) : null;
        return FourthActEntryRules.Qualifies(
            runState.Players.Any(player => player.Character is MaidenSuccubusCharacter),
            state.FourthRouteRelicStage >= 4 && Trial(runState).Phase == FourthTrialPhase.Complete,
            alignment, CorruptionQuery.Get(runState));
    }

    // Qualification is deliberately NOT an enable switch for the unfinished act.
    public static bool CanEnterFourthAct(RunState runState) =>
        FourthActEntryRules.NormalEntryEnabled && HasFourthActQualification(runState);

    public static void RecordThirdActEnding(RunState runState)
    {
        M5ProgressState state = M5Progress.Handle.Get(runState);
        if (!FourthActEntryRules.ShouldRecordEnding(
            runState.Players.Any(player => player.Character is MaidenSuccubusCharacter),
            runState.CurrentActIndex, state.FourthRouteEndingChecked)) return;
        bool qualifies = HasFourthActQualification(runState);
        M5Progress.Handle.Modify(runState, data =>
        {
            data.FourthRouteThirdBossDefeated = true;
            data.FourthRouteEndingChecked = true;
            data.FourthRouteEndingEligible = qualifies;
        });
    }

    public static int TargetFor(FourthRouteQuest quest, int trial = 1) => FourthRouteTrialRules.Target(quest, trial);

    public static string QuestName(FourthRouteQuest quest) => quest switch
    {
        FourthRouteQuest.Pride => "傲慢", FourthRouteQuest.Greed => "贪婪",
        FourthRouteQuest.Lust => "色欲", FourthRouteQuest.Envy => "嫉妒",
        FourthRouteQuest.Gluttony => "暴食", FourthRouteQuest.Wrath => "愤怒",
        FourthRouteQuest.Sloth => "懒惰", FourthRouteQuest.Humility => "谦逊",
        FourthRouteQuest.Generosity => "慷慨", FourthRouteQuest.Chastity => "贞洁",
        FourthRouteQuest.Benevolence => "仁爱", FourthRouteQuest.Temperance => "节制",
        FourthRouteQuest.Patience => "耐心", _ => "勤勉"
    };

    public static string QuestText(FourthRouteQuest quest, int trial = 1) => FourthRouteTrialRules.Text(quest, trial);

    public static string ProgressText(Player player, FourthRouteQuest quest)
    {
        if (player.RunState is not RunState run || !TryGetQuest(run, out var active) || active != quest) return "";
        FourthRouteTrialState state = Trial(run);
        if (state.Phase == FourthTrialPhase.Fragment) return "取得这件遗物的碎片，以推进试炼。";
        if (state.Phase == FourthTrialPhase.Sacrifice) return "在女神献上祭品，以推进试炼。";
        if (state.Phase == FourthTrialPhase.Complete) return "试炼完成了。允许进入第四层，但因为还没做完，所以进不去XD";
        int trial = FourthRouteTrialRules.Number(state.Phase);
        int target = TargetFor(quest, trial);
        int progress = quest == FourthRouteQuest.Greed && FourthRouteTrialRules.Active(state.Phase)
            ? Math.Clamp(player.Gold, 0, target) : Math.Min(state.Progress, target);
        return $"{QuestText(quest, trial)}\n{progress}/{target}"
            + (FourthRouteTrialRules.Pending(state.Phase) ? "（待领取试炼的奖赏）" : "");
    }

    public static FourthRouteRelic CreateRelicPreview(FourthRouteQuest quest, int stage = 1)
    {
        FourthRouteRelic relic = CreateRelic(quest);
        relic.Stage = stage;
        return relic;
    }

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
