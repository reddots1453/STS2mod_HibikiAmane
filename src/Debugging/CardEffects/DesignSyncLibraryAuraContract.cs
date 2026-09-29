#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Relics;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Cards;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncLibraryAuraContract
{
    internal static async Task Run(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        ctx.AssertEqual("library skill", CardType.Skill, card.Type, effect: false);
        ctx.AssertEqual("library ancient", CardRarity.Ancient, card.Rarity, effect: false);
        ctx.AssertEqual("library 2/1 cost", upgraded ? 1 : 2, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        var outside = ctx.Player.RunState.CreateCard<InsatiableGreed>(ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        foreach (var (instance, pile) in new[] { (outside, PileType.Deck), (card, PileType.Hand) })
        {
            ctx.AssertEqual("library aura full rendered text " + pile,
                "虚无。\n这张牌左侧相邻的卡牌获得消耗，右侧相邻的卡牌获得重放：1。",
                DesignSyncNeutralTextContract.Normalize(instance.GetDescriptionForPile(pile)), effect: false);
            ctx.AssertEqual("library aura title", upgraded ? "娅露丝的书库+" : "娅露丝的书库", instance.Title, effect: false);
        }
        // Inspect the native tome's eligible pool without invoking third-party
        // SetupForPlayer prefixes installed by unrelated mods in this run.
        CardModel[] tomePool = ctx.Player.Character.CardPool
            .GetUnlockedCards(ctx.Player.UnlockState, ctx.Player.RunState.CardMultiplayerConstraint)
            .Where(candidate => candidate.Rarity == CardRarity.Ancient
                && !ArchaicTooth.TranscendenceCards.Contains(candidate))
            .ToArray();
        ctx.AssertTrue("dusty tome pool contains new library skill",
            tomePool.Any(candidate => candidate is InsatiableGreed && candidate.Type == CardType.Skill));

        await ctx.Reset();
        var left = await ctx.Add<DefendIronclad>(PileType.Hand);
        var book = await ctx.Add<InsatiableGreed>(PileType.Hand, upgraded);
        var right = await ctx.Add<DefendIronclad>(PileType.Hand);
        ctx.AssertTrue("left gains exhaust without playing library", left.Keywords.Contains(CardKeyword.Exhaust));
        ctx.AssertEqual("right gains replay without playing library", 1, right.GetEnchantedReplayCount());
        ctx.AssertEqual("library has no wraparound neighbour effect", LibraryAuraEffect.None, LibraryHandAura.InHand(book));
        ctx.AssertTrue("left local keywords untouched", !left.GetKeywordsWithSources(KeywordSources.Local).Contains(CardKeyword.Exhaust));
        ctx.AssertTrue("explicit global query includes temporary exhaust", left.GetKeywordsWithSources(KeywordSources.Global).Contains(CardKeyword.Exhaust));
        var clone = ctx.Combat.CloneCard(left);
        ctx.AssertTrue("clone does not persist aura", !clone.Keywords.Contains(CardKeyword.Exhaust));
        var restored = CardModel.FromSerializable(left.ToSerializable());
        ctx.AssertTrue("save does not persist aura", !restored.GetKeywordsWithSources(KeywordSources.Local).Contains(CardKeyword.Exhaust));
        await CardPileCmd.Add(book, PileType.Discard, skipVisuals: true);
        ctx.AssertTrue("book leaves: left restores", !left.Keywords.Contains(CardKeyword.Exhaust));
        ctx.AssertEqual("book leaves: right restores", 0, right.GetEnchantedReplayCount());
        await CardPileCmd.Add(book, PileType.Hand, skipVisuals: true);
        ctx.AssertTrue("reordered book affects new left neighbour", right.Keywords.Contains(CardKeyword.Exhaust));
        ctx.AssertTrue("old left remains unaffected", !left.Keywords.Contains(CardKeyword.Exhaust));
        ctx.AssertEqual("book at right boundary has no wrap", 0, left.GetEnchantedReplayCount());
        await ctx.Play(book);
        ctx.AssertEqual("library ordinary play goes to discard", PileType.Discard, book.Pile!.Type);
        ctx.AssertTrue("playing library removes aura", !right.Keywords.Contains(CardKeyword.Exhaust));
        ctx.AssertTrue("new library does not install old persistent power", !ctx.Self.HasPower<MaidenSuccubus.Powers.YarusLibraryPower>());

        await ctx.Reset();
        var first = await ctx.Add<InsatiableGreed>(PileType.Hand);
        var middle = await ctx.Add<DefendIronclad>(PileType.Hand);
        var last = await ctx.Add<InsatiableGreed>(PileType.Hand);
        ctx.AssertEqual("two libraries combine both effects", LibraryAuraEffect.Exhaust | LibraryAuraEffect.Replay, LibraryHandAura.InHand(middle));
        CardCmd.Enchant<Glam>(middle, 1);
        ctx.AssertEqual("native enchantment replay is additive", 2, middle.GetEnchantedReplayCount());
        ctx.AssertEqual("both contributions appear in actual text", "获得5点格挡。\n重放2。\n消耗。",
            DesignSyncNeutralTextContract.Normalize(middle.GetDescriptionForPile(PileType.Hand)), effect: false);
        await ctx.Play(middle);
        ctx.AssertEqual("actual replay survives hand-to-play transition", 15m, ctx.Self.Block);
        ctx.AssertEqual("actual exhaust survives hand-to-play transition", PileType.Exhaust, middle.Pile!.Type);
        ctx.AssertEqual("after play native Glam is spent and aura gone", 0, middle.GetEnchantedReplayCount());
        ctx.AssertTrue("after play temporary exhaust absent", !middle.Keywords.Contains(CardKeyword.Exhaust));
        ctx.AssertTrue("native enchantment is preserved", middle.Enchantment is Glam);

        await ctx.Reset();
        var innate = await ctx.Add<DefendIronclad>(PileType.Hand);
        innate.AddKeyword(CardKeyword.Exhaust);
        book = await ctx.Add<InsatiableGreed>(PileType.Hand);
        await CardPileCmd.Add(book, PileType.Discard, skipVisuals: true);
        ctx.AssertTrue("native exhaust is not removed", innate.Keywords.Contains(CardKeyword.Exhaust));
        await ctx.Play(innate);
        ctx.AssertEqual("native exhaust still resolves", PileType.Exhaust, innate.Pile!.Type);

        await ctx.Reset();
        first = await ctx.Add<InsatiableGreed>(PileType.Hand);
        middle = await ctx.Add<DefendIronclad>(PileType.Hand);
        foreach (bool cancel in new[] { false, true })
        {
            using var scope = LibraryHandAura.Capture(middle);
            await CardPileCmd.Add(middle, PileType.Play, skipVisuals: true);
            ctx.AssertEqual("captured replay active only in play", 1, middle.GetEnchantedReplayCount());
            using (LibraryHandAura.Capture(middle))
                ctx.AssertEqual("nested non-hand play does not inherit", 0, middle.GetEnchantedReplayCount());
            try
            {
                Task failed = Task.FromException(cancel ? new TaskCanceledException() : new InvalidOperationException("fixture"));
                await LibraryHandAura.Complete(failed, scope);
            }
            catch (Exception ex) when (ex is TaskCanceledException or InvalidOperationException) { }
            ctx.AssertEqual("failure/cancellation clears captured replay", 0, middle.GetEnchantedReplayCount());
            await CardPileCmd.Add(middle, PileType.Hand, skipVisuals: true);
            ctx.AssertEqual("return to hand restores live aura", 1, middle.GetEnchantedReplayCount());
        }
        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), first);
        ctx.AssertEqual("exhausted library removes aura", 0, middle.GetEnchantedReplayCount());
        var foreign = Player.CreateForNewRun<Ironclad>(ctx.Player.UnlockState, ctx.Player.NetId + 1000);
        foreign.RunState = ctx.Player.RunState;
        var other = foreign.RunState.CreateCard<DefendIronclad>(foreign);
        ctx.AssertEqual("other character never acquires aura", LibraryAuraEffect.None, LibraryHandAura.InHand(other));
        ctx.AssertEqual("out of combat library is not an aura", LibraryAuraEffect.None, LibraryHandAura.InHand(outside));
    }
}
#endif
