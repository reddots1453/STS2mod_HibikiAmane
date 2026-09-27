using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Characters;

namespace MaidenSuccubus.Acts;

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
        FourthActEntryRules.AlignmentOf(quest) ?? throw new ArgumentOutOfRangeException(nameof(quest));

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

    public static Task AddProgress(Player player, FourthRouteQuest quest, int amount = 1)
    {
        if (player.RunState is not RunState runState || !TryGetQuest(runState, out FourthRouteQuest active)
            || active != quest || M5Progress.Handle.Get(runState).FourthRouteQuestCompleted)
            return Task.CompletedTask;
        int target = TargetFor(quest);
        M5Progress.Handle.Modify(runState, state =>
        {
            state.FourthRouteQuestProgress = Math.Min(target, state.FourthRouteQuestProgress + amount);
            if (state.FourthRouteQuestProgress >= target)
            {
                state.FourthRouteQuestCompleted = true;
                state.FourthRouteRewardPending = true;
            }
        });
        return Task.CompletedTask;
    }

    public static bool HasPendingInitialReward(RunState runState)
    {
        M5ProgressState state = M5Progress.Handle.Get(runState);
        // Legacy saves have no pending flag and therefore do not replay their
        // already-granted stage-one reward. New rewards retain the pending flag
        // until acquisition finishes, so a failed grant can be retried safely.
        return state.FourthRouteQuestCompleted
            && !state.FourthRouteRewardClaimed
            && state.FourthRouteRewardPending
            && TryGetQuest(runState, out _);
    }

    public static async Task ClaimInitialReward(Player player)
    {
        if (player.RunState is not RunState runState
            || !HasPendingInitialReward(runState)
            || !TryGetQuest(runState, out FourthRouteQuest quest))
            return;

        M5ProgressState before = M5Progress.Handle.Get(runState);
        if (!before.FourthRouteRewardCorruptionApplied)
        {
            M5Progress.Handle.Modify(runState,
                state => state.FourthRouteRewardCorruptionApplied = true);
            CorruptionCmd.Modify(runState,
                AlignmentOf(quest) == FourthRouteAlignment.Dark ? 1 : -1,
                new CorruptionChangeSource($"fourth_route.{quest.ToString().ToLowerInvariant()}"));
        }

        M5Progress.Handle.Modify(runState, state =>
        {
            state.FourthRouteRelicStage = Math.Max(1, state.FourthRouteRelicStage);
            state.FourthRouteFragmentPending = true;
        });

        if (!player.Relics.OfType<FourthRouteRelic>().Any(relic => relic.Quest == quest))
        {
            FourthRouteRelic relic = CreateRelic(quest);
            relic.Stage = 1;
            await RelicCmd.Obtain(relic, player);
        }

        M5Progress.Handle.Modify(runState, state =>
        {
            state.FourthRouteRewardPending = false;
            state.FourthRouteRewardClaimed = true;
        });
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
        });
    }

    public static bool HasFourthActQualification(RunState runState)
    {
        M5ProgressState state = M5Progress.Handle.Get(runState);
        FourthRouteAlignment? alignment = TryGetQuest(runState, out FourthRouteQuest quest)
            && Enum.IsDefined(quest) ? AlignmentOf(quest) : null;
        return FourthActEntryRules.Qualifies(
            runState.Players.Any(player => player.Character is MaidenSuccubusCharacter),
            state.FourthRouteRelicStage >= 4, alignment, CorruptionQuery.Get(runState));
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

    public static string QuestText(FourthRouteQuest quest) => quest switch
    {
        FourthRouteQuest.Pride => "进行3场精英战斗。",
        FourthRouteQuest.Greed => "持有至少300金币。",
        FourthRouteQuest.Lust => "在欲望不低于5的状态下结束3场战斗。",
        FourthRouteQuest.Envy => "移除2张牌。",
        FourthRouteQuest.Gluttony => "使用3瓶药水。",
        FourthRouteQuest.Wrath => "在第3回合结束前赢得4场战斗。",
        FourthRouteQuest.Sloth => "在火堆休息2次。",
        FourthRouteQuest.Humility => "升级2张初始牌。",
        FourthRouteQuest.Generosity => "跳过1个宝箱。",
        FourthRouteQuest.Chastity => "以不高于2点欲望的状态结束3场战斗。",
        FourthRouteQuest.Benevolence => "向牌组中加入5张牌。",
        FourthRouteQuest.Temperance => "跳过2次卡牌奖励。",
        FourthRouteQuest.Patience => "进行5场普通战斗。",
        _ => "在火堆锻造3次。"
    };

    public static FourthRouteRelic CreateRelicPreview(FourthRouteQuest quest)
    {
        FourthRouteRelic relic = CreateRelic(quest);
        relic.Stage = 1;
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
