#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncIgniteContract
{
    internal static async Task Run(CardEffectTestContext ctx, Ignite card, bool upgraded)
    {
        ctx.AssertEqual("ignite cost", upgraded ? 0 : 1,
            card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("ignite rarity", CardRarity.Uncommon, card.Rarity, effect: false);
        var choice = new BlockingPlayerChoiceContext();
        int TotalHp() => ctx.Enemies.Sum(enemy => enemy.CurrentHp);
        IgnitePower[] Pending() => ctx.Self.Powers.OfType<IgnitePower>().ToArray();
        var decoy = await ctx.Add<MaidenStrike>(PileType.Exhaust, upgraded);
        var selected = await ctx.Add<MaidenStrike>(PileType.Hand, upgraded);
        selected.DynamicVars.Damage.BaseValue = 17;
        int beforeCast = TotalHp();
        await ctx.Play(card, selectedCards: [selected]);
        ctx.AssertEqual("scheduled card does not play immediately", beforeCast, TotalHp());
        IgnitePower power = Pending().Single();
        ctx.AssertTrue("power remembers exact selected instance", ReferenceEquals(selected, power.SelectedCard));
        ctx.AssertEqual("selection really exhausted", PileType.Exhaust, selected.Pile?.Type);
        ctx.AssertEqual("independent applications supported", PowerInstanceType.Instanced, power.InstanceType);
        int hp = TotalHp();
        await PlayerCmd.SetEnergy(0, ctx.Player);
        Player foreign = Player.CreateForNewRun<Ironclad>(ctx.Player.UnlockState, ctx.Player.NetId + 1000);
        foreign.RunState = ctx.Player.RunState;
        await power.AfterPlayerTurnStart(choice, foreign);
        ctx.AssertEqual("other player turn does not trigger", hp, TotalHp());
        ctx.AssertTrue("other player turn does not consume target", ReferenceEquals(selected, power.SelectedCard));
        await power.AfterPlayerTurnStart(choice, ctx.Player);
        ctx.AssertEqual("selected seventeen damage not same-id decoy", 17, hp - TotalHp());
        ctx.AssertEqual("auto play does not spend energy", 0, ctx.Player.PlayerCombatState!.Energy);
        ctx.AssertEqual("same-id decoy stays exhausted", PileType.Exhaust, decoy.Pile?.Type);
        ctx.AssertEqual("selected original moves to discard", PileType.Discard, selected.Pile?.Type);
        ctx.AssertTrue("reference released after resolution", power.SelectedCard == null);
        ctx.AssertEqual("pending power removed", 0, Pending().Length);
        hp = TotalHp();
        await power.AfterPlayerTurnStart(choice, ctx.Player);
        ctx.AssertEqual("duplicate callback cannot replay", hp, TotalHp());

        // Two real Ignite plays must retain two different cards, including when
        // they share the same ID and upgrade status.
        var first = await ctx.Add<MaidenStrike>(PileType.Hand);
        var second = await ctx.Add<MaidenStrike>(PileType.Hand);
        first.DynamicVars.Damage.BaseValue = 11;
        second.DynamicVars.Damage.BaseValue = 23;
        await ctx.Play(ctx.Create<Ignite>(upgraded), selectedCards: [first]);
        await ctx.Play(ctx.Create<Ignite>(upgraded), selectedCards: [second]);
        IgnitePower[] powers = Pending();
        ctx.AssertEqual("two casts produce two pending powers", 2, powers.Length);
        ctx.AssertTrue("first target survives second cast", powers.Any(p => ReferenceEquals(p.SelectedCard, first)));
        ctx.AssertTrue("second cast owns second target", powers.Any(p => ReferenceEquals(p.SelectedCard, second)));
        hp = TotalHp();
        await Hook.AfterPlayerTurnStart(ctx.Combat, choice, ctx.Player);
        ctx.AssertEqual("both selected cards autoplay exactly once", 34, hp - TotalHp());
        ctx.AssertEqual("both pending effects expire", 0, Pending().Length);

        foreach (PileType destination in new[] { PileType.Draw, PileType.Hand, PileType.Discard })
        {
            var moving = await ctx.Add<MaidenStrike>(PileType.Hand);
            await ctx.Play(ctx.Create<Ignite>(upgraded), selectedCards: [moving]);
            IgnitePower pending = Pending().Single();
            CardCmd.Upgrade(moving); // Its old ID/upgrade lookup would now fail.
            await CardPileCmd.Add(moving, destination, skipVisuals: true);
            hp = TotalHp();
            await pending.AfterPlayerTurnStart(choice, ctx.Player);
            ctx.AssertEqual("upgraded moved target plays from " + destination, 9, hp - TotalHp());
            ctx.AssertEqual("moved target used not recreated " + destination, PileType.Discard, moving.Pile?.Type);
            ctx.AssertEqual("moved target power expires " + destination, 0, Pending().Length);
        }

        var removed = await ctx.Add<MaidenStrike>(PileType.Hand, upgraded);
        await ctx.Play(ctx.Create<Ignite>(upgraded), selectedCards: [removed]);
        power = Pending().Single();
        await CardPileCmd.RemoveFromCombat(removed, skipVisuals: true);
        hp = TotalHp();
        await power.AfterPlayerTurnStart(choice, ctx.Player);
        ctx.AssertEqual("removed target is not replaced with same-id decoy", hp, TotalHp());
        ctx.AssertEqual("decoy remains after removed target", PileType.Exhaust, decoy.Pile?.Type);
        ctx.AssertEqual("removed target consumes scheduled effect", 0, Pending().Length);

        // Legacy metadata without a live card reference must not guess a target.
        await ctx.ApplyPower<IgnitePower>(ctx.Self, 1);
        power = Pending().Single();
        power.CardId = decoy.Id.Entry;
        power.WasUpgraded = decoy.IsUpgraded;
        hp = TotalHp();
        await power.AfterPlayerTurnStart(choice, ctx.Player);
        ctx.AssertEqual("missing reference does not guess by metadata", hp, TotalHp());
        ctx.AssertEqual("missing reference does not strand power", 0, Pending().Length);

        var status = await ctx.Add<Dazed>(PileType.Hand);
        await ctx.Play(ctx.Create<Ignite>(upgraded), selectedCards: [status]);
        power = Pending().Single();
        await power.AfterPlayerTurnStart(choice, ctx.Player);
        ctx.AssertEqual("native unplayable handling completes", 0, Pending().Length);
        ctx.AssertTrue("native unplayable does not enter endless play", status.Pile?.Type != PileType.Play);

        // The remaining cards are outside hand; an empty selection schedules nothing.
        await CardPileCmd.RemoveFromCombat(PileType.Hand.GetPile(ctx.Player).Cards.ToArray(), skipVisuals: true);
        await ctx.Play(ctx.Create<Ignite>(upgraded));
        ctx.AssertEqual("no selectable card creates no pending power", 0, Pending().Length);
    }
}
#endif
