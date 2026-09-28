#if DEBUG
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Events;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>Real event options and native selectors. Called only by the confirmed disposable-run test.</summary>
internal static class DesignUndeadEventContract
{
    internal static async Task Run(Player player, Action<bool, string> check)
    {
        var run = (RunState)player.RunState;
        string Text(string value) => Regex.Replace(value, @"\[/?(?:gold|purple|blue|sine)\]", "");
        async Task<UndeadGathering> Start()
        {
            var model = (UndeadGathering)ModelDb.Event<UndeadGathering>().ToMutable();
            await model.BeginEvent(player, null, false);
            check(model.Title.GetFormattedText() == "亡灵集会", "exact event title");
            check(model.InitialDescription.GetRawText() == "不知怎的，你误入了亡灵们的集会场所。\n\n半透明的宾客围坐在一张长得看不见尽头的餐桌两旁。它们没有呼吸，房间里却挤满了层层叠叠的交谈声。杯盘早已腐朽，只有桌首的一簇[blue]苍白火焰[/blue]仍在燃烧。\n\n离你最近的亡灵向后挪了挪。一把空椅子从桌下自行滑出。\n\n[sine]“活人，坐。我们正缺一个听众。”[/sine]", "exact initial narrative including colors and paragraph breaks");
            check(model.CurrentOptions.Count == 3, "exactly three initial options");
            check(!model.IsFinished, "new event unfinished");
            return model;
        }
        async Task<CardModel[]> Deck(int count)
        {
            await CardPileCmd.RemoveFromDeck(player.Deck.Cards.ToArray());
            for (int index = 0; index < count; index++)
                await CardPileCmd.Add(run.CreateCard<MaidenStrike>(player), PileType.Deck, skipVisuals: true);
            return player.Deck.Cards.ToArray();
        }
        void Finished(UndeadGathering model, string page)
        {
            check(model.IsFinished, page + " event completed");
            check(model.CurrentOptions.Count == 0, page + " removes initial options");
            string expected = page switch
            {
                "LISTEN" => "你坐进空位。每个亡灵都急着讲完自己生前最后一件小事，声音很快堆成一片无法分辨的低语。离席时，其中有两段故事仍不肯结束，悄悄跟进了你的牌组。",
                "PRAY" => "你在苍白火焰前为这些可怜的逝者闭眼祈祷。满桌低语同时停顿，你能感受到自己正受到注视。\n祈祷结束，亡灵们已经不在此处。你感到自己在寂静中褪去原形，获得了新的面貌。",
                "LEARN" => "你没有坐下，而是研究起桌布下方倒写的悼词与咒语。\n它们介绍了亡灵们的由来，同时揭示了关于生命与死亡的奥秘。\n等最后一个字烙进你的脑海，你获知了死灵的禁忌知识。但这并非没有代价——某种平庸而沉重的东西，也随之占据了空出的寂静。",
                _ => throw new ArgumentOutOfRangeException(nameof(page)),
            };
            check(model.Description!.GetFormattedText() == expected, page + " exact result page");
        }
        string State(UndeadGathering model) => string.Join("|", player.Creature.CurrentHp,
            player.Creature.MaxHp, CorruptionQuery.Get(run),
            string.Join(",", player.Deck.Cards.Select(card => card.Id.Entry + ":" + card.Enchantment?.Amount)),
            model.Rng.ToSerializable().ToString());

        foreach (var (maximum, expected) in new[] { (1, 0), (9, 0), (10, 1), (19, 1), (59, 5), (60, 6), (97, 9), (100, 10), (199, 19) })
        {
            await CreatureCmd.SetMaxAndCurrentHp(player.Creature, maximum);
            var model = await Start();
            check(UndeadGathering.PrayerHpLoss(maximum) == expected, "production hp rounding boundary");
            check(model.CurrentOptions[1].Description.GetFormattedText().StartsWith($"失去{expected}点生命值。"),
                "actual displayed rounding boundary");
        }
        await CreatureCmd.SetMaxAndCurrentHp(player.Creature, 97);
        for (int corruption = -5; corruption <= 5; corruption++)
        {
            await Deck(2);
            CorruptionCmd.Set(run, corruption);
            var model = await Start();
            var option = model.CurrentOptions[2];
            check(option.IsLocked == (corruption < 3), "learn threshold " + corruption);
            check(Text(model.CurrentOptions[1].Description.GetFormattedText()) ==
                "失去9点生命值。选择2张牌，将其变化为随机圣洁牌。失去1点堕落值。", "exact numeric prayer option");
            check(model.CurrentOptions[0].Title.GetFormattedText() == "与灵魂对话"
                && Text(model.CurrentOptions[0].Description.GetFormattedText()) == "将2张灵魂加入你的牌组。",
                "exact listen option");
            if (corruption < 3)
            {
                check(option.Title.GetFormattedText() == "[需要+3或更高堕落值] 窥探死灵秘术", "exact locked title");
                check(option.Description.GetFormattedText() == "亡灵的密语在你听清之前便已散去。", "exact locked hint");
                string before = State(model);
                await option.Chosen();
                check(State(model) == before && !model.IsFinished, "locked choice is no-op");
            }
        }
        foreach (bool empty in new[] { true, false })
        {
            var cards = await Deck(empty ? 0 : 2);
            foreach (var card in cards) CardCmd.Enchant<NecromancyEnchantment>(card, 1);
            CorruptionCmd.Set(run, 3);
            var model = await Start();
            var option = model.CurrentOptions[2];
            check(option.IsLocked && option.Title.GetFormattedText() == "[没有可附魔的牌] 窥探死灵秘术",
                "empty or fully enchanted deck locks learning");
            string before = State(model);
            await option.Chosen();
            check(State(model) == before && !model.IsFinished, "no-candidate choice cannot charge or finish");
        }

        // Native selector automatically takes all eligible cards when fewer than requested.
        foreach (int count in new[] { 0, 1, 2, 4 })
        {
            var originals = await Deck(count);
            await CreatureCmd.SetMaxAndCurrentHp(player.Creature, 97);
            CorruptionCmd.Set(run, -4);
            var model = await Start();
            var prayer = model.CurrentOptions[1];
            CardModel[] selected = count == 4 ? [originals[1], originals[3]] : originals;
            CardModel[] untouched = originals.Except(selected).ToArray();
            var pool = ModelDb.CardPool<MSHolyCardPool>()
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .Where(card => card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare).ToArray();
            check(pool.Length > 0, "holy transformation pool exists");
            var expectedRng = new Rng(model.Rng.ToSerializable());
            var expectedIds = selected.Select(_ => expectedRng.NextItem(pool)!.Id).ToArray();
            string nicheBefore = run.Rng.Niche.ToSerializable().ToString();
            string rewardsBefore = player.PlayerRng.Rewards.ToSerializable().ToString();
            var selector = new TestCardSelector();
            if (count == 4)
            {
                // Keep the real selection suspended, then issue the same option again.
                var pending = selector.SetupForAsyncCardSelection();
                using (CardSelectCmd.UseSelector(selector))
                {
                    Task action = prayer.Chosen();
                    try
                    {
                        check(!action.IsCompleted && !model.IsFinished, "selection waits for response");
                        await prayer.Chosen();
                        check(player.Deck.Cards.SequenceEqual(originals), "duplicate click cannot transform while waiting");
                    }
                    finally
                    {
                        pending.TrySetResult(selected);
                        await action;
                        selector.Cleanup();
                    }
                }
            }
            else await prayer.Chosen(); // Real automatic <=2 fallback, no fake selection.
            CardModel[] added = player.Deck.Cards.Except(originals).ToArray();
            check(player.Creature.CurrentHp == 88 && player.Creature.MaxHp == 97, "one integer current-hp payment");
            check(CorruptionQuery.Get(run) == -5, "prayer corruption floor");
            check(player.Deck.Cards.Count == count && added.Length == Math.Min(2, count), "native short-deck count");
            check(selected.All(card => !player.Deck.Cards.Contains(card))
                && untouched.All(player.Deck.Cards.Contains), "only selected cards replaced");
            check(added.Select(card => card.Id).SequenceEqual(expectedIds), "exact event-rng replacement sequence");
            check(added.All(card => card is MSHolyCard && card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare),
                "only ordinary holy cards, no starter/generated/ancient");
            check(model.Rng.ToSerializable().ToString() == expectedRng.ToSerializable().ToString(), "only required event rolls consumed");
            check(run.Rng.Niche.ToSerializable().ToString() == nicheBefore
                && player.PlayerRng.Rewards.ToSerializable().ToString() == rewardsBefore, "unrelated RNG untouched");
            Finished(model, "PRAY");
            string after = State(model);
            await prayer.Chosen();
            check(State(model) == after, "finished prayer cannot pay or transform again");
        }

        await Deck(2);
        CorruptionCmd.Set(run, 0);
        var listen = await Start();
        var listening = listen.CurrentOptions[0];
        int hpBefore = player.Creature.CurrentHp;
        await listening.Chosen();
        check(player.Deck.Cards.Count == 4 && player.Deck.Cards.OfType<Soul>().Count() == 2,
            "two independent native Soul instances added");
        check(player.Deck.Cards.OfType<Soul>().Distinct(ReferenceEqualityComparer.Instance).Count() == 2,
            "soul rewards have independent identities");
        check(player.Creature.CurrentHp == hpBefore && CorruptionQuery.Get(run) == 0, "listen has no unrelated cost");
        Finished(listen, "LISTEN");
        string listened = State(listen);
        await listening.Chosen();
        check(State(listen) == listened, "listen cannot add duplicate rewards");

        var targets = await Deck(3);
        CardCmd.Enchant<NecromancyEnchantment>(targets[0], 1);
        CorruptionCmd.Set(run, 5);
        var learn = await Start();
        var learning = learn.CurrentOptions[2];
        check(Text(learning.Description.GetFormattedText()) ==
            "选择1张可附魔牌，为其附魔：死灵。将1张凡庸加入你的牌组。获得1点堕落值。", "exact learning option");
        var enchantSelector = new TestCardSelector();
        enchantSelector.PrepareToSelect([targets[2]]);
        using (CardSelectCmd.UseSelector(enchantSelector)) await learning.Chosen();
        check(targets[0].Enchantment is NecromancyEnchantment { Amount: 1 }
            && targets[1].Enchantment == null && targets[2].Enchantment is NecromancyEnchantment { Amount: 1 },
            "selected eligible card enchanted; existing and unselected cards unchanged");
        check(player.Deck.Cards.Count == 4 && player.Deck.Cards.OfType<Normality>().Count() == 1
            && CorruptionQuery.Get(run) == 5, "one Normality and clamped corruption gain");
        Finished(learn, "LEARN");
        string learned = State(learn);
        await learning.Chosen();
        check(State(learn) == learned, "learning cannot duplicate enchantment or curse");
    }
}
#endif
