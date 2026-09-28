namespace MaidenSuccubus.Acts;

public enum FourthTrialPhase
{
    First, FirstReward, Fragment, Second, SecondReward, Sacrifice, Third, ThirdReward, Complete
}

public sealed class FourthRouteTrialState
{
    public FourthTrialPhase Phase { get; set; }
    public int Progress { get; set; }
}

/// <summary>One authoritative catalog for all 42 trials and their transition boundaries.</summary>
public static class FourthRouteTrialRules
{
    public static int Number(FourthTrialPhase phase) => phase switch
    {
        FourthTrialPhase.First or FourthTrialPhase.FirstReward or FourthTrialPhase.Fragment => 1,
        FourthTrialPhase.Second or FourthTrialPhase.SecondReward or FourthTrialPhase.Sacrifice => 2,
        FourthTrialPhase.Third or FourthTrialPhase.ThirdReward or FourthTrialPhase.Complete => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(phase))
    };
    public static bool Active(FourthTrialPhase phase) =>
        phase is FourthTrialPhase.First or FourthTrialPhase.Second or FourthTrialPhase.Third;
    public static bool Pending(FourthTrialPhase phase) =>
        phase is FourthTrialPhase.FirstReward or FourthTrialPhase.SecondReward or FourthTrialPhase.ThirdReward;
    public static int RewardStage(FourthTrialPhase phase) => phase switch
    {
        FourthTrialPhase.FirstReward => 1, FourthTrialPhase.SecondReward => 2,
        FourthTrialPhase.ThirdReward => 4, _ => 0
    };

    public static int Target(FourthRouteQuest quest, int trial)
    {
        if (trial is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(trial));
        return quest switch
        {
            FourthRouteQuest.Pride or FourthRouteQuest.Lust or FourthRouteQuest.Gluttony
                or FourthRouteQuest.Wrath or FourthRouteQuest.Chastity or FourthRouteQuest.Diligence => 2,
            FourthRouteQuest.Envy or FourthRouteQuest.Sloth or FourthRouteQuest.Humility
                or FourthRouteQuest.Generosity => 1,
            FourthRouteQuest.Greed => trial switch { 1 => 200, 2 => 300, _ => 500 },
            FourthRouteQuest.Benevolence => trial == 3 ? 5 : 4,
            FourthRouteQuest.Temperance => trial == 3 ? 3 : 2,
            FourthRouteQuest.Patience => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(quest))
        };
    }

    public static string Text(FourthRouteQuest quest, int trial)
    {
        int count = Target(quest, trial);
        return quest switch
        {
            FourthRouteQuest.Pride => $"进行{count}场精英战斗。",
            FourthRouteQuest.Greed => $"持有至少{count}金币。",
            FourthRouteQuest.Lust => $"在欲望不低于{trial + 3}的状态下结束{count}场战斗。",
            FourthRouteQuest.Envy => $"移除{count}张牌。",
            FourthRouteQuest.Gluttony => $"使用{count}瓶药水。",
            FourthRouteQuest.Wrath => $"在第3回合结束前赢得{count}场战斗。",
            FourthRouteQuest.Sloth => $"在火堆休息{count}次。",
            FourthRouteQuest.Humility => $"升级{count}张初始牌。",
            FourthRouteQuest.Generosity => $"放弃{count}个遗物。",
            FourthRouteQuest.Chastity => trial == 3 ? "以0点欲望的状态结束2场战斗。"
                : $"以不高于{3 - trial}点欲望的状态结束{count}场战斗。",
            FourthRouteQuest.Benevolence => trial == 1 ? $"向牌组中加入{count}张牌。" : $"在牌组中再加入{count}张牌。",
            FourthRouteQuest.Temperance => $"跳过{count}次卡牌奖励。",
            FourthRouteQuest.Patience => $"进行{count}场普通战斗。",
            _ => $"在火堆锻造{count}次。"
        };
    }

    public static bool CountsCombat(FourthRouteQuest quest, int trial, bool elite, bool normal, int round, int desire) =>
        quest switch
        {
            FourthRouteQuest.Pride => elite,
            FourthRouteQuest.Patience => normal,
            FourthRouteQuest.Wrath => round is >= 1 and <= 3,
            FourthRouteQuest.Lust => desire >= trial + 3,
            FourthRouteQuest.Chastity => desire >= 0 && desire <= 3 - trial,
            _ => false
        };

    public static bool Add(FourthRouteTrialState state, FourthRouteQuest quest, int amount)
    {
        if (!Active(state.Phase) || amount <= 0 || quest == FourthRouteQuest.Greed) return false;
        return SetProgress(state, quest, (long)state.Progress + amount);
    }
    public static bool SetGold(FourthRouteTrialState state, int gold) =>
        Active(state.Phase) && SetProgress(state, FourthRouteQuest.Greed, gold);

    private static bool SetProgress(FourthRouteTrialState state, FourthRouteQuest quest, long value)
    {
        int target = Target(quest, Number(state.Phase));
        state.Progress = (int)Math.Clamp(value, 0L, target);
        if (state.Progress < target) return false;
        state.Phase = state.Phase switch
        {
            FourthTrialPhase.First => FourthTrialPhase.FirstReward,
            FourthTrialPhase.Second => FourthTrialPhase.SecondReward,
            _ => FourthTrialPhase.ThirdReward
        };
        return true;
    }

    public static bool UnlockSecond(FourthRouteTrialState state)
    {
        if (state.Phase != FourthTrialPhase.Fragment) return false;
        state.Phase = FourthTrialPhase.Second;
        state.Progress = 0;
        return true;
    }
    public static bool UnlockThird(FourthRouteTrialState state, int removedCount, bool matchingDirection)
    {
        if (state.Phase != FourthTrialPhase.Sacrifice || removedCount <= 0 || !matchingDirection) return false;
        state.Phase = FourthTrialPhase.Third;
        state.Progress = 0;
        return true;
    }
    public static bool Claim(FourthRouteTrialState state)
    {
        if (!Pending(state.Phase)) return false;
        state.Phase = state.Phase switch
        {
            FourthTrialPhase.FirstReward => FourthTrialPhase.Fragment,
            FourthTrialPhase.SecondReward => FourthTrialPhase.Sacrifice,
            _ => FourthTrialPhase.Complete
        };
        return true;
    }

    // Preserve rewards already obtained under the old flow, never replay their
    // pickup effects. Old stage 3 unlocked the final trial; it did not finish it.
    public static FourthRouteTrialState FromLegacy(int stage, int progress, bool pending) => new()
    {
        Phase = stage switch
        {
            >= 4 => FourthTrialPhase.Complete,
            3 => FourthTrialPhase.Third,
            2 => FourthTrialPhase.Sacrifice,
            1 when !pending => FourthTrialPhase.Fragment,
            _ => pending ? FourthTrialPhase.FirstReward : FourthTrialPhase.First
        },
        Progress = stage == 0 ? Math.Max(0, progress) : 0
    };
}
