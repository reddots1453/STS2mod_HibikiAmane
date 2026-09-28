using System.Text.Json;
using MaidenSuccubus.Acts;

internal static class FourthRouteTrialContracts
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            checks++;
        }
        FourthRouteTrialState RoundTrip(FourthRouteTrialState state)
        {
            string json = JsonSerializer.Serialize(state);
            var restored = JsonSerializer.Deserialize<FourthRouteTrialState>(json)!;
            Check(restored.Phase == state.Phase && restored.Progress == state.Progress, "saved phase/progress round trip");
            Check(!ReferenceEquals(restored, state), "saved state does not alias");
            return restored;
        }

        // Independent literal oracle, not derived from the production catalog.
        Dictionary<FourthRouteQuest, int[]> targets = new()
        {
            [FourthRouteQuest.Pride] = [2, 2, 2], [FourthRouteQuest.Greed] = [200, 300, 500],
            [FourthRouteQuest.Lust] = [2, 2, 2], [FourthRouteQuest.Envy] = [1, 1, 1],
            [FourthRouteQuest.Gluttony] = [2, 2, 2], [FourthRouteQuest.Wrath] = [2, 2, 2],
            [FourthRouteQuest.Sloth] = [1, 1, 1], [FourthRouteQuest.Humility] = [1, 1, 1],
            [FourthRouteQuest.Generosity] = [1, 1, 1], [FourthRouteQuest.Chastity] = [2, 2, 2],
            [FourthRouteQuest.Benevolence] = [4, 4, 5], [FourthRouteQuest.Temperance] = [2, 2, 3],
            [FourthRouteQuest.Patience] = [4, 4, 4], [FourthRouteQuest.Diligence] = [2, 2, 2]
        };
        Check(targets.Count == 14, "every route represented");
        foreach (var (quest, expected) in targets)
        {
            var state = new FourthRouteTrialState();
            Check(!FourthRouteTrialRules.Claim(state), "cannot claim unfinished first trial");
            Check(!FourthRouteTrialRules.UnlockSecond(state), "cannot buy fragment before first reward");
            Check(!FourthRouteTrialRules.UnlockThird(state, 1, true), "cannot sacrifice before complete relic");
            for (int trial = 1; trial <= 3; trial++)
            {
                int target = expected[trial - 1];
                Check(FourthRouteTrialRules.Target(quest, trial) == target, $"{quest} trial {trial} target");
                Check(FourthRouteTrialRules.Number(state.Phase) == trial, "correct active trial");
                Check(state.Progress == 0, "unlocked trial starts at zero");
                Check(FourthRouteTrialRules.RewardStage(state.Phase) == 0, "no reward while active");
                Check(!FourthRouteTrialRules.Add(state, quest, -1), "negative additions rejected");
                Check(!FourthRouteTrialRules.Add(state, quest, 0), "zero additions rejected");
                Check(state.Progress == 0, "invalid additions leave state unchanged");
                if (quest == FourthRouteQuest.Greed)
                {
                    Check(!FourthRouteTrialRules.Add(state, quest, int.MaxValue), "gold is state not accumulated events");
                    Check(!FourthRouteTrialRules.SetGold(state, target - 1), "one gold below threshold");
                    Check(!FourthRouteTrialRules.SetGold(state, 0) && state.Progress == 0, "spending lowers active gold progress");
                    Check(FourthRouteTrialRules.SetGold(state, target), "held gold exactly meets threshold");
                }
                else
                {
                    Check(!FourthRouteTrialRules.Add(state, quest, target - 1), "one count below target");
                    Check(state.Progress == target - 1, "intermediate progress exact");
                    state = RoundTrip(state);
                    Check(FourthRouteTrialRules.Add(state, quest, int.MaxValue), "large additions clamp without overflow");
                }
                Check(state.Progress == target, "completed progress capped");
                Check(FourthRouteTrialRules.Pending(state.Phase), "completion waits for claim");
                Check(FourthRouteTrialRules.RewardStage(state.Phase) == new[] { 1, 2, 4 }[trial - 1], "new reward stage, no empty fourth reward");
                Check(!FourthRouteTrialRules.Add(state, quest, 1), "pending cannot accrue future progress");
                Check(!FourthRouteTrialRules.SetGold(state, 0), "pending gold trial never uncompletes after spending");
                Check(!FourthRouteTrialRules.UnlockSecond(state), "cannot unlock before claiming");
                state = RoundTrip(state);
                Check(FourthRouteTrialRules.Claim(state), "claim completed trial once");
                Check(!FourthRouteTrialRules.Claim(state), "duplicate claim rejected");
                Check(!FourthRouteTrialRules.Add(state, quest, int.MaxValue), "waiting does not accumulate counts");
                Check(!FourthRouteTrialRules.SetGold(state, int.MaxValue), "waiting does not satisfy future gold trial");
                state = RoundTrip(state);
                if (trial == 1)
                {
                    Check(state.Phase == FourthTrialPhase.Fragment, "first reward waits for fragment");
                    Check(!FourthRouteTrialRules.UnlockThird(state, 4, true), "sacrifice cannot skip second trial");
                    Check(FourthRouteTrialRules.UnlockSecond(state), "fragment only unlocks second");
                    Check(!FourthRouteTrialRules.UnlockSecond(state), "duplicate fragment does not reset progress");
                }
                else if (trial == 2)
                {
                    Check(state.Phase == FourthTrialPhase.Sacrifice, "second reward waits for sacrifice");
                    Check(!FourthRouteTrialRules.UnlockThird(state, 0, true), "no removed card no unlock");
                    Check(!FourthRouteTrialRules.UnlockThird(state, 1, false), "opposite sealed direction no unlock");
                    Check(FourthRouteTrialRules.UnlockThird(state, 1, true), "matching real removal only unlocks third");
                    Check(!FourthRouteTrialRules.UnlockThird(state, 1, true), "duplicate sacrifice no reset");
                }
                else
                {
                    Check(state.Phase == FourthTrialPhase.Complete, "three rewards complete route");
                    Check(!FourthRouteTrialRules.UnlockSecond(state), "finished cannot reopen second");
                    Check(!FourthRouteTrialRules.UnlockThird(state, 1, true), "finished cannot reopen third");
                }
            }
        }
        // Separate room/turn/desire predicates, including exact inclusive bounds.
        for (int trial = 1; trial <= 3; trial++)
        {
            for (int desire = 0; desire <= 10; desire++)
            {
                int min = new[] { 4, 5, 6 }[trial - 1];
                int max = new[] { 2, 1, 0 }[trial - 1];
                Check(FourthRouteTrialRules.CountsCombat(FourthRouteQuest.Lust, trial, false, true, 1, desire) == (desire >= min), "desire lower bound");
                Check(FourthRouteTrialRules.CountsCombat(FourthRouteQuest.Chastity, trial, false, true, 1, desire) == (desire <= max), "desire upper bound");
            }
            foreach (int round in new[] { 0, 1, 2, 3, 4, 10 })
                Check(FourthRouteTrialRules.CountsCombat(FourthRouteQuest.Wrath, trial, false, false, round, 0)
                    == (round is 1 or 2 or 3), "wrath end of third turn inclusive");
            foreach (bool elite in new[] { false, true })
            foreach (bool normal in new[] { false, true })
            {
                Check(FourthRouteTrialRules.CountsCombat(FourthRouteQuest.Pride, trial, elite, normal, 1, 0) == elite, "elite filter");
                Check(FourthRouteTrialRules.CountsCombat(FourthRouteQuest.Patience, trial, elite, normal, 1, 0) == normal, "normal filter");
            }
        }
        FourthTrialPhase[] legacyPhases = [FourthTrialPhase.First, FourthTrialPhase.Fragment,
            FourthTrialPhase.Sacrifice, FourthTrialPhase.Third, FourthTrialPhase.Complete];
        for (int stage = 0; stage <= 4; stage++)
        {
            var restored = FourthRouteTrialRules.FromLegacy(stage, 7, false);
            Check(restored.Phase == legacyPhases[stage], "legacy preserves earned reward, no invented final completion");
            Check(restored.Progress == (stage == 0 ? 7 : 0), "legacy future trials do not inherit counts");
            RoundTrip(restored);
        }
        Check(FourthRouteTrialRules.FromLegacy(1, 0, true).Phase == FourthTrialPhase.FirstReward, "interrupted first reward remains pending");
        foreach (int invalid in new[] { 0, 4, -1 })
        {
            bool threw = false;
            try { FourthRouteTrialRules.Target(FourthRouteQuest.Pride, invalid); }
            catch (ArgumentOutOfRangeException) { threw = true; }
            Check(threw, "invalid trial rejected");
        }
        return checks;
    }
}
