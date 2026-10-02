using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Relics;

[RegisterTouchOfOrobasRefinement(typeof(EternalOrb))]
[RegisterRelic(typeof(MSRelicPool))]
public sealed class HeroOrb : RetentionOrb
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override bool IsEternal => false;
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class EternalOrb : RetentionOrb
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override bool IsEternal => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat(HoverTipFactory.FromEnchantment<SlumberingEssence>());
}

public abstract class RetentionOrb : ModRelicTemplate
{
    protected abstract bool IsEternal { get; }
    public override RelicAssetProfile AssetProfile
    {
        get
        {
            if (this is EternalOrb) return RelicIconAssets.For("starter_EternalOrb");
            if (IsMutable && Owner is { Character: MaidenSuccubusCharacter, RunState: RunState run })
            {
                int corruption = CorruptionQuery.Get(run);
                if (corruption <= -4) return RelicIconAssets.For("starter_HeroOrb_Holy");
                if (corruption >= 4) return RelicIconAssets.For("starter_HeroOrb_Corrupt");
            }
            return RelicIconAssets.For("starter_HeroOrb");
        }
    }
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    public override async Task BeforeFlushLate(PlayerChoiceContext context, Player player)
    {
        if (player != Owner || player.Character is not MaidenSuccubusCharacter
            || player.RunState is not RunState run || player.Creature.IsDead) return;
        CardModel? card = (await CardSelectCmd.FromHand(context, player,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), null, this)).FirstOrDefault();
        if (card == null || card.Owner != player || card.Pile?.Type != PileType.Hand
            || !CombatEnchantmentCmd.IsCombatClone(card)) return;
        RetentionOrbRule rule = RetentionOrbRule.At(CorruptionQuery.Get(run), IsEternal);
        Flash();
        Task firstEnchantmentTick = Task.CompletedTask;
        using (ControlQuery.SuppressPresentation())
        {
            if (rule.PermanentRetain) CardCmd.ApplyKeyword(card, CardKeyword.Retain);
            else card.GiveSingleTurnRetain();
            if (rule.Upgrade && card.IsUpgradable) CardCmd.Upgrade(card);
            if (rule.Enchant && card.Enchantment == null
                && ModelDb.Enchantment<SlumberingEssence>().CanEnchant(card))
            {
                SlumberingEssence enchantment = CombatEnchantmentCmd.ApplyVanilla<SlumberingEssence>(card, 1);
                // All BeforeFlush listeners have already run. A newly attached
                // enchantment missed that snapshot; execute its real synchronous
                // callback once now. Existing enchantments must NOT run twice.
                // Never hold the thread-local presentation scope across await.
                firstEnchantmentTick = enchantment.BeforeFlush(context, player);
            }
        }
        await firstEnchantmentTick;
    }
}
