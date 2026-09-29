#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Events;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

internal static class DesignSuspiciousShopEventContract
{
    internal static async Task Run(Player player, Action<bool, string> check)
    {
        foreach (RelicModel relic in player.Relics.ToArray())
            await RelicCmd.Remove(relic);
        var run = (RunState)player.RunState;
        async Task<SuspiciousShop> Start()
        {
            var model = (SuspiciousShop)ModelDb.Event<SuspiciousShop>().ToMutable();
            await model.BeginEvent(player, null, isPreFinished: false);
            check(model.Title.GetFormattedText() == "可疑的商店" && model.CurrentOptions.Count == 2,
                "suspicious shop title and two options");
            return model;
        }

        Type[] inventory = [typeof(Blindfold), typeof(Vibrator), typeof(Milker)];
        check(Enumerable.Range(0, 3).Select(index => SuspiciousShop.RelicAt(index).GetType())
            .SequenceEqual(inventory), "suspicious shop exact three-relic inventory");

        CorruptionCmd.Set(run, 1);
        SuspiciousShop locked = await Start();
        check(locked.CurrentOptions[0].IsLocked && !locked.CurrentOptions[1].IsLocked,
            "suspicious shop work locked below +2");
        check(locked.CurrentOptions[0].Title.GetFormattedText() == "[需要+2或更高堕落值] 打工",
            "suspicious shop locked title");

        CorruptionCmd.Set(run, 2);
        await Data.Desire.Set(player, 0);
        SuspiciousShop work = await Start();
        check(!work.CurrentOptions[0].IsLocked, "suspicious shop work unlocks at +2");
        int goldBefore = (int)player.Gold;
        int rngBefore = work.Rng.ToSerializable().counter;
        var workOption = work.CurrentOptions[0];
        await workOption.Chosen();
        int goldGain = (int)player.Gold - goldBefore;
        check(goldGain is >= 175 and <= 200 && work.Rng.ToSerializable().counter == rngBefore + 1,
            "suspicious shop work uses one event roll in inclusive gold range");
        check(CorruptionQuery.Get(run) == 3 && Data.Desire.Get(player) == 2,
            "suspicious shop work applies corruption and desire");
        check(work.IsFinished && work.Description!.GetFormattedText().StartsWith("帘幕在你身后合拢。"),
            "suspicious shop work result page");
        await workOption.Chosen();
        check((int)player.Gold == goldBefore + goldGain && CorruptionQuery.Get(run) == 3
              && Data.Desire.Get(player) == 2, "suspicious shop work cannot pay twice");

        CorruptionCmd.Set(run, 0);
        SuspiciousShop browse = await Start();
        var relicsBefore = player.Relics.ToHashSet();
        goldBefore = (int)player.Gold;
        rngBefore = browse.Rng.ToSerializable().counter;
        var browseOption = browse.CurrentOptions[1];
        await browseOption.Chosen();
        var granted = player.Relics.Where(relic => !relicsBefore.Contains(relic)).ToArray();
        check(granted.Length == 1 && inventory.Contains(granted[0].GetType()),
            "suspicious shop grants exactly one inventory relic");
        check(browse.Rng.ToSerializable().counter == rngBefore + 1
              && (int)player.Gold == goldBefore && CorruptionQuery.Get(run) == 0
              && Data.Desire.Get(player) == 2,
            "suspicious shop browse consumes only one event roll");
        check(browse.IsFinished && browse.Description!.GetFormattedText().StartsWith("“第一次光顾"),
            "suspicious shop browse result page");
        await browseOption.Chosen();
        check(player.Relics.Count(relic => !relicsBefore.Contains(relic)) == 1,
            "suspicious shop browse cannot grant twice");
    }
}
#endif
