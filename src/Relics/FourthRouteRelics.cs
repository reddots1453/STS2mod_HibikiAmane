using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Relics;

public abstract class FourthRouteRelic : ModRelicTemplate
{
    private int _stage = 1;
    [SavedProperty]
    public int Stage
    {
        get => _stage;
        set
        {
            AssertMutable();
            _stage = Math.Clamp(value, 1, 4);
            if (DynamicVars.TryGetValue("Stage", out DynamicVar? stageVar)) stageVar.BaseValue = _stage;
        }
    }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Stage", 1)];
    public override LocString Title
    {
        get
        {
            LocString title = new("relics", "MAIDEN_SUCCUBUS_RELIC_FOURTH_ROUTE_STAGE.title");
            title.Add("Relic", new LocString("relics", Id.Entry + ".title"));
            title.Add("Stage", new LocString("relics", $"MAIDEN_SUCCUBUS_RELIC_FOURTH_ROUTE_STAGE_{Stage}.title"));
            return title;
        }
    }
    public abstract FourthRouteQuest Quest { get; }
    public override RelicRarity Rarity => RelicRarity.Event;
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/circlet.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres");
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class FourthRouteFragmentRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/circlet.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres");
    public override bool IsAllowed(MegaCrit.Sts2.Core.Runs.IRunState runState) => false;
    public override async Task AfterObtained()
    {
        await FourthRouteProgressService.AdvanceStage(Owner, 1);
        await RelicCmd.Remove(this);
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class PrideRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Pride;
    public override async Task BeforeCombatStart()
    {
        int amount = Math.Min(Stage, 3);
        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Owner.Creature, amount, Owner.Creature, null);
        await PowerCmd.Apply<SelfImportantPower>(new BlockingPlayerChoiceContext(), Owner.Creature, amount, Owner.Creature, null);
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class GreedRouteRelic : FourthRouteRelic
{
    private bool _freeShopPending;
    private bool _freeShopActive;
    [SavedProperty] public bool FreeShopPending { get => _freeShopPending; set { AssertMutable(); _freeShopPending = value; } }
    [SavedProperty] public bool FreeShopActive { get => _freeShopActive; set { AssertMutable(); _freeShopActive = value; } }
    public override FourthRouteQuest Quest => FourthRouteQuest.Greed;
    public override bool HasUponPickupEffect => true;
    public override async Task AfterObtained()
    {
        if (Stage <= 2) await PlayerCmd.GainGold(100, Owner);
        if (Stage == 3)
        {
            await CardPileCmd.AddCurseToDeck<Greed>(Owner);
            FreeShopPending = true;
        }
    }
    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost) =>
        player == Owner && FreeShopActive ? 0m : cost;
    internal void OnMerchantCreated()
    {
        if (FreeShopActive) { FreeShopActive = false; Status = RelicStatus.Disabled; }
        if (FreeShopPending) { FreeShopPending = false; FreeShopActive = true; Status = RelicStatus.Active; }
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class LustRouteRelic : FourthRouteRelic, ISecondaryResourceHookListener
{
    private bool _used;
    [SavedProperty]
    public bool UsedThisCombat { get => _used; set { AssertMutable(); _used = value; } }
    public override FourthRouteQuest Quest => FourthRouteQuest.Lust;
    public async Task AfterSecondaryResourceChanged(SecondaryResourceChangeContext context)
    {
        if (_used || context.Definition.Id != DesireResource.Id || context.Player != Owner || context.Delta <= 0) return;
        _used = true;
        Flash();
        for (int i = 0; i < Math.Min(Stage, 2); i++)
        {
            HashSet<CardModel> before = PileType.Hand.GetPile(Owner).Cards.ToHashSet();
            await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), Owner);
            CardModel? card = PileType.Hand.GetPile(Owner).Cards.FirstOrDefault(candidate => !before.Contains(candidate));
            if (Stage >= 3) card?.EnergyCost.SetThisTurnOrUntilPlayed(0);
        }
    }
    public override Task AfterCombatEnd(CombatRoom room) { _used = false; return Task.CompletedTask; }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class EnvyRouteRelic : FourthRouteRelic
{
    private bool _used;
    [SavedProperty]
    public bool UsedThisWindow { get => _used; set { AssertMutable(); _used = value; } }
    public override FourthRouteQuest Quest => FourthRouteQuest.Envy;
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_used || amount <= 0 || power.Type != PowerType.Debuff || applier != Owner.Creature || power.Owner.Side == Owner.Creature.Side) return;
        _used = true;
        Flash();
        await PlayerCmd.GainEnergy(1, Owner);
        if (Stage >= 2) await CardPileCmd.Draw(context, Owner);
    }
    public override Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Creature.Side && Stage >= 3) _used = false;
        return Task.CompletedTask;
    }
    public override Task AfterCombatEnd(CombatRoom room) { _used = false; return Task.CompletedTask; }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class GluttonyRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Gluttony;
    public override bool HasUponPickupEffect => true;
    public override async Task AfterObtained()
    {
        if (Stage <= 2)
        {
            await CreatureCmd.GainMaxHp(Owner.Creature, 5);
            await PlayerCmd.GainMaxPotionCount(1, Owner);
        }
        else if (Stage == 3)
        {
            int empty = Owner.MaxPotionCount - Owner.Potions.Count();
            IEnumerable<PotionModel> potions = PotionFactory.CreateRandomPotionsOutOfCombat(Owner, empty, Owner.RunState.Rng.CombatPotionGeneration);
            foreach (PotionModel potion in potions) await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }
    public override Task AfterPotionUsed(PotionModel potion, Creature? target) =>
        Stage >= 3 ? CreatureCmd.GainMaxHp(Owner.Creature, 5) : Task.CompletedTask;
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class WrathRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Wrath;
    public override bool HasUponPickupEffect => true;
    public override async Task AfterObtained()
    {
        if (Stage > 2) return;
        CardSelectorPrefs prefs = new(CardSelectorPrefs.EnchantSelectionPrompt, 1) { Cancelable = false };
        CardModel? selected = (await CardSelectCmd.FromDeckGeneric(Owner, prefs,
            card => card.Type == CardType.Attack && card.Enchantment == null)).FirstOrDefault();
        if (selected != null) CardCmd.Enchant(ModelDb.Enchantment<WrathEnchantment>().ToMutable(), selected, 1);
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class SlothRouteRelic : FourthRouteRelic
{
    private decimal _energySpent;
    private bool _triggered;
    [SavedProperty]
    public decimal EnergySpentThisTurn { get => _energySpent; set { AssertMutable(); _energySpent = Math.Max(0, value); } }
    [SavedProperty]
    public bool TriggeredForNextTurn { get => _triggered; set { AssertMutable(); _triggered = value; } }
    public override FourthRouteQuest Quest => FourthRouteQuest.Sloth;
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner) _energySpent += cardPlay.Resources.EnergyValue;
        return Task.CompletedTask;
    }
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side) return;
        _triggered = _energySpent <= 2;
        _energySpent = 0;
        if (_triggered && Stage >= 3) await CreatureCmd.GainBlock(Owner.Creature, 12, ValueProp.Unpowered, null);
    }
    public override async Task AfterEnergyReset(Player player)
    {
        if (player == Owner && _triggered && Owner.Creature.CombatState?.RoundNumber > 1)
            await PlayerCmd.GainEnergy(Math.Min(Stage, 3), Owner);
        _triggered = false;
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class HumilityRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Humility;
    public override async Task BeforeCombatStart()
    {
        HumilityLesson card = Owner.RunState.CreateCard<HumilityLesson>(Owner);
        if (Stage >= 2) CardCmd.Upgrade(card);
        await CardPileCmd.Add(card, PileType.Hand);
    }
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (Stage < 3 || cardPlay.Card.Owner != Owner || cardPlay.Card is HumilityLesson) return Task.CompletedTask;
        bool pure = cardPlay.Card.Description.GetFormattedText().Split('.', '。').Count(part => !string.IsNullOrWhiteSpace(part)) <= 2;
        return pure ? CardPileCmd.Draw(context, Owner) : Task.CompletedTask;
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class GenerosityRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Generosity;
    public override bool HasUponPickupEffect => true;
    public override async Task AfterObtained()
    {
        if (Stage > 2) return;
        List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(
            Owner, new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).ToList();
        await CardPileCmd.RemoveFromDeck(cards);
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class ChastityRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Chastity;
    public override Task BeforeCombatStart() => PowerCmd.Apply<PreventNextDesireGainPower>(
        new BlockingPlayerChoiceContext(), Owner.Creature, Math.Min(Stage, 2), Owner.Creature, null);
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player) =>
        player == Owner && Stage >= 3 && Data.Desire.Get(Owner) <= 2
            ? PlayerCmd.GainEnergy(1, Owner) : Task.CompletedTask;
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class BenevolenceRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Benevolence;
    public override bool HasUponPickupEffect => true;
    public override async Task AfterObtained()
    {
        if (Stage == 4) return;
        int count = Stage >= 3 ? 3 : 2;
        CardCreationOptions options = new(AllMaidenSuccubusCards.Pools, CardCreationSource.Other, CardRarityOddsType.RegularEncounter);
        List<Reward> rewards = [];
        for (int i = 0; i < count; i++)
        {
            var reward = new CardReward(options, 3, Owner);
            if (Stage >= 3)
                reward.AfterGenerated += () => BlessRewardOptions(reward);
            rewards.Add(reward);
        }
        await RewardsCmd.OfferCustom(Owner, rewards);
    }

    private void BlessRewardOptions(CardReward reward)
    {
        foreach (CardModel card in reward.Cards)
        {
            if (card.IsUpgradable)
                CardCmd.Upgrade(card);
            List<EnchantmentModel> options = ModelDb.DebugEnchantments
                .Where(enchantment => enchantment.GetType().Namespace?.StartsWith("MegaCrit.Sts2.Core.Models.Enchantments") == true)
                .Select(enchantment => enchantment.ToMutable())
                .Where(enchantment => enchantment.CanEnchant(card))
                .ToList().StableShuffle(Owner.RunState.Rng.Niche);
            if (options.FirstOrDefault() is { } enchantment)
                CardCmd.Enchant(enchantment, card, 1);
        }
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class TemperanceRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Temperance;
    public override async Task BeforeCombatStart()
    {
        CardPile pile = PileType.Draw.GetPile(Owner);
        int count = Math.Min(Stage, 2);
        IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(new BlockingPlayerChoiceContext(), pile, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, count));
        foreach (CardModel card in selected)
        {
            card.AddKeyword(CardKeyword.Exhaust);
            if (Stage >= 3 && card.Enchantment == null && ModelDb.Enchantment<Swift>().CanEnchant(card))
                CombatEnchantmentCmd.ApplyVanilla<Swift>(card, 2);
        }
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class PatienceRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Patience;
    public override Task BeforeCombatStart() => Stage < 3 ? CreateHoly() : Task.CompletedTask;
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player) =>
        player == Owner && Stage >= 3 ? CreateHoly() : Task.CompletedTask;
    private async Task CreateHoly()
    {
        List<CardModel> pool = ModelDb.CardPool<MSHolyCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(card => (card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
                && card.CanBeGeneratedInCombat)
            .ToList();
        CardModel canonical = pool[Owner.RunState.Rng.CombatCardGeneration.NextInt(pool.Count)];
        CardModel card = Owner.Creature.CombatState!.CreateCard(canonical, Owner);
        if (Stage >= 2) CardCmd.Upgrade(card);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class DiligenceRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Diligence;
    public override bool HasUponPickupEffect => true;
    public override Task AfterObtained()
    {
        if (Stage >= 3) return Task.CompletedTask;
        UpgradeRandom(Stage == 1 ? 2 : 3);
        return Task.CompletedTask;
    }
    public override Task AfterCombatVictory(CombatRoom room)
    {
        if (Stage >= 3) UpgradeRandom(1);
        return Task.CompletedTask;
    }
    private void UpgradeRandom(int count)
    {
        foreach (CardModel card in Owner.Deck.Cards.Where(card => card.IsUpgradable).ToList()
            .StableShuffle(Owner.RunState.Rng.Niche).Take(count))
            CardCmd.Upgrade(card, CardPreviewStyle.MessyLayout);
    }
}
