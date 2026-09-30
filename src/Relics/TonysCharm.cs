using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Core.Rewards;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Relics;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class TonysCharm : MSRelicTemplate
{
    [SavedProperty] public bool PickupEffectGranted { get; set; }
    private readonly HashSet<CardModel> _resolving = [];
    public override RelicRarity Rarity => RelicRarity.Shop;
    public override bool HasUponPickupEffect => true;
    public override RelicAssetProfile AssetProfile => RelicIconAssets.For("character_TonysTalisman");

    public override async Task AfterObtained()
    {
        if (PickupEffectGranted || Owner.Character is not MaidenSuccubusCharacter) return;
        PickupEffectGranted = true;
        var selected = await CardSelectCmd.FromDeckForRemoval(Owner,
            new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2));
        foreach (var card in selected.Distinct().Take(2))
        {
            if (!Owner.Relics.Contains(this)) break;
            if (card.Owner == Owner && card.Pile?.Type == PileType.Deck && card.IsRemovable)
                await CardPileCmd.RemoveFromDeck(card);
        }
    }

    public override async Task BeforeCardRemoved(CardModel card)
    {
        if (Owner.Character is not MaidenSuccubusCharacter || !Owner.Relics.Contains(this)
            || card.Owner != Owner || card.Pile?.Type != PileType.Deck || !_resolving.Add(card)) return;
        try
        {
            var candidates = UnifiedRouteCardPool.Get(Owner, rareOnly: true);
            if (candidates.Length == 0) return;
            var reward = Owner.RunState.CreateCard(Owner.PlayerRng.Rewards.NextItem(candidates)!, Owner);
            CardCmd.Upgrade(reward);
            Flash();
            await CardPileCmd.Add(reward, PileType.Deck);
        }
        finally { _resolving.Remove(card); }
    }
}
