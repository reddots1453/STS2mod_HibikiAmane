#if DEBUG
using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Data;
using MaidenSuccubus.Patches;
using MaidenSuccubus.Relics;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>Destructive route/model tests, not a substitute for the natural opening UI hand test.</summary>
public sealed class DesignRouteOpeningTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_route_opening";
    public override string Args => "confirm";
    public override string Description => "Destructive saved opening/route tests; disposable single-player run only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (_running || player?.Character is not MaidenSuccubusCharacter || player.RunState is not RunState run
            || run.Players.Count != 1 || CombatManager.Instance.IsInProgress || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_route_opening confirm outside combat in a disposable single-player Maiden run.");
        return new CmdResult(Run(player, run), true, "Destructive route tests started; see [DS27OpeningTest].");
    }

    private static async Task Run(Player player, RunState run)
    {
        _running = true;
        bool previous = TestMode.IsOn;
        int checks = 0;
        void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException("DS27 opening: " + name);
            checks++;
        }
        try
        {
            TestMode.IsOn = true;
            MethodBase target = FourthRouteOpeningPatch.TargetMethod();
            Check(Harmony.GetPatchInfo(target)?.Transpilers.Any(p => p.PatchMethod.DeclaringType == typeof(FourthRouteOpeningPatch)) == true,
                "actual async MoveNext patch installed");
            var il = PatchProcessor.GetOriginalInstructions(target).ToArray();
            var rewritten = FourthRouteOpeningPatch.Transpiler(il, target).ToArray();
            Check(rewritten.Count(instruction => instruction.operand is MethodInfo method
                && method.DeclaringType == typeof(FourthRouteOpeningPatch) && method.Name == "WaitForOpening") == 1,
                "actual game IL gets exactly one awaited bridge");
            var wait = new TaskCompletionSource();
            Task gated = FourthRouteOpeningPatch.WaitForOpening(wait.Task, null!);
            Check(!gated.IsCompleted, "bridge awaits original task even in test mode");
            wait.SetResult();
            await gated;
            Check(gated.IsCompletedSuccessfully, "test mode never creates UI");

            foreach (var quest in Enum.GetValues<FourthRouteQuest>())
            {
                foreach (var relic in player.Relics.Where(r => r is FourthRouteRelic or FourthRouteFragmentRelic).ToArray())
                    await RelicCmd.Remove(relic);
                M5Progress.Handle.Modify(run, data =>
                {
                    data.FourthRouteQuestId = "";
                    data.FourthRouteAlignment = "";
                    data.FourthRouteQuestProgress = 42; // Must not be inherited by a new choice.
                    data.FourthRouteQuestCompleted = false;
                    data.FourthRouteRelicStage = 0;
                    data.FourthRouteTrial = null;
                    data.FourthRouteOpening = null;
                    data.FourthRouteRewardPending = false;
                    data.FourthRouteRewardClaimed = false;
                    data.FourthRouteRewardCorruptionApplied = false;
                    data.FourthRouteFragmentPending = false;
                    data.FourthRouteFragmentOffered = false;
                    data.FourthRouteFragmentPurchased = false;
                    data.FourthRouteSacrificeCompleted = false;
                    data.FourthRouteEndingChecked = false;
                    data.FourthRouteEndingEligible = false;
                });
                var opening = FourthRouteOpeningService.Prepare(run);
                var offers = (opening.Dark, opening.Light);
                FourthRouteOpeningService.Prepare(run);
                Check(offers == (opening.Dark, opening.Light), "reopening preserves actual stored random offers");
                bool dark = FourthRouteProgressService.AlignmentOf(quest) == FourthRouteAlignment.Dark;
                var explicitOffers = new FourthRouteOpeningState();
                explicitOffers.Offer(dark ? quest : FourthRouteQuest.Pride, dark ? FourthRouteQuest.Humility : quest);
                M5Progress.Handle.Modify(run, data => data.FourthRouteOpening = explicitOffers);
                Player foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
                foreign.RunState = run;
                Check(!await FourthRouteOpeningService.Confirm(foreign, quest), "other character cannot select route");
                Check(await FourthRouteOpeningService.Confirm(player, quest), "actual route confirmation");
                Check(FourthRouteProgressService.TryGetQuest(run, out var actual) && actual == quest, "chosen route saved immediately");
                Check(player.Relics.OfType<FourthRouteRelic>().Count() == 1
                    && player.Relics.OfType<FourthRouteRelic>().Single().Stage == 0, "exactly one dormant relic obtained");
                Check(M5Progress.Handle.Get(run).FourthRouteQuestProgress != 42, "pre-choice counter not inherited");
                Check(FourthRouteOpeningService.NeedsOpening(run), "narrative remains pending after confirmation");
                Check(await FourthRouteOpeningService.Confirm(player, quest), "resume same locked selection allowed");
                Check(player.Relics.OfType<FourthRouteRelic>().Count() == 1, "resume cannot duplicate dormant relic");
                Check(!await FourthRouteOpeningService.Confirm(player, dark ? FourthRouteQuest.Humility : FourthRouteQuest.Pride),
                    "opposite route rejected after confirmation");
                var restored = JsonSerializer.Deserialize<FourthRouteOpeningState>(JsonSerializer.Serialize(M5Progress.Handle.Get(run).FourthRouteOpening))!;
                M5Progress.Handle.Modify(run, data => data.FourthRouteOpening = restored);
                Check(FourthRouteOpeningService.Finish(run), "restored narrative can finish");
                Check(!FourthRouteOpeningService.Finish(run) && !FourthRouteOpeningService.NeedsOpening(run), "finished narrative not repeated");
                string story = FourthRouteOpeningScreen.TextFor(quest + ".story");
                string flavor = FourthRouteOpeningScreen.TextFor(quest + ".flavor");
                Check(!string.IsNullOrWhiteSpace(story) && !story.Contains("MAIDEN_SUCCUBUS_ROUTE_OPENING"), "actual localized story resolves");
                Check(!string.IsNullOrWhiteSpace(flavor) && !flavor.Contains("试炼：") && !flavor.Contains("奖励："), "flavor has no stale mechanics");
            }
            MaidenSuccubusMod.Logger.Info($"[DS27OpeningTest] PASS {checks}; UI, natural first-run and full save/load not executed by this command.");
        }
        catch (Exception ex) { MaidenSuccubusMod.Logger.Error("[DS27OpeningTest] FAIL " + ex); throw; }
        finally { TestMode.IsOn = previous; _running = false; }
    }
}
#endif
