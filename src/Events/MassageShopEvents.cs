using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Characters;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Relics;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Events;

internal static class MassageShopRewards
{
    internal static async Task Add<T>(MegaCrit.Sts2.Core.Entities.Players.Player player)
        where T : CardModel
    {
        var result = await CardPileCmd.Add(
            player.RunState.CreateCard<T>(player), PileType.Deck);
        CardCmd.PreviewCardPileAdd(result, style: CardPreviewStyle.EventLayout);
    }

    internal static async Task Replace<TOld, TNew>(
        MegaCrit.Sts2.Core.Entities.Players.Player player)
        where TOld : CardModel where TNew : CardModel
    {
        CardModel? old = player.Deck.Cards.OfType<TOld>().FirstOrDefault();
        if (old != null) await CardPileCmd.RemoveFromDeck(old);
        await Add<TNew>(player);
    }
}

[RegisterSharedEvent]
public sealed class MassageShopFirst : MSEventTemplate
{
    public override string CustomInitialPortraitPath => "res://images/events/whispering_hollow.png";

    public override bool IsAllowed(IRunState runState) =>
        runState.CurrentActIndex == 0
        && runState.Players.Count > 0
        && runState.Players.All(player =>
            player.Character is MaidenSuccubusCharacter && player.Gold >= 100);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new(this, General, InitialOptionKey("GENERAL")),
        new(this, Special, InitialOptionKey("SPECIAL")),
        new(this, Refuse, InitialOptionKey("REFUSE")),
    ];

    private async Task General()
    {
        if (Owner!.Gold < 100) return;
        await PlayerCmd.LoseGold(100, Owner, GoldLossType.Spent);
        await RelicCmd.Obtain<Refreshed>(Owner);
        SetEventFinished(PageDescription("GENERAL"));
    }

    private async Task Special()
    {
        if (Owner!.Gold < 50) return;
        await PlayerCmd.LoseGold(50, Owner, GoldLossType.Spent);
        await MassageShopRewards.Add<LewdMarkMinorCurse>(Owner);
        MassageAppointmentService.Schedule((RunState)Owner.RunState, 2);
        SetEventFinished(PageDescription("SPECIAL"));
    }

    private Task Refuse()
    {
        SetEventFinished(PageDescription("REFUSE"));
        return Task.CompletedTask;
    }
}

[RegisterSharedEvent]
public sealed class MassageShopSecond : MSEventTemplate
{
    public override string CustomInitialPortraitPath => "res://images/events/whispering_hollow.png";
    public override bool IsAllowed(IRunState runState) => false;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new(this, Special, InitialOptionKey("SPECIAL")),
        new(this, Desire < 5 ? Refuse : null,
            InitialOptionKey(Desire < 5 ? "REFUSE" : "REFUSE_LOCKED")),
    ];

    private async Task Special()
    {
        await MassageShopRewards.Replace<LewdMarkMinorCurse, LewdMarkSpreadCurse>(Owner!);
        CorruptionCmd.Modify((RunState)Owner!.RunState, 1,
            new CorruptionChangeSource("massage_shop.second.special"));
        MassageAppointmentService.Schedule((RunState)Owner.RunState, 3);
        SetEventFinished(PageDescription("SPECIAL"));
    }

    private Task Refuse()
    {
        if (Desire >= 5) return Task.CompletedTask;
        CorruptionCmd.Modify((RunState)Owner!.RunState, -1,
            new CorruptionChangeSource("massage_shop.second.refuse"));
        MassageAppointmentService.Cancel((RunState)Owner.RunState);
        SetEventFinished(PageDescription("REFUSE"));
        return Task.CompletedTask;
    }
}

[RegisterSharedEvent]
public sealed class MassageShopThird : MSEventTemplate
{
    public override string CustomInitialPortraitPath => "res://images/events/whispering_hollow.png";
    public override bool IsAllowed(IRunState runState) => false;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new(this, Special, InitialOptionKey("SPECIAL")),
        new(this, CannotLeave, InitialOptionKey("REFUSE")),
    ];

    private async Task Special()
    {
        await MassageShopRewards.Replace<LewdMarkSpreadCurse, LewdMarkCompleteCurse>(Owner!);
        CorruptionCmd.Modify((RunState)Owner!.RunState, 1,
            new CorruptionChangeSource("massage_shop.third.special"));
        MassageAppointmentService.Cancel((RunState)Owner.RunState);
        SetEventFinished(PageDescription("SPECIAL"));
    }

    private Task CannotLeave()
    {
        SetEventState(PageDescription("CANNOT_LEAVE"),
            [new EventOption(this, Special, InitialOptionKey("SPECIAL"))]);
        return Task.CompletedTask;
    }
}
