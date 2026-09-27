#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncLightWingsContract
{
    private static LayeredEnchantment Layers(CardModel card) => (LayeredEnchantment)card.Enchantment!;

    internal static async Task Run(CardEffectTestContext ctx, LightWings card, bool upgraded)
    {
        bool previousTestMode = TestMode.IsOn;
        try
        {
            TestMode.IsOn = true;
            ctx.AssertEqual("light wings rarity", CardRarity.Rare, card.Rarity, effect: false);
            ctx.AssertEqual("light wings pool", typeof(MSHolyCardPool), card.Pool.GetType(), effect: false);
            ctx.AssertEqual("light wings cost", 1, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
            ctx.AssertEqual("light wings damage", upgraded ? 12m : 9m, card.DynamicVars.Damage.BaseValue, effect: false);
            await ctx.Add<StrikeIronclad>(PileType.Draw);
            var enchanted = await ctx.Add<MaidenStrike>(PileType.Draw);
            CombatEnchantmentCmd.ApplyVanilla<Sharp>(enchanted, 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("plain card damage", ctx.PrimaryEnemy, hp, upgraded ? 12 : 9);
            ctx.AssertEqual("draw searches for enchanted card", PileType.Hand, enchanted.Pile?.Type);

            await ctx.Reset();
            var layered = await ctx.Add<LightWings>(PileType.Hand, upgraded);
            var sharp2 = CardCmd.Enchant<Sharp>(layered, 2); // Exercise permanent/native entry, typed result.
            var sharp3 = CombatEnchantmentCmd.ApplyVanilla<Sharp>(layered, 3);
            CombatEnchantmentCmd.ApplyVanilla<Adroit>(layered, 3);
            var charge = CombatEnchantmentCmd.ApplyAudited<ChargeEnchantment>(layered, 2);
            var swift = CombatEnchantmentCmd.ApplyVanilla<Swift>(layered, 1);
            var glam1 = CombatEnchantmentCmd.ApplyVanilla<Glam>(layered, 1);
            var glam2 = CombatEnchantmentCmd.ApplyVanilla<Glam>(layered, 1);
            ctx.AssertTrue("native typed result is child", sharp2 is Sharp && sharp2.Card == layered);
            ctx.AssertTrue("independent same-name nonstackable instances", !ReferenceEquals(sharp2, sharp3) && !ReferenceEquals(glam1, glam2));
            ctx.AssertEqual("all seven native/custom layers retained", 7, Layers(layered).Layers.Count);
            ctx.AssertEqual("two Glam actually add two replays", 2, layered.GetEnchantedReplayCount());
            var runListeners = ctx.Player.RunState.IterateHookListeners(ctx.Combat).ToArray();
            foreach (var child in Layers(layered).Layers)
                ctx.AssertEqual("child hook appears once in run+combat stream", 1, runListeners.Count(model => ReferenceEquals(model, child)));
            ctx.AssertTrue("technical container is not an extra hook listener", !runListeners.Contains(layered.Enchantment));
            ctx.AssertTrue("hover exposes duplicate names", layered.Enchantment!.DynamicDescription.GetFormattedText().Contains("×2"));
            ctx.AssertTrue("each child has a hover", layered.Enchantment.HoverTips.Count() >= 8);
            await ctx.AddFillerCards(PileType.Draw, 10);
            int energy = ctx.Player.PlayerCombatState!.Energy;
            int block = ctx.Self.Block;
            hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(layered, ctx.PrimaryEnemy);
            ctx.AssertDamage("Sharp sums and both Glam execute", ctx.PrimaryEnemy, hp, upgraded ? 51 : 42);
            ctx.AssertBlock("Adroit executes for each replay", block, 9);
            ctx.AssertEqual("Charge once across replay series", 2, ctx.Player.PlayerCombatState.Energy - energy);
            ctx.AssertEqual("Swift once across replay series", 1, ctx.CountCards<StrikeIronclad>(PileType.Hand));
            ctx.AssertTrue("one-shot child states disabled", charge.Status == EnchantmentStatus.Disabled && swift.Status == EnchantmentStatus.Disabled);
            ctx.AssertEqual("both Glam spent through native hooks", 0, layered.GetEnchantedReplayCount());
            hp = ctx.PrimaryEnemy.CurrentHp;
            block = ctx.Self.Block;
            energy = ctx.Player.PlayerCombatState.Energy;
            await ctx.Play(layered, ctx.PrimaryEnemy);
            ctx.AssertDamage("later play only once", ctx.PrimaryEnemy, hp, upgraded ? 17 : 14);
            ctx.AssertBlock("ongoing child effect persists", block, 3);
            ctx.AssertEqual("Charge not refreshed", energy, ctx.Player.PlayerCombatState.Energy);

            var clone = layered.CreateClone();
            ctx.AssertEqual("clone layer count", 7, Layers(clone).Layers.Count);
            ctx.AssertTrue("clone has independent native models", Layers(clone).Layers.Zip(Layers(layered).Layers)
                .All(pair => !ReferenceEquals(pair.First, pair.Second) && pair.First.Card == clone));
            ctx.AssertEqual("clone preserves spent replays", 0, clone.GetEnchantedReplayCount());
            Layers(clone).Layers.OfType<Sharp>().First().Amount = 99;
            ctx.AssertEqual("clone mutation isolated", 2, sharp2!.Amount);
            var saved = layered.ToSerializable();
            var loaded = ctx.Player.RunState.LoadCard(saved, ctx.Player);
            ctx.AssertEqual("native save-load layer count", 7, Layers(loaded).Layers.Count);
            ctx.AssertEqual("native save-load upgrade", upgraded, loaded.IsUpgraded);
            ctx.AssertEqual("native save-load amount", 5, Layers(loaded).Layers.OfType<Sharp>().Sum(sharp => sharp.Amount));
            ctx.AssertTrue("loaded children bind to loaded card", Layers(loaded).Layers.All(child => child.Card == loaded));
            var savedStateCard = await ctx.Add<LightWings>(PileType.Hand);
            var wrath = CardCmd.Enchant<WrathEnchantment>(savedStateCard, 1)
                ?? throw new InvalidOperationException("Wrath layer failed to apply.");
            wrath.UsedThisCombat = true;
            CardCmd.Enchant<Sharp>(savedStateCard, 2);
            var savedStateLoaded = CardModel.FromSerializable(savedStateCard.ToSerializable());
            ctx.AssertTrue("child SavedProperty survives ownerless load", Layers(savedStateLoaded).Layers
                .OfType<WrathEnchantment>().Single().UsedThisCombat);
            var childrenBeforeClear = Layers(clone).Layers.ToArray();
            CardCmd.ClearEnchantment(clone);
            ctx.AssertTrue("clear detaches container and children", clone.Enchantment == null && childrenBeforeClear.All(child => !child.HasCard));

            await ctx.Reset();
            var costCard = await ctx.Add<LightWings>(PileType.Hand, upgraded);
            CombatEnchantmentCmd.ApplyVanilla<Instinct>(costCard, 1);
            CombatEnchantmentCmd.ApplyVanilla<Adroit>(costCard, 3);
            costCard.EnergyCost.UpgradeBy(1);
            ctx.AssertEqual("adding a new layer does not reapply Instinct", 1, costCard.EnergyCost.GetWithModifiers(CostModifiers.None));
            ctx.AssertTrue("skill-only enchantment still illegal", !ModelDb.Enchantment<Imbued>().CanEnchant(costCard));
            ctx.AssertTrue("defend-only enchantment still illegal", !ModelDb.Enchantment<Goopy>().CanEnchant(costCard));
            CombatEnchantmentCmd.ApplyVanilla<TezcatarasEmber>(costCard, 1);
            ctx.AssertTrue("zero-cost Instinct still illegal", !ModelDb.Enchantment<Instinct>().CanEnchant(costCard));
            var ordinary = await ctx.Add<MaidenStrike>(PileType.Hand);
            CombatEnchantmentCmd.ApplyVanilla<Sharp>(ordinary, 1);
            ctx.AssertTrue("ordinary card still has one slot", !ModelDb.Enchantment<Glam>().CanEnchant(ordinary) && ordinary.Enchantment is Sharp);
            var legacy = await ctx.Add<LightWings>(PileType.Hand);
            var legacyInstinct = (Instinct)ModelDb.Enchantment<Instinct>().ToMutable();
            legacy.EnchantInternal(legacyInstinct, 1); // Pre-container single-slot save layout.
            legacyInstinct.ModifyCard();
            CardCmd.Enchant<Sharp>(legacy, 2);
            legacy.EnergyCost.UpgradeBy(1);
            ctx.AssertTrue("legacy instance migrates rather than being replaced", Layers(legacy).Layers.Contains(legacyInstinct));
            ctx.AssertEqual("legacy cost mutation not applied twice", 1, legacy.EnergyCost.GetWithModifiers(CostModifiers.None));

            await ctx.Reset();
            var slumber = await ctx.Add<LightWings>(PileType.Hand, upgraded);
            slumber.EnergyCost.SetThisCombat(5);
            CombatEnchantmentCmd.ApplyVanilla<SlumberingEssence>(slumber, 1);
            CombatEnchantmentCmd.ApplyVanilla<SlumberingEssence>(slumber, 1);
            await Hook.BeforeFlush(ctx.Combat, ctx.Player);
            ctx.AssertEqual("each end-phase child once", 3, slumber.EnergyCost.GetWithModifiers(CostModifiers.All));
            await Hook.BeforeFlush(ctx.Combat, ctx.Player);
            ctx.AssertEqual("next phase not doubled by two listener streams", 1, slumber.EnergyCost.GetWithModifiers(CostModifiers.All));
            await ctx.Play(slumber, ctx.PrimaryEnemy);
            ctx.AssertEqual("until-played costs reset", 5, slumber.EnergyCost.GetWithModifiers(CostModifiers.All));

            await ctx.Reset();
            var multiplier = await ctx.Add<LightWings>(PileType.Hand, upgraded);
            CardCmd.Enchant<Corrupted>(multiplier, 1);
            CardCmd.Enchant<Corrupted>(multiplier, 1);
            hp = ctx.PrimaryEnemy.CurrentHp;
            int selfHp = ctx.Self.CurrentHp;
            await ctx.Play(multiplier, ctx.PrimaryEnemy);
            ctx.AssertDamage("multiplicative layers both execute", ctx.PrimaryEnemy, hp, upgraded ? 27 : 20);
            ctx.AssertEqual("both on-play costs execute", 4, selfHp - ctx.Self.CurrentHp);

            await ctx.Reset();
            var linked = await ctx.Add<LightWings>(PileType.Draw);
            CombatEnchantmentCmd.ApplyVanilla<Sharp>(linked, 1);
            CombatEnchantmentCmd.ApplyAudited<SoulLinkEnchantment>(linked, 1);
            var linkSource = await ctx.Add<MaidenStrike>(PileType.Hand);
            CombatEnchantmentCmd.ApplyAudited<SoulLinkEnchantment>(linkSource, 1);
            await ctx.Play(linkSource, ctx.PrimaryEnemy);
            ctx.AssertEqual("SoulLink finds an internal layer", PileType.Hand, linked.Pile?.Type);
            var forge = await ctx.Add<ForgeNimble>(PileType.Hand);
            await ctx.Play(forge, selectedCards: [linked]);
            ctx.AssertTrue("ForgeNimble selector accepts already enchanted LightWings", LayeredEnchantments.Has<Adroit>(linked));

            await ctx.Reset();
            var deck = ctx.Player.RunState.CreateCard<LightWings>(ctx.Player);
            await CardPileCmd.Add(deck, PileType.Deck, skipVisuals: true);
            CardCmd.Enchant<Sharp>(deck, 2);
            var combatCopy = await ctx.Add<LightWings>(PileType.Hand);
            combatCopy.DeckVersion = deck;
            CombatEnchantmentCmd.ApplyVanilla<Sharp>(combatCopy, 7);
            CombatEnchantmentCmd.ApplyVanilla<Glam>(combatCopy, 1);
            ctx.AssertEqual("temporary additions do not enter permanent deck", 1, Layers(deck).Layers.Count);
            ctx.AssertEqual("permanent original amount unchanged", 2, Layers(deck).Layers.Single().Amount);
            CardCmd.Enchant<Clone>(deck, 1);
            CardCmd.Enchant<Clone>(deck, 1);
            int expectedCopies = ctx.Player.Deck.Cards.Sum(candidate => candidate.Enchantment is Clone ? 1
                : candidate.Enchantment is LayeredEnchantment all ? all.Layers.OfType<Clone>().Count() : 0);
            int beforeDeck = ctx.Player.Deck.Cards.Count;
            ctx.AssertTrue("native clone rest action completes", await new CloneRestSiteOption(ctx.Player).OnSelect());
            ctx.AssertEqual("clone rest includes both internal Clone layers", expectedCopies, ctx.Player.Deck.Cards.Count - beforeDeck);
        }
        finally { TestMode.IsOn = previousTestMode; }
    }
}
#endif
