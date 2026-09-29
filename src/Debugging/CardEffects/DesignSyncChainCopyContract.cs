#if DEBUG
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncChainCopyContract
{
    internal static readonly Type[] Types = [typeof(SummonThunder), typeof(UltimateFlare), typeof(FlashStab)];
    private static string Plain(string text) => Regex.Replace(text, @"\[[^\]]*\]", "");

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        if (!Types.Contains(card.GetType())) return;
        string expected = card switch
        {
            SummonThunder => $"造成{(upgraded ? 9 : 7)}点伤害。\n斩杀时和魔力解放：对生命值最低的敌人造成{(upgraded ? 9 : 7)}点伤害。",
            UltimateFlare => $"对所有敌人造成{(upgraded ? 52 : 40)}点伤害。\n回合结束时，如果这张牌在你的手牌中，本场战斗中耗能降低1。",
            _ => $"造成{(upgraded ? 7 : 5)}点伤害。\n将一张复制加入抽牌堆。"
        };
        ctx.AssertEqual("chain/copy exact cost", card is SummonThunder ? 1 : card is UltimateFlare ? 4 : 0,
            card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("chain/copy exact rarity", card is SummonThunder ? CardRarity.Common : CardRarity.Uncommon, card.Rarity, effect: false);
        ctx.AssertEqual("chain/copy attack type", CardType.Attack, card.Type, effect: false);
        ctx.AssertEqual("chain/copy target", card is UltimateFlare ? TargetType.AllEnemies : TargetType.AnyEnemy, card.TargetType, effect: false);
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        ctx.AssertEqual("chain/copy full permanent preview", expected, Plain(outside.GetDescriptionForPile(PileType.Deck)), effect: false);
        ctx.AssertEqual("chain/copy full combat preview", expected, Plain(card.GetDescriptionForPile(PileType.Hand)), effect: false);
    }

    internal static async Task Thunder(CardEffectTestContext ctx, SummonThunder card, bool upgraded)
    {
        int damage = upgraded ? 9 : 7;
        foreach (string scenario in new[] { "plain", "release", "decline", "chain", "chain+release", "initial-minion", "followup-minion" })
        {
            await ctx.Reset();
            var spawned = new List<Creature>();
            async Task<Creature> Enemy(int hp)
            {
                var enemy = await CreatureCmd.Add<Byrdonis>(ctx.Combat);
                spawned.Add(enemy);
                foreach (var power in enemy.Powers.ToArray()) await PowerCmd.Remove(power);
                await CreatureCmd.SetMaxAndCurrentHp(enemy, hp);
                return enemy;
            }
            try
            {
                bool armor = scenario is "release" or "decline" or "chain+release";
                if (armor) await ctx.SetUpArmour(1);
                var sink = await Enemy(damage * 20);
                var initial = await Enemy(scenario is "plain" or "release" or "decline" ? damage * 30 : damage);
                var victims = new List<Creature>();
                if (scenario is "chain" or "chain+release")
                {
                    victims.Add(await Enemy(damage - 1));
                    victims.Add(await Enemy(damage - 2));
                }
                if (scenario == "initial-minion") await ctx.ApplyPower<MinionPower>(initial, 1);
                if (scenario == "followup-minion")
                {
                    var minion = await Enemy(1);
                    await ctx.ApplyPower<MinionPower>(minion, 1);
                    victims.Add(minion);
                }
                var untouched = ctx.Enemies.Except(spawned).ToDictionary(e => e, e => e.CurrentHp);
                int initialHp = initial.CurrentHp;
                int sinkHp = sink.CurrentHp;
                await ctx.Play(ctx.Create<SummonThunder>(upgraded), initial,
                    selectedIndices: armor ? [scenario == "decline" ? 1 : 0] : null);
                ctx.AssertDamage(scenario + " selected target", initial, initialHp, damage);
                int sinkHits = scenario is "release" or "chain" ? 1 : scenario == "chain+release" ? 2 : 0;
                ctx.AssertDamage(scenario + " lowest-health survivor receives all pending hits", sink, sinkHp, damage * sinkHits);
                foreach (var victim in victims)
                    ctx.AssertEqual(scenario + " follow-up target killed", 0, victim.CurrentHp);
                ctx.AssertTrue(scenario + " unrelated enemies not hit", untouched.All(p => p.Key.CurrentHp == p.Value));
                ctx.AssertPower(scenario + " exactly one accepted armor payment", ctx.Self, "MagicArmorPower", scenario == "decline" ? 1 : 0);
            }
            finally
            {
                foreach (var enemy in spawned.Where(e => !e.IsDead)) await CreatureCmd.Escape(enemy);
            }
        }
    }

    internal static async Task Flare(CardEffectTestContext ctx, UltimateFlare card, bool upgraded)
    {
        var choice = new BlockingPlayerChoiceContext();
        var deck = ctx.Player.RunState.CreateCard<UltimateFlare>(ctx.Player);
        if (upgraded) CardCmd.Upgrade(deck);
        await CardPileCmd.Add(deck, PileType.Deck, skipVisuals: true);
        try
        {
            var combat = ctx.Combat.CloneCard(deck);
            combat.DeckVersion = deck;
            await CardPileCmd.Add(combat, PileType.Hand, skipVisuals: true);
            var idle = await ctx.Add<UltimateFlare>(PileType.Draw, upgraded);
            ctx.AssertTrue("flare opts into native end-in-hand dispatcher", combat.HasTurnEndInHandEffect);
            for (int turn = 1; turn <= 5; turn++)
            {
                // Native CombatManager snapshots eligible hand cards, moves each to Play,
                // calls the wrapper, then discards. This invokes that same card lifecycle,
                // not the entire player/enemy turn state machine.
                await CardPileCmd.Add(combat, PileType.Play, skipVisuals: true);
                await combat.OnTurnEndInHandWrapper(choice);
                ctx.AssertEqual("reduction before end-turn discard " + turn, Math.Max(0, 4 - turn), combat.EnergyCost.GetWithModifiers(CostModifiers.All));
                await CardPileCmd.Add(combat, PileType.Discard, skipVisuals: true);
                combat.EnergyCost.EndOfTurnCleanup();
                ctx.AssertEqual("discount survives turn cleanup " + turn, Math.Max(0, 4 - turn), combat.EnergyCost.GetWithModifiers(CostModifiers.All));
                ctx.AssertEqual("permanent deck is never discounted " + turn, 4, deck.EnergyCost.GetWithModifiers(CostModifiers.All));
                ctx.AssertEqual("draw-pile card has no end-hand callback " + turn, 4, idle.EnergyCost.GetWithModifiers(CostModifiers.All));
                await CardPileCmd.Add(combat, PileType.Hand, skipVisuals: true);
            }
            var hp = ctx.Enemies.ToDictionary(e => e, e => e.CurrentHp);
            await ctx.Play(combat);
            foreach (var pair in hp) ctx.AssertDamage("flare exact area damage", pair.Key, pair.Value, upgraded ? 52 : 40);
            ctx.AssertEqual("playing does not expire combat discount", 0, combat.EnergyCost.GetWithModifiers(CostModifiers.All));
            var freshCombatCopy = ctx.Combat.CloneCard(deck);
            ctx.AssertEqual("fresh permanent-deck combat copy starts at four", 4, freshCombatCopy.EnergyCost.GetWithModifiers(CostModifiers.All));
            ctx.AssertEqual("fresh copy keeps upgrade", upgraded, freshCombatCopy.IsUpgraded);
        }
        finally
        {
            if (deck.Pile?.Type == PileType.Deck) await CardPileCmd.RemoveFromDeck(deck, showPreview: false);
        }
    }

    internal static async Task Flash(CardEffectTestContext ctx, FlashStab card, bool upgraded)
    {
        int deckCount = PileType.Deck.GetPile(ctx.Player).Cards.Count;
        await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
        CombatEnchantmentCmd.ApplyVanilla<Sharp>(card, 2);
        await ctx.ApplyPower<StrengthPower>(ctx.Self, 3);
        card.EnergyCost.AddThisCombat(2);
        card.ExhaustOnNextPlay = true;
        int hp = ctx.PrimaryEnemy.CurrentHp;
        await ctx.Play(card, ctx.PrimaryEnemy);
        ctx.AssertDamage("flash includes strength and enchantment", ctx.PrimaryEnemy, hp, (upgraded ? 7 : 5) + 5);
        ctx.AssertEqual("source exhausts only when flagged", PileType.Exhaust, card.Pile?.Type);
        var copies = PileType.Draw.GetPile(ctx.Player).Cards.OfType<FlashStab>().ToArray();
        ctx.AssertEqual("exactly one shuffled copy", 1, copies.Length);
        var copy = copies.Single();
        ctx.AssertTrue("copy is a separate model with native origin", !ReferenceEquals(copy, card) && ReferenceEquals(copy.CloneOf, card));
        ctx.AssertEqual("copy upgrade preserved", upgraded, copy.IsUpgraded);
        ctx.AssertEqual("copy damage preserved", upgraded ? 7m : 5m, copy.DynamicVars.Damage.BaseValue);
        ctx.AssertEqual("copy temporary cost preserved", 2, copy.EnergyCost.GetWithModifiers(CostModifiers.All));
        ctx.AssertTrue("copy belongs to the same player and combat", copy.Owner == ctx.Player && ReferenceEquals(copy.CombatState, ctx.Combat));
        ctx.AssertTrue("copy enchantment is independent", copy.Enchantment is Sharp && !ReferenceEquals(copy.Enchantment, card.Enchantment));
        ctx.AssertEqual("copy enchantment amount", 2, copy.Enchantment!.Amount);
        ctx.AssertTrue("one-play exhaust flag does not leak to copy", !copy.ExhaustOnNextPlay);
        copy.EnergyCost.AddThisCombat(-1);
        ctx.AssertEqual("copy discount independent", 1, copy.EnergyCost.GetWithModifiers(CostModifiers.All));
        ctx.AssertEqual("source cost unmodified by copy", 2, card.EnergyCost.GetWithModifiers(CostModifiers.All));
        await CardPileCmd.Add(copy, PileType.Hand, skipVisuals: true);
        hp = ctx.PrimaryEnemy.CurrentHp;
        await ctx.Play(copy, ctx.PrimaryEnemy);
        ctx.AssertDamage("copied flash performs full attack", ctx.PrimaryEnemy, hp, (upgraded ? 7 : 5) + 5);
        var descendant = PileType.Draw.GetPile(ctx.Player).Cards.OfType<FlashStab>().Single();
        ctx.AssertTrue("copy can generate another independent copy", !ReferenceEquals(descendant, copy) && ReferenceEquals(descendant.CloneOf, copy));
        ctx.AssertEqual("descendant inherits current cost", 1, descendant.EnergyCost.GetWithModifiers(CostModifiers.All));
        ctx.AssertEqual("combat copying does not add permanent cards", deckCount, PileType.Deck.GetPile(ctx.Player).Cards.Count);
    }
}
#endif
