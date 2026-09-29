#if DEBUG
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Events;

namespace MaidenSuccubus.ConsoleCommands;

internal static class DesignLesserEvilEventContract
{
    internal static async Task Run(Player player, Action<bool, string> check)
    {
        async Task<LesserEvil> Start()
        {
            var model = (LesserEvil)ModelDb.Event<LesserEvil>().ToMutable();
            await model.BeginEvent(player, null, isPreFinished: false);
            check(model.Title.GetFormattedText() == "择祸从轻", "lesser-evil title");
            check(model.CurrentOptions.Count == 3 && model.CurrentOptions.All(option => !option.IsLocked),
                "lesser-evil three unlocked options");
            check(model.CurrentOptions.Select(option => option.Title.GetFormattedText())
                    .SequenceEqual(["咒具盒", "媚毒室", "放弃"]),
                "lesser-evil option order");
            return model;
        }

        Type[] expectedCurseTypes =
        [
            typeof(GagCurse), typeof(ClimaxBanCurse), typeof(InfatuationCurse),
            typeof(TransparentOutfitCurse), typeof(AphrodisiacPoisoningCurse),
        ];
        check(Enumerable.Range(0, expectedCurseTypes.Length)
                .Select(index => LesserEvil.CurseAt(index).GetType())
                .SequenceEqual(expectedCurseTypes), "lesser-evil exact five-curse pool");

        await Data.Desire.Set(player, 0);
        LesserEvil leave = await Start();
        int deckBefore = player.Deck.Cards.Count;
        int relicsBefore = player.Relics.Count;
        int eventRngBefore = leave.Rng.ToSerializable().counter;
        await leave.CurrentOptions[2].Chosen();
        check(leave.IsFinished && leave.Description!.GetFormattedText().StartsWith("你没有碰任何一座装置。"),
            "lesser-evil leave result page");
        check(player.Deck.Cards.Count == deckBefore && player.Relics.Count == relicsBefore
              && Data.Desire.Get(player) == 0 && leave.Rng.ToSerializable().counter == eventRngBefore,
            "lesser-evil leave has no resource or rng effects");

        LesserEvil curse = await Start();
        CardModel[] originalCards = player.Deck.Cards.ToArray();
        RelicModel[] originalRelics = player.Relics.ToArray();
        int desireBefore = Data.Desire.Get(player);
        int curseRngBefore = curse.Rng.ToSerializable().counter;
        var curseOption = curse.CurrentOptions[0];
        await curseOption.Chosen();
        CardModel[] newCards = player.Deck.Cards.Except(originalCards).ToArray();
        RelicModel[] newRelics = player.Relics.Except(originalRelics).ToArray();
        check(newCards.Length == 1 && expectedCurseTypes.Contains(newCards[0].GetType()),
            "lesser-evil grants exactly one whitelisted curse");
        check(newRelics.Length == 1 && (newRelics[0].Rarity == RelicRarity.Rare || newRelics[0] is Circlet),
            "lesser-evil curse branch grants one rare relic or native exhausted-pool fallback");
        check(Data.Desire.Get(player) == desireBefore && curse.Rng.ToSerializable().counter == curseRngBefore + 1,
            "lesser-evil curse uses one event rng draw without desire gain");
        check(curse.IsFinished && curse.Description!.GetFormattedText().StartsWith("你把手伸进铁盒。"),
            "lesser-evil curse result page");
        await curseOption.Chosen();
        check(player.Deck.Cards.Except(originalCards).Count() == 1
              && player.Relics.Except(originalRelics).Count() == 1,
            "lesser-evil curse option cannot grant twice");

        await Data.Desire.Set(player, 0);
        LesserEvil chamber = await Start();
        originalCards = player.Deck.Cards.ToArray();
        originalRelics = player.Relics.ToArray();
        var chamberOption = chamber.CurrentOptions[1];
        await chamberOption.Chosen();
        check(Data.Desire.Get(player) == 6 && player.Deck.Cards.SequenceEqual(originalCards),
            "lesser-evil chamber gains six desire without a curse");
        newRelics = player.Relics.Except(originalRelics).ToArray();
        check(newRelics.Length == 1 && (newRelics[0].Rarity == RelicRarity.Rare || newRelics[0] is Circlet),
            "lesser-evil chamber grants one rare relic or native exhausted-pool fallback");
        check(chamber.IsFinished && chamber.Description!.GetFormattedText().StartsWith("玻璃门在身后锁死。"),
            "lesser-evil chamber result page");
        await chamberOption.Chosen();
        check(Data.Desire.Get(player) == 6 && player.Relics.Except(originalRelics).Count() == 1,
            "lesser-evil chamber cannot gain desire or relic twice");
    }
}
#endif
