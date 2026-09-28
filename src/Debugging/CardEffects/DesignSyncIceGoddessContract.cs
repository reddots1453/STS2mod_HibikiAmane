#if DEBUG
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncIceGoddessContract
{
    private static string Plain(string text) => Regex.Replace(text, @"\[[^\]]*\]", "");

    internal static async Task Run(CardEffectTestContext ctx, GoddessOfIce card, bool upgraded)
    {
        var choice = new BlockingPlayerChoiceContext();
        string expected = "每当你打出附魔牌时，将1张冰晶碎片" + (upgraded ? "+" : "") + "加入手牌。";
        ctx.AssertEqual("goddess rare", CardRarity.Rare, card.Rarity, effect: false);
        ctx.AssertEqual("goddess power", CardType.Power, card.Type, effect: false);
        ctx.AssertEqual("goddess one energy", 1, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        var outside = ctx.Player.RunState.CreateCard<GoddessOfIce>(ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        ctx.AssertEqual("goddess exact outside description", expected, Plain(outside.GetDescriptionForPile(PileType.Deck)), effect: false);
        ctx.AssertEqual("goddess exact combat description", expected, Plain(card.GetDescriptionForPile(PileType.Hand)), effect: false);
        var expectedHover = HoverTipFactory.FromCard<IceShard>(upgraded);
        ctx.AssertTrue("card has correct shard preview", card.HoverTips.Any(tip => tip.Id == expectedHover.Id), effect: false);
        await ctx.Play(card);
        GoddessOfIcePower power = ctx.Self.GetPower<GoddessOfIcePower>()!;
        ctx.AssertPower("goddess amount", ctx.Self, "GoddessOfIcePower", upgraded ? 2 : 1);
        ctx.AssertEqual("exact live power description", expected, Plain(power.Description.GetFormattedText()), effect: false);
        ctx.AssertEqual("exact live smart description", expected, Plain(power.SmartDescription.GetFormattedText()), effect: false);
        ctx.AssertTrue("power has correct shard preview", power.HoverTips.Any(tip => tip.Id == expectedHover.Id), effect: false);

        int Shards(PileType pile = PileType.Hand) => ctx.CountCards<IceShard>(pile);
        CardPlay Receipt(CardModel played, Player? actualPlayer = null) => new()
        {
            Card = played, Player = actualPlayer ?? ctx.Player, Target = null, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 },
            IsAutoPlay = true, PlayIndex = 0, PlayCount = 1
        };
        async Task Activate()
        {
            await ctx.Reset();
            await ctx.Play(ctx.Create<GoddessOfIce>(upgraded));
            power = ctx.Self.GetPower<GoddessOfIcePower>()!;
        }

        var plain = await ctx.Add<MaidenDefend>(PileType.Hand);
        await ctx.Play(plain);
        ctx.AssertEqual("unenchanted play generates no shard", 0, Shards());
        var sharp = await ctx.Add<MaidenStrike>(PileType.Hand);
        CombatEnchantmentCmd.ApplyVanilla<Sharp>(sharp, 1);
        await ctx.Play(sharp, ctx.PrimaryEnemy);
        ctx.AssertEqual("one enchanted card generates one shard", 1, Shards());
        ctx.AssertTrue("generated shard upgrade state", PileType.Hand.GetPile(ctx.Player).Cards.OfType<IceShard>().All(s => s.IsUpgraded == upgraded));

        await Activate();
        var replay = await ctx.Add<MaidenDefend>(PileType.Hand);
        CombatEnchantmentCmd.ApplyVanilla<Glam>(replay, 1);
        await ctx.Play(replay);
        ctx.AssertEqual("Glam gives two actual plays and two shards", 2, Shards());
        ctx.AssertTrue("both replay shards have requested upgrade", PileType.Hand.GetPile(ctx.Player).Cards.OfType<IceShard>().All(s => s.IsUpgraded == upgraded));
        await ctx.Play(replay);
        ctx.AssertEqual("later play of same instance still generates once", 3, Shards());

        await Activate();
        var wings = await ctx.Add<LightWings>(PileType.Hand);
        CombatEnchantmentCmd.ApplyVanilla<Sharp>(wings, 1);
        CombatEnchantmentCmd.ApplyVanilla<Glam>(wings, 1);
        await ctx.Play(wings, ctx.PrimaryEnemy);
        ctx.AssertEqual("layered replay counts two plays not four layers", 2, Shards());

        await Activate();
        var expiring = ctx.Create<MaidenDefend>();
        CombatEnchantmentCmd.ApplyVanilla<Swift>(expiring, 1);
        CardPlay receipt = Receipt(expiring);
        await power.BeforeCardPlayed(receipt);
        ctx.AssertEqual("no generation before completion", 0, Shards());
        expiring.ClearEnchantmentInternal();
        await power.AfterCardPlayed(choice, receipt);
        await power.AfterCardPlayed(choice, receipt);
        ctx.AssertEqual("expired enchantment counts once despite repeated callback", 1, Shards());
        CardPlay later = Receipt(expiring);
        await power.BeforeCardPlayed(later);
        await power.AfterCardPlayed(choice, later);
        ctx.AssertEqual("later unenchanted play cannot reuse old eligibility", 1, Shards());

        var enchanted = ctx.Create<MaidenDefend>();
        CombatEnchantmentCmd.ApplyVanilla<Swift>(enchanted, 1);
        var foreign = Player.CreateForNewRun<Ironclad>(ctx.Player.UnlockState, ctx.Player.NetId + 1000);
        foreign.RunState = ctx.Player.RunState;
        var foreignPlay = Receipt(enchanted, foreign);
        await power.BeforeCardPlayed(foreignPlay);
        await power.AfterCardPlayed(choice, foreignPlay);
        ctx.AssertEqual("actual foreign player excluded despite own card", 1, Shards());
        var noStart = Receipt(enchanted);
        await power.AfterCardPlayed(choice, noStart);
        ctx.AssertEqual("unobserved completion cannot manufacture eligibility", 1, Shards());

        CardPlay pending = Receipt(enchanted);
        await power.BeforeCardPlayed(pending);
        var clone = (GoddessOfIcePower)power.ClonePreservingMutability();
        await PowerCmd.Remove(power);
        await power.AfterCardPlayed(choice, pending);
        ctx.AssertEqual("removed power cannot finish pending reaction", 1, Shards());
        await PowerCmd.Apply(choice, clone, ctx.Self, upgraded ? 2 : 1, ctx.Self, null);
        await clone.AfterCardPlayed(choice, pending);
        ctx.AssertEqual("power clone does not inherit old play receipts", 1, Shards());
        await clone.BeforeCardPlayed(pending);
        await clone.AfterCardPlayed(choice, pending);
        ctx.AssertEqual("clone can observe its own new play", 2, Shards());

        await ctx.Reset();
        var selfEnchanted = ctx.Create<GoddessOfIce>(upgraded);
        CombatEnchantmentCmd.ApplyVanilla<Swift>(selfEnchanted, 1);
        await ctx.Play(selfEnchanted);
        ctx.AssertEqual("newly applied power observes own enchanted card once", 1, Shards());

        await Activate();
        await ctx.AddFillerCards(PileType.Hand, 10);
        var overflow = ctx.Create<MaidenDefend>();
        CombatEnchantmentCmd.ApplyVanilla<Swift>(overflow, 1);
        CardPlay fullHandPlay = Receipt(overflow);
        await power.BeforeCardPlayed(fullHandPlay);
        await power.AfterCardPlayed(choice, fullHandPlay);
        ctx.AssertEqual("full hand stays at native limit", 10, PileType.Hand.GetPile(ctx.Player).Cards.Count);
        ctx.AssertEqual("overflow uses native discard destination", 1, Shards(PileType.Discard));
        ctx.AssertTrue("overflow shard upgrade state", PileType.Discard.GetPile(ctx.Player).Cards.OfType<IceShard>().Single().IsUpgraded == upgraded);
    }
}
#endif
