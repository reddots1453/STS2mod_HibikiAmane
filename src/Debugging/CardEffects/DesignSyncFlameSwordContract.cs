#if DEBUG
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Rooms;
using MaidenSuccubus.Cards;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncFlameSwordContract
{
    private static string Text(CardModel card, PileType pile = PileType.None) =>
        Regex.Replace(card.GetDescriptionForPile(pile), @"\[[^\]]*\]", "");

    internal static async Task Run(CardEffectTestContext ctx, FlameSword card, bool upgraded)
    {
        ctx.AssertEqual("FlameSword cost", 1, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("FlameSword rarity", CardRarity.Uncommon, card.Rarity, effect: false);
        ctx.AssertEqual("FlameSword damage", upgraded ? 12m : 9m, card.DynamicVars.Damage.BaseValue, effect: false);
        string expected = $"造成{(upgraded ? 12 : 9)}点伤害。\n完成5场战斗后，为这张牌附魔：特兹卡塔拉的余烬。\n（还剩5场战斗）";
        ctx.AssertEqual("exact runtime description outside combat", expected, Text(card), effect: false);
        ctx.AssertEqual("exact runtime description in hand", expected, Text(card, PileType.Hand), effect: false);

        var deck = ctx.Player.RunState.CreateCard<FlameSword>(ctx.Player);
        if (upgraded) { deck.UpgradeInternal(); deck.FinalizeUpgradeInternal(); }
        await CardPileCmd.Add(deck, PileType.Deck, skipVisuals: true);
        card.DeckVersion = deck;
        try
        {
            int hp = ctx.PrimaryEnemy.CurrentHp;
            for (int i = 0; i < 6; i++) await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("six actual plays only deal damage", ctx.PrimaryEnemy, hp, (upgraded ? 12 : 9) * 6);
            ctx.AssertEqual("plays do not advance deck battles", 0, deck.CompletedCombats);
            ctx.AssertTrue("plays never apply old sixth-play enchantment", deck.Enchantment == null && card.Enchantment == null);
            ctx.AssertTrue("native run dispatcher includes permanent card",
                ctx.Player.RunState.IterateHookListeners(ctx.Combat).Contains(deck));

            for (int completed = 1; completed <= 5; completed++)
            {
                // Invoke the real model lifecycle with distinct room tokens.
                // This is not a substitute for five natural combat victories.
                var room = new CombatRoom(ctx.Combat);
                await card.AfterCombatVictory(room);
                ctx.AssertEqual("combat clone cannot double-credit deck", completed - 1, deck.CompletedCombats);
                await deck.AfterCombatVictory(room);
                ctx.AssertEqual("one victory advances one battle", completed, deck.CompletedCombats);
                await deck.AfterCombatVictory(room);
                ctx.AssertEqual("duplicate room callback is idempotent", completed, deck.CompletedCombats);
                ctx.AssertEqual("remaining actual battles", 5 - completed, deck.DynamicVars["Remaining"].IntValue);
                ctx.AssertTrue("only fifth victory enchants permanent card", (deck.Enchantment is TezcatarasEmber) == (completed == 5));
                DesignSyncRemainingTextContract.FlameProgress(ctx, deck, PileType.Deck, upgraded, completed);
                var loaded = (FlameSword)CardModel.FromSerializable(deck.ToSerializable());
                ctx.AssertEqual("completed battle count survives save/load", completed, loaded.CompletedCombats);
                ctx.AssertEqual("upgrade survives save/load", upgraded, loaded.IsUpgraded);
                ctx.AssertEqual("loaded remaining is not reset", 5 - completed, loaded.DynamicVars["Remaining"].IntValue);
                ctx.AssertTrue("remaining suffix hidden only after enchantment",
                    Text(loaded).Contains($"（还剩{5 - completed}场战斗）") == (completed < 5));
                if (completed == 3)
                {
                    var resumed = (FlameSword)ctx.Player.RunState.LoadCard(deck.ToSerializable(), ctx.Player);
                    await CardPileCmd.Add(resumed, PileType.Deck, skipVisuals: true);
                    try
                    {
                        await resumed.AfterCombatVictory(new CombatRoom(ctx.Combat));
                        ctx.AssertEqual("resumed mid-growth save continues to four", 4, resumed.CompletedCombats);
                        ctx.AssertTrue("resumed fourth battle does not enchant early", resumed.Enchantment == null);
                        ctx.AssertEqual("loaded instance progress is independent", 3, deck.CompletedCombats);
                    }
                    finally { await CardPileCmd.RemoveFromDeck([resumed], showPreview: false); }
                }
            }
            ctx.AssertEqual("permanent Ember cost zero", 0, deck.EnergyCost.GetWithModifiers(CostModifiers.All));
            ctx.AssertTrue("permanent Ember eternal", deck.Keywords.Contains(CardKeyword.Eternal));
            var finished = deck.Enchantment;
            await deck.AfterCombatVictory(new CombatRoom(ctx.Combat));
            ctx.AssertTrue("later victory never stacks Ember", ReferenceEquals(finished, deck.Enchantment) && finished!.Amount == 1);
            ctx.AssertEqual("count remains capped", 5, deck.CompletedCombats);
            var nextCombat = (FlameSword)ctx.Combat.CloneCard(deck);
            await CardPileCmd.Add(nextCombat, PileType.Hand, skipVisuals: true);
            ctx.AssertTrue("next combat inherits Ember and completed progress",
                nextCombat.Enchantment is TezcatarasEmber && nextCombat.CompletedCombats == 5);
            ctx.AssertTrue("clone enchantment belongs to clone and does not alias deck",
                !ReferenceEquals(nextCombat.Enchantment, deck.Enchantment)
                && ReferenceEquals(nextCombat.Enchantment!.Card, nextCombat));
            ctx.AssertTrue("next combat suffix remains hidden", !Text(nextCombat).Contains("还剩"));
            DesignSyncRemainingTextContract.FlameProgress(ctx, nextCombat, PileType.Hand, upgraded, 5);
        }
        finally
        {
            if (deck.Pile?.Type == PileType.Deck)
                await CardPileCmd.RemoveFromDeck([deck], showPreview: false);
        }
        var removed = ctx.Player.RunState.CreateCard<FlameSword>(ctx.Player);
        removed.CompletedCombats = 2;
        await CardPileCmd.Add(removed, PileType.Deck, skipVisuals: true);
        await CardPileCmd.RemoveFromDeck([removed], showPreview: false);
        await removed.AfterCombatVictory(new CombatRoom(ctx.Combat));
        ctx.AssertEqual("removed incomplete card never receives growth", 2, removed.CompletedCombats);

        var legacy = (FlameSword)ModelDb.Card<FlameSword>().ToMutable();
        legacy.TimesPlayed = 6;
        var legacyLoaded = (FlameSword)CardModel.FromSerializable(legacy.ToSerializable());
        ctx.AssertEqual("old play count retained for compatibility", 6, legacyLoaded.TimesPlayed);
        ctx.AssertEqual("old plays never converted into battle count", 0, legacyLoaded.CompletedCombats);
        ctx.AssertTrue("legacy unenchanted still needs five battles", Text(legacyLoaded).Contains("还剩5场战斗"));
        CardCmd.Enchant<TezcatarasEmber>(legacy, 1);
        legacyLoaded = (FlameSword)CardModel.FromSerializable(legacy.ToSerializable());
        ctx.AssertTrue("legacy earned Ember preserved", legacyLoaded.Enchantment is TezcatarasEmber);
        ctx.AssertTrue("legacy earned Ember hides obsolete counter", !Text(legacyLoaded).Contains("还剩"));

        var occupied = ctx.Player.RunState.CreateCard<FlameSword>(ctx.Player);
        occupied.CompletedCombats = 4;
        await CardPileCmd.Add(occupied, PileType.Deck, skipVisuals: true);
        try
        {
            var sharp = CardCmd.Enchant<Sharp>(occupied, 3);
            await occupied.AfterCombatVictory(new CombatRoom(ctx.Combat));
            ctx.AssertEqual("occupied slot still completes battle progress", 5, occupied.CompletedCombats);
            ctx.AssertTrue("existing permanent enchantment not overwritten", ReferenceEquals(sharp, occupied.Enchantment));
            ctx.AssertTrue("unfulfilled Ember does not falsely hide counter", Text(occupied).Contains("还剩0场战斗"));
            CardCmd.ClearEnchantment(occupied);
            await occupied.AfterCombatVictory(new CombatRoom(ctx.Combat));
            ctx.AssertTrue("released slot receives earned Ember at next victory", occupied.Enchantment is TezcatarasEmber);
        }
        finally
        {
            await CardPileCmd.RemoveFromDeck([occupied], showPreview: false);
        }
        var clamp = (FlameSword)ModelDb.Card<FlameSword>().ToMutable();
        clamp.CompletedCombats = -8;
        ctx.AssertEqual("invalid negative save clamped", 0, clamp.CompletedCombats);
        clamp.CompletedCombats = int.MaxValue;
        ctx.AssertEqual("invalid excessive save clamped", 5, clamp.CompletedCombats);
        var generated = await ctx.Add<FlameSword>(PileType.Hand);
        await generated.AfterCombatVictory(new CombatRoom(ctx.Combat));
        ctx.AssertEqual("generated card does not grant deck progress", 0, generated.CompletedCombats);
    }
}
#endif
