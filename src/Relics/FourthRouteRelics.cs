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
using MegaCrit.Sts2.Core.HoverTips;
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
using MaidenSuccubus.Core.Relics;
using System.Runtime.CompilerServices;
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
            _stage = Math.Clamp(value, 0, 4);
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
    private string _routeTitleKey = "";
    [SavedProperty] public string RouteTitleKey { get => _routeTitleKey; set { AssertMutable(); _routeTitleKey = value; } }
    public override LocString Title
    {
        get
        {
            if (string.IsNullOrEmpty(_routeTitleKey)) return base.Title;
            LocString title = new("relics", "MAIDEN_SUCCUBUS_RELIC_FOURTH_ROUTE_FRAGMENT_RELIC.routeTitle");
            title.Add("Relic", new LocString("relics", _routeTitleKey));
            return title;
        }
    }
    public override RelicRarity Rarity => RelicRarity.Event;
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/circlet.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres");
    public override bool IsAllowed(MegaCrit.Sts2.Core.Runs.IRunState runState) => false;
    public override Task AfterObtained() => FourthRouteProgressService.UnlockSecondTrial(Owner);
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class PrideRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Pride;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => Stage == 0
        ? base.AdditionalHoverTips
        : base.AdditionalHoverTips.Concat([
            HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<SelfImportantPower>()]);
    public override async Task BeforeCombatStart()
    {
        if (Stage == 0) return;
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
        if (Stage == 0) return;
        if (Stage <= 2) await PlayerCmd.GainGold(100, Owner);
        if (Stage >= 3)
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
        if (Stage == 0 || _used || context.Definition.Id != DesireResource.Id || context.Player != Owner || context.Delta <= 0) return;
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
    private EnvyTriggerState _trigger;
    [SavedProperty]
    public bool UsedThisWindow { get => _trigger.Used; set { AssertMutable(); _trigger.Used = value; } }
    public override FourthRouteQuest Quest => FourthRouteQuest.Envy;
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        bool ownApplication = applier == Owner.Creature && Owner.Creature.CombatState != null
            && power.Owner.CombatState == Owner.Creature.CombatState;
        if (!_trigger.TryUse(Stage, ownApplication, amount != 0 && power.GetTypeForAmount(amount) == PowerType.Debuff)) return;
        Flash();
        await PlayerCmd.GainEnergy(1, Owner);
        int draw = EnvyTriggerState.CardsToDraw(Stage);
        if (draw > 0) await CardPileCmd.Draw(context, draw, Owner);
    }
    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        _trigger.StartTurn(Stage, combatState == Owner.Creature.CombatState
            && side == Owner.Creature.Side && participants.Contains(Owner.Creature));
        return Task.CompletedTask;
    }
    public override Task BeforeCombatStart() { _trigger = default; return Task.CompletedTask; }
    public override Task AfterCombatEnd(CombatRoom room) { _trigger = default; return Task.CompletedTask; }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class GluttonyRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Gluttony;
    public override bool HasUponPickupEffect => Stage > 0;
    [SavedProperty] public bool PickupEffectGranted { get; set; }
    public override async Task AfterObtained()
    {
        if (Stage == 0 || PickupEffectGranted) return;
        PickupEffectGranted = true;
        int maxHp = GluttonyRules.PickupMaxHp(Stage);
        if (maxHp > 0)
        {
            await CreatureCmd.GainMaxHp(Owner.Creature, maxHp);
            await PlayerCmd.GainMaxPotionCount(GluttonyRules.PickupSlots(Stage), Owner);
        }
        else if (GluttonyRules.FillOnPickup(Stage))
        {
            int empty = GluttonyRules.EmptySlots(Owner.MaxPotionCount, Owner.Potions.Count());
            if (empty == 0) return;
            IEnumerable<PotionModel> potions = PotionFactory.CreateRandomPotionsOutOfCombat(Owner, empty, Owner.RunState.Rng.CombatPotionGeneration);
            foreach (PotionModel potion in potions) await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }
    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        int amount = GluttonyRules.MaxHpOnPotionUsed(Stage, potion.Owner == Owner);
        return amount > 0 ? CreatureCmd.GainMaxHp(Owner.Creature, amount) : Task.CompletedTask;
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class WrathRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Wrath;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => Stage == 0
        ? base.AdditionalHoverTips
        : base.AdditionalHoverTips.Concat(HoverTipFactory.FromEnchantment<WrathEnchantment>());
    public override bool HasUponPickupEffect => WrathRules.EnchantOnPickup(Stage);
    [SavedProperty] public bool PickupEffectGranted { get; set; }
    public override async Task AfterObtained()
    {
        if (Stage == 0 || !WrathRules.EnchantOnPickup(Stage) || PickupEffectGranted) return;
        PickupEffectGranted = true;
        var enchantment = ModelDb.Enchantment<WrathEnchantment>().ToMutable();
        CardSelectorPrefs prefs = new(CardSelectorPrefs.EnchantSelectionPrompt, 1) { Cancelable = false };
        CardModel? selected = (await CardSelectCmd.FromDeckGeneric(Owner, prefs,
            card => card.Type == CardType.Attack && LayeredEnchantments.HasOpenSlot(card)
                && enchantment.CanEnchant(card))).FirstOrDefault();
        if (selected != null && selected.Owner == Owner && selected.Pile?.Type == PileType.Deck)
            CardCmd.Enchant(enchantment, selected, 1);
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        WrathRules.AwakenedBonus(Stage, props.IsPoweredAttack(), cardSource?.Owner == Owner,
            cardSource?.Type == CardType.Attack, cardSource != null && LayeredEnchantments.Has<WrathEnchantment>(cardSource));
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class SlothRouteRelic : FourthRouteRelic
{
    private SlothTurnState _turn;
    [SavedProperty]
    public int EnergySpentThisTurn { get => _turn.EnergySpent; set { AssertMutable(); _turn.EnergySpent = Math.Max(0, value); } }
    [SavedProperty]
    public bool TriggeredForNextTurn { get => _turn.PendingEnergy; set { AssertMutable(); _turn.PendingEnergy = value; } }
    [SavedProperty]
    public bool TurnEndResolved { get => _turn.EndResolved; set { AssertMutable(); _turn.EndResolved = value; } }
    public override FourthRouteQuest Quest => FourthRouteQuest.Sloth;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => Stage is 3 or 4
        ? base.AdditionalHoverTips.Concat([HoverTipFactory.Static(StaticHoverTip.Block)])
        : base.AdditionalHoverTips;
    public override Task BeforeCombatStart() { _turn = default; return Task.CompletedTask; }
    public override Task AfterCombatEnd(CombatRoom room) { _turn = default; return Task.CompletedTask; }
    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        _turn.StartTurn(combatState == Owner.Creature.CombatState
            && side == Owner.Creature.Side && participants.Contains(Owner.Creature));
        return Task.CompletedTask;
    }
    public override Task AfterEnergySpent(CardModel card, int amount)
    {
        _turn.RecordPayment(amount, Stage > 0 && card.Owner == Owner && Owner.Creature.CombatState != null);
        return Task.CompletedTask;
    }
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        int block = _turn.ResolveEnd(Stage, Owner.Creature.CombatState != null
            && side == Owner.Creature.Side && participants.Contains(Owner.Creature));
        if (block > 0) await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Unpowered, null);
    }
    public override async Task AfterEnergyReset(Player player)
    {
        int energy = _turn.TakeEnergy(Stage, player == Owner && Owner.Creature.CombatState != null);
        if (energy > 0) await PlayerCmd.GainEnergy(energy, Owner);
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class HumilityRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Humility;
    public override async Task BeforeCombatStart()
    {
        if (Stage == 0) return;
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
        if (Stage == 0 || Stage > 2) return;
        List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(
            Owner, new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).ToList();
        await CardPileCmd.RemoveFromDeck(cards);
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class ChastityRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Chastity;
    public override Task BeforeCombatStart() => Stage == 0 ? Task.CompletedTask : PowerCmd.Apply<PreventNextDesireGainPower>(
        new BlockingPlayerChoiceContext(), Owner.Creature, Math.Min(Stage, 2), Owner.Creature, null);
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player) =>
        player == Owner && Stage >= 3 && Data.Desire.Get(Owner) <= 2
            ? PlayerCmd.GainEnergy(1, Owner) : Task.CompletedTask;
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class BenevolenceRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Benevolence;
    public override bool HasUponPickupEffect => VirtuePickupRules.BenevolencePickupCount(Stage) > 0;
    [SavedProperty] public bool PickupEffectGranted { get; set; }
    public override Task AfterObtained()
    {
        if (Stage == 0 || PickupEffectGranted) return Task.CompletedTask;
        PickupEffectGranted = true;
        UpgradeRandom(VirtuePickupRules.BenevolencePickupCount(Stage));
        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (VirtuePickupRules.BenevolenceOnAdded(Stage, card.Owner == Owner,
            oldPileType == PileType.Deck, card.Pile?.Type == PileType.Deck)) UpgradeRandom(2);
        return Task.CompletedTask;
    }

    private void UpgradeRandom(int count)
    {
        if (count == 0) return;
        List<CardModel> candidates = Owner.Deck.Cards.Where(card => card.IsUpgradable).ToList();
        if (candidates.Count == 0) return;
        foreach (CardModel card in candidates.StableShuffle(Owner.RunState.Rng.Niche).Take(count))
            if (card.Pile?.Type == PileType.Deck && card.IsUpgradable)
                CardCmd.Upgrade(card, CardPreviewStyle.MessyLayout);
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class TemperanceRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Temperance;
    public override async Task BeforeCombatStart()
    {
        if (Stage == 0 || Owner.Creature.CombatState is not { } combat) return;
        CardPile pile = PileType.Draw.GetPile(Owner);
        int maximum = VirtueCombatRules.TemperanceMaximum(Stage);
        if (maximum == 0 || pile.Cards.Count == 0) return;
        IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(new BlockingPlayerChoiceContext(), pile, Owner,
            SelectionPrefs(Stage));
        // The selector does not move cards: only annotate surviving combat instances.
        if (Owner.Creature.CombatState != combat) return;
        foreach (CardModel card in selected)
        {
            if (card.Owner != Owner || card.Pile != pile) continue;
            card.AddKeyword(CardKeyword.Exhaust);
        }
    }

    internal static CardSelectorPrefs SelectionPrefs(int stage) => new(
        new LocString("card_selection", "MAIDEN_SUCCUBUS_TEMPERANCE_ADD_EXHAUST"),
        0, VirtueCombatRules.TemperanceMaximum(stage));
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class PatienceRouteRelic : FourthRouteRelic
{
    public override FourthRouteQuest Quest => FourthRouteQuest.Patience;
    public override Task BeforeCombatStart() =>
        VirtueCombatRules.PatienceAtCombatStart(Stage) ? CreateHoly() : Task.CompletedTask;
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player) =>
        VirtueCombatRules.PatienceAtTurnStart(Stage, player == Owner) ? CreateHoly() : Task.CompletedTask;
    private async Task CreateHoly()
    {
        if (Owner.Creature.CombatState is not { } combat || Owner.PlayerCombatState == null) return;
        List<CardModel> pool = ModelDb.CardPool<MSHolyCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(card => (card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
                && card.CanBeGeneratedInCombat)
            .ToList();
        if (pool.Count == 0) return;
        CardModel canonical = pool[Owner.RunState.Rng.CombatCardGeneration.NextInt(pool.Count)];
        CardModel card = combat.CreateCard(canonical, Owner);
        PrepareGeneratedCard(card, Stage);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }

    internal static void PrepareGeneratedCard(CardModel card, int stage)
    {
        if (!VirtueCombatRules.PatienceDiscount(stage)) return;
        // Relative, energy-only, persists across turns until this instance is played.
        card.EnergyCost.AddUntilPlayed(-1);
        card.InvokeEnergyCostChanged();
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class DiligenceRouteRelic : FourthRouteRelic
{
    private ConditionalWeakTable<CardModel, object> _prepared = new();
    private ConditionalWeakTable<CardModel, object> _pendingReveal = new();
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _prepared = new();
        _pendingReveal = new();
    }
    public override FourthRouteQuest Quest => FourthRouteQuest.Diligence;
    public override bool HasUponPickupEffect => Stage > 0;
    [SavedProperty] public bool PickupRewardsGranted { get; set; }
    public override async Task AfterObtained()
    {
        if (Stage == 0 || PickupRewardsGranted) return;
        PickupRewardsGranted = true;
        await RewardsCmd.OfferCustom(Owner, CreatePickupRewards());
    }

    internal List<Reward> CreatePickupRewards()
    {
        DiligenceRewardRule rule = VirtuePickupRules.Diligence(Stage);
        List<Reward> rewards = [];
        for (int i = 0; i < rule.Count; i++)
        {
            CardReward reward = new(CardCreationOptions.ForRoom(Owner, RoomType.Monster), 3, Owner);
            reward.AfterGenerated += () => PrepareRewardOptions(reward.Cards, rule);
            rewards.Add(reward);
        }
        return rewards;
    }

    internal static IReadOnlyList<EnchantmentModel> EnchantmentOptions(CardModel card) =>
        ModelDb.DebugEnchantments.Where(enchantment =>
            enchantment.GetType().Assembly == typeof(EnchantmentModel).Assembly
            && enchantment.GetType().Namespace == "MegaCrit.Sts2.Core.Models.Enchantments"
            && enchantment is not DeprecatedEnchantment)
            .Select(enchantment => enchantment.ToMutable()).Where(enchantment => enchantment.CanEnchant(card)).ToArray();

    internal void PrepareRewardOptions(IEnumerable<CardModel> cards, DiligenceRewardRule rule)
    {
        foreach (CardModel card in cards)
        {
            if (card.Owner != Owner || _prepared.TryGetValue(card, out _)) continue;
            _prepared.Add(card, new object());
            if (rule.Upgrade && card.IsUpgradable) CardCmd.Upgrade(card, CardPreviewStyle.None);
            if (!rule.Enchant) continue;
            IReadOnlyList<EnchantmentModel> options = EnchantmentOptions(card);
            if (options.Count == 0) continue;
            var rng = Owner.RunState.Rng.Niche;
            EnchantmentModel enchantment = options[rng.NextInt(options.Count)];
            int amount = VirtuePickupRules.RollEnchantmentAmount(enchantment.GetType().Name, rng.NextInt);
            if (CardCmd.Enchant(enchantment, card, amount) != null) _pendingReveal.Add(card, new object());
        }
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card.Owner == Owner && oldPileType != PileType.Deck && card.Pile?.Type == PileType.Deck
            && _pendingReveal.Remove(card)) EnchantmentVfxCmd.PreviewAfterCardPickup(card);
        return Task.CompletedTask;
    }
}
