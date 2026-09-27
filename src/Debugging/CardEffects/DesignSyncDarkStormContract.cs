#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Debugging.CardEffects;

/// <summary>DS27 literals plus real play, upgrade, clone and serialization paths.</summary>
internal static class DesignSyncDarkStormContract
{
    internal static async Task Run(CardEffectTestContext ctx, DarkStorm card, bool upgraded)
    {
        ctx.AssertEqual("DS27 storm name", "黑暗风暴", card.TitleLocString.GetFormattedText(), effect: false);
        ctx.AssertEqual("DS27 storm type", CardType.Attack, card.Type, effect: false);
        ctx.AssertEqual("DS27 storm rarity", CardRarity.Uncommon, card.Rarity, effect: false);
        ctx.AssertEqual("DS27 storm target", TargetType.AllEnemies, card.TargetType, effect: false);
        ctx.AssertEqual("DS27 storm pool", typeof(MSCorruptCardPool), card.Pool.GetType(), effect: false);
        ctx.AssertEqual("DS27 storm cost", 2, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("DS27 fixed damage", 8m, card.DynamicVars.Damage.BaseValue, effect: false);
        ctx.AssertEqual("DS27 fixed vulnerable", 2m, card.DynamicVars["VulnerablePower"].BaseValue, effect: false);
        string description = System.Text.RegularExpressions.Regex.Replace(
            card.GetDescriptionForPile(PileType.None), @"\[[^\]]*\]", "");
        ctx.AssertTrue("runtime formatted upgrade enchantment text",
            description.Contains("升级时，为这张牌附魔：华彩。") && !description.Contains("拾起时"), effect: false);
        ctx.AssertTrue("upgrade alone attaches real Glam", (card.Enchantment is Glam) == upgraded);
        ctx.AssertEqual("first-play additional count", upgraded ? 1 : 0, card.GetEnchantedReplayCount());

        var enemies = ctx.Enemies;
        var hp = enemies.Select(enemy => enemy.CurrentHp).ToArray();
        await ctx.Play(card);
        for (int i = 0; i < enemies.Count; i++)
        {
            // The first 8 damage applies Vulnerable before Glam's second hit:
            // 8 + 12, not 8 * 2. Each execution applies two Vulnerable.
            ctx.AssertDamage("first play actual AoE and replay", enemies[i], hp[i], upgraded ? 20 : 8);
            ctx.AssertPower("first play vulnerable stacks", enemies[i], "VulnerablePower", upgraded ? 4 : 2);
        }
        ctx.AssertEqual("Glam only first play of combat", 0, card.GetEnchantedReplayCount());
        hp = enemies.Select(enemy => enemy.CurrentHp).ToArray();
        await ctx.Play(card);
        for (int i = 0; i < enemies.Count; i++)
        {
            ctx.AssertDamage("second play does not replay", enemies[i], hp[i], 12);
            ctx.AssertPower("second play adds two vulnerable", enemies[i], "VulnerablePower", upgraded ? 6 : 4);
        }

        CardModel clone = card.CreateClone();
        ctx.AssertEqual("combat clone retains upgrade", upgraded, clone.IsUpgraded);
        ctx.AssertTrue("combat clone enchantment identity isolated",
            !upgraded || !ReferenceEquals(card.Enchantment, clone.Enchantment));
        ctx.AssertEqual("combat clone preserves used Glam", 0, clone.GetEnchantedReplayCount());

        // Native save contains permanent enchantments; loading restores them
        // before replaying OnUpgrade. Never append a second Glam during load.
        CardModel loaded = CardModel.FromSerializable(card.ToSerializable());
        ctx.AssertEqual("save/load upgrade level", upgraded, loaded.IsUpgraded);
        ctx.AssertTrue("save/load enchantment", (loaded.Enchantment is Glam) == upgraded);
        ctx.AssertEqual("save/load fixed damage", 8m, loaded.DynamicVars.Damage.BaseValue);
        ctx.AssertEqual("save/load fixed vulnerable", 2m, loaded.DynamicVars["VulnerablePower"].BaseValue);
        if (upgraded) ctx.AssertEqual("save/load one Glam", 1, loaded.Enchantment!.Amount);

        var preview = (DarkStorm)ModelDb.Card<DarkStorm>().ToMutable();
        preview.UpgradeInternal(); // No Owner, no Pile, no CombatState.
        preview.FinalizeUpgradeInternal();
        ctx.AssertTrue("ownerless upgrade preview enchanted", preview.Enchantment is Glam);
        ctx.AssertTrue("preview does not mutate canonical card", ModelDb.Card<DarkStorm>().Enchantment == null);
        ctx.AssertEqual("preview fixed damage", 8m, preview.DynamicVars.Damage.BaseValue);

        var deck = ctx.Player.RunState.CreateCard<DarkStorm>(ctx.Player);
        await CardPileCmd.Add(deck, PileType.Deck, skipVisuals: true);
        ctx.AssertTrue("base pickup no longer enchants", deck.Enchantment == null);
        DarkStorm combat = await ctx.Add<DarkStorm>(PileType.Hand);
        combat.DeckVersion = deck;
        CardCmd.Upgrade(combat, CardPreviewStyle.None);
        ctx.AssertTrue("real combat upgrade attaches Glam", combat.IsUpgraded && combat.Enchantment is Glam);
        ctx.AssertTrue("combat upgrade never mutates deck", !deck.IsUpgraded && deck.Enchantment == null);
        var once = combat.Enchantment;
        CardCmd.Upgrade(combat, CardPreviewStyle.None);
        ctx.AssertTrue("repeat upgrade cannot reapply", ReferenceEquals(once, combat.Enchantment) && once!.Amount == 1);
        CardCmd.Upgrade(deck, CardPreviewStyle.None);
        ctx.AssertTrue("real deck upgrade attaches Glam", deck.IsUpgraded && deck.Enchantment is Glam);
        CardModel deckLoaded = ctx.Player.RunState.LoadCard(deck.ToSerializable(), ctx.Player);
        ctx.AssertTrue("upgraded deck roundtrip", deckLoaded.IsUpgraded && deckLoaded.Enchantment is Glam { Amount: 1 });
        await CardPileCmd.RemoveFromDeck([deck]);

        DarkStorm existing = await ctx.Add<DarkStorm>(PileType.Hand);
        var sharp = CombatEnchantmentCmd.ApplyVanilla<Sharp>(existing, 3);
        CardCmd.Upgrade(existing, CardPreviewStyle.None);
        ctx.AssertTrue("normal single-slot upgrade preserves existing enchantment",
            existing.IsUpgraded && ReferenceEquals(sharp, existing.Enchantment) && sharp.Amount == 3);

        // An old save can contain an unupgraded pickup-Glam. Preserve it; do
        // not destructively migrate or double it when later upgraded.
        var legacy = (DarkStorm)ModelDb.Card<DarkStorm>().ToMutable();
        legacy.EnchantedOnPickup = true;
        CardCmd.Enchant<Glam>(legacy, 1);
        var legacyLoaded = (DarkStorm)CardModel.FromSerializable(legacy.ToSerializable());
        ctx.AssertTrue("legacy saved field and enchantment survive",
            legacyLoaded.EnchantedOnPickup && !legacyLoaded.IsUpgraded && legacyLoaded.Enchantment is Glam);
        var legacyEnchant = legacyLoaded.Enchantment;
        legacyLoaded.UpgradeInternal();
        legacyLoaded.FinalizeUpgradeInternal();
        ctx.AssertTrue("legacy upgrade preserves one existing Glam",
            ReferenceEquals(legacyEnchant, legacyLoaded.Enchantment) && legacyEnchant!.Amount == 1);
    }
}
#endif
