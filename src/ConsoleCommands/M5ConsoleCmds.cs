using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;
using MaidenSuccubus.Merchant;
using MaidenSuccubus.Rewards;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class M5AddInvasionCurseConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_add_invasion_curse";
    public override string Args => "<sourced|plain> [sourceId]";
    public override string Description =>
        "Add a permanent Semen curse with or without an invasion source marker";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || args.Length == 0)
        {
            return new CmdResult(false, "Expected sourced or plain in a MaidenSuccubus run.");
        }
        bool sourced = args[0].Equals("sourced", StringComparison.OrdinalIgnoreCase);
        bool plain = args[0].Equals("plain", StringComparison.OrdinalIgnoreCase);
        if (!sourced && !plain)
        {
            return new CmdResult(false, "Expected sourced or plain.");
        }

        var curse = ((RunState)issuingPlayer.RunState)
            .CreateCard<SemenCurse>(issuingPlayer);
        curse.SourceMonsterId = sourced
            ? (args.Length > 1 ? args[1] : "M5_DEBUG_INVADER")
            : "";
        Task task = CardPileCmd.Add(curse, PileType.Deck);
        return new CmdResult(
            task,
            true,
            $"Adding {(sourced ? "sourced" : "plain")} Semen curse to deck.");
    }
}

public sealed class M5MerchantCurseConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_shop_curse";
    public override string Args => string.Empty;
    public override string Description =>
        "Remove all semen curses, refunding the fixed 50 gold each";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter)
        {
            return new CmdResult(false, "Use this in a MaidenSuccubus run.");
        }
        int count = InvasionCurseMerchantService.GetEligible(issuingPlayer).Count;
        if (count == 0)
        {
            return new CmdResult(false, "No invasion-sourced curse exists in the deck.");
        }
        Task task = InvasionCurseMerchantService.SelectAndRemove(issuingPlayer);
        return new CmdResult(
            task,
            true,
            $"Removing {count} semen curses; refund="
            + $"{count * InvasionCurseMerchantConfig.RefundGoldPerCurse}.");
    }
}

public sealed class M5BlessingConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_blessing";
    public override string Args => "offer";
    public override string Description => "Open the independent goddess blessing reward page";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter)
        {
            return new CmdResult(false, "Use this in a MaidenSuccubus run.");
        }
        if (args.Length != 1
            || !args[0].Equals("offer", StringComparison.OrdinalIgnoreCase))
        {
            return new CmdResult(false, "Expected: ms_blessing offer");
        }
        return new CmdResult(
            GoddessBlessingService.Offer(issuingPlayer),
            true,
            "Opening goddess blessing reward page.");
    }
}

public sealed class M5StateConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_m5_state";
    public override string Args => string.Empty;
    public override string Description => "Dump M5 start, merchant, and blessing state";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.RunState is not RunState runState)
        {
            return new CmdResult(false, "No active run.");
        }
        M5ProgressState state = M5Progress.Handle.Get(runState);
        int corruption = CorruptionQuery.Get(runState);
        int curses = InvasionCurseMerchantService.GetEligible(issuingPlayer).Count;
        return new CmdResult(
            true,
            $"profile={state.StartProfileId}; applied={state.StartProfileApplied}; "
            + $"corruption={corruption}; invasionCurses={curses}; "
            + $"refundEach={InvasionCurseMerchantConfig.RefundGoldPerCurse}; "
            + $"blessingActs=[{string.Join(",", state.BlessingOfferedActs.Order())}]");
    }
}
