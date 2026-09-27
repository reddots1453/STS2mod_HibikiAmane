using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Relics;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Events;

[RegisterSharedEvent]
public sealed class DarvAssistance : MSEventTemplate
{
    public override string CustomInitialPortraitPath => "res://images/events/whispering_hollow.png";

    public override bool IsAllowed(IRunState runState) =>
        runState.CurrentActIndex == 2
        && runState.Players.All(player => player.Character is MaidenSuccubusCharacter);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new(this, Corruption >= 3 ? DarkInsight : null,
            InitialOptionKey(Corruption >= 3 ? "DARK" : "DARK_LOCKED")),
        new(this, Corruption <= -3 ? HolyInsight : null,
            InitialOptionKey(Corruption <= -3 ? "HOLY" : "HOLY_LOCKED")),
        new(this, Corruption is > -3 and < 3 ? BalancedInsight : null,
            InitialOptionKey(Corruption is > -3 and < 3 ? "BALANCE" : "BALANCE_LOCKED"),
            HoverTipFactory.FromRelic<SoulCompass>()),
    ];

    internal static CardReward CreateInsightReward(Player player, RouteCardKind route, Rng rng)
    {
        CardPoolModel pool = route switch
        {
            RouteCardKind.Holy => ModelDb.CardPool<MSHolyCardPool>(),
            RouteCardKind.Corrupt => ModelDb.CardPool<MSCorruptCardPool>(),
            _ => throw new ArgumentOutOfRangeException(nameof(route)),
        };
        // Event RNG and a fixed rare route pool. Ordinary encounter replacement
        // must not turn this explicitly promised reward into a different route.
        var options = CardCreationOptions.ForNonCombatWithUniformOdds(
            [pool], card => card.Rarity == CardRarity.Rare)
            .WithFlags(CardCreationFlags.NoCardPoolModifications)
            .WithRngOverride(rng);
        return new CardReward(options, 3, player);
    }

    private Task DarkInsight() => ResolveInsight(RouteCardKind.Corrupt, "DARK");
    private Task HolyInsight() => ResolveInsight(RouteCardKind.Holy, "HOLY");

    private async Task ResolveInsight(RouteCardKind route, string page)
    {
        if (route == RouteCardKind.Corrupt ? Corruption < 3 : Corruption > -3) return;
        await RewardsCmd.OfferCustom(Owner!, [CreateInsightReward(Owner!, route, Rng)]);
        SetEventFinished(PageDescription(page));
    }

    private async Task BalancedInsight()
    {
        if (Corruption is <= -3 or >= 3) return;
        await RelicCmd.Obtain<SoulCompass>(Owner!);
        SetEventFinished(PageDescription("BALANCE"));
    }
}
