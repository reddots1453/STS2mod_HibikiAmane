using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Characters;
using MaidenSuccubus.ContentTemplates;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Events;

[RegisterSharedEvent]
public sealed class LesserEvil : MSEventTemplate
{
    private const int CurseCount = 5;

    public override string CustomInitialPortraitPath => "res://images/events/whispering_hollow.png";

    public override bool IsAllowed(IRunState runState) =>
        runState.Players.All(player => player.Character is MaidenSuccubusCharacter);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new(this, TakeCurse, InitialOptionKey("CURSE")),
        new(this, EnterChamber, InitialOptionKey("CHAMBER")),
        new(this, Leave, InitialOptionKey("LEAVE")),
    ];

    internal static CardModel CurseAt(int index) => index switch
    {
        0 => ModelDb.Card<GagCurse>(),
        1 => ModelDb.Card<ClimaxBanCurse>(),
        2 => ModelDb.Card<InfatuationCurse>(),
        3 => ModelDb.Card<TransparentOutfitCurse>(),
        4 => ModelDb.Card<AphrodisiacPoisoningCurse>(),
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    private async Task TakeCurse()
    {
        await CardPileCmd.AddCursesToDeck([CurseAt(Rng.NextInt(CurseCount))], Owner!);
        await GrantRareRelic();
        SetEventFinished(PageDescription("CURSE"));
    }

    private async Task EnterChamber()
    {
        await Data.Desire.Modify(Owner!, 6);
        await GrantRareRelic();
        SetEventFinished(PageDescription("CHAMBER"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    private async Task GrantRareRelic()
    {
        await RelicCmd.Obtain(
            RelicFactory.PullNextRelicFromFront(Owner!, RelicRarity.Rare).ToMutable(),
            Owner!);
    }
}
