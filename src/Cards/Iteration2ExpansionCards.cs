using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class SoulFuenika : MSHolyCard
{
    [SavedProperty]
    public List<SerializableCard> PendingPostCombatCards { get; set; } = [];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public SoulFuenika()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay play)
    {
        List<CardModel> choices = CardFactory.GetDistinctForCombat(
            Owner,
            ModelDb.CardPool<MSHolyCardPool>().GetUnlockedCards(
                Owner.UnlockState,
                Owner.RunState.CardMultiplayerConstraint),
            3,
            Owner.RunState.Rng.CombatCardGeneration).ToList();
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            context, choices, Owner, canSkip: true);
        if (selected != null)
        {
            SoulFuenika persistent = DeckVersion as SoulFuenika ?? this;
            persistent.PendingPostCombatCards.Add(selected.ToSerializable());
            await CardPileCmd.AddGeneratedCardToCombat(
                selected, PileType.Hand, Owner);
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        SerializableCard[] cards = PendingPostCombatCards.ToArray();
        PendingPostCombatCards.Clear();
        foreach (SerializableCard savedCard in cards)
        {
            CardModel copy = Owner.RunState.LoadCard(savedCard, Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(copy, PileType.Deck));
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class FamiliarContract : MSHolyCard
{
    protected override bool HasEnergyCostX => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<FamiliarEnchantment>();

    public FamiliarContract()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay play)
    {
        IEnumerable<CardModel> generated = CardFactory.GetForCombat(
            Owner,
            ModelDb.CardPool<MSHolyCardPool>().GetUnlockedCards(
                Owner.UnlockState,
                Owner.RunState.CardMultiplayerConstraint),
            ResolveEnergyXValue(),
            Owner.RunState.Rng.CombatCardGeneration);
        foreach (CardModel card in generated)
        {
            if (IsUpgraded)
            {
                CardCmd.Upgrade(card);
            }
            await CardPileCmd.AddGeneratedCardToCombat(
                card, PileType.Hand, Owner);
            CombatEnchantmentCmd.Apply<FamiliarEnchantment>(card, 1);
        }
    }

    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class LightWings : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9, ValueProp.Move)];

    public LightWings()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(context);

        CardPile drawPile = PileType.Draw.GetPile(Owner);
        CardModel? enchanted = drawPile.Cards
            .FirstOrDefault(card => card.Enchantment != null);
        if (enchanted != null)
        {
            drawPile.MoveToTopInternal(enchanted);
            await CardPileCmd.Draw(context, 1, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class OpeningPrayer : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<OpeningPrayerPower>(2)];

    public OpeningPrayer()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<OpeningPrayerPower>(
            context,
            Owner.Creature,
            DynamicVars["OpeningPrayerPower"].BaseValue,
            Owner.Creature,
            this);

    protected override void OnUpgrade() =>
        DynamicVars["OpeningPrayerPower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class GaleSword : MSNeutralCard
{
    [SavedProperty]
    public bool EnchantedOnPickup { get; set; }

    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<Swift>(2);
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(11, ValueProp.Move)];

    public GaleSword()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? source)
    {
        if (!EnchantedOnPickup
            && PickupEnchantmentCmd.IsPickupOrDeckTransformation(this, card, oldPileType))
        {
            if (Enchantment == null)
                PickupEnchantmentCmd.EnchantAndPreview<Swift>(this, 2);
            EnchantedOnPickup = Enchantment is Swift;
        }
        return Task.CompletedTask;
    }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(context);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class ShiningSword : MSNeutralCard
{
    [SavedProperty]
    public bool EnchantedOnPickup { get; set; }

    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<Vigorous>(3);
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4, ValueProp.Move), new RepeatVar(2)];

    public ShiningSword()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? source)
    {
        if (!EnchantedOnPickup
            && PickupEnchantmentCmd.IsPickupOrDeckTransformation(this, card, oldPileType))
        {
            if (Enchantment == null)
                PickupEnchantmentCmd.EnchantAndPreview<Vigorous>(this, 3);
            EnchantedOnPickup = Enchantment is Vigorous;
        }
        return Task.CompletedTask;
    }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(context);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class FlameSword : MSNeutralCard
{
    private const int RequiredCombats = 5;
    private int _completedCombats;
    private WeakReference<CombatRoom>? _lastCountedRoom;

    // Preserve the old serialized property, but plays are not completed combats.
    // Existing Ember enchantments also remain intact during native save loading.
    [SavedProperty]
    public int TimesPlayed { get; set; }

    [SavedProperty]
    public int CompletedCombats
    {
        get => _completedCombats;
        set
        {
            AssertMutable();
            _completedCombats = Math.Clamp(value, 0, RequiredCombats);
            DynamicVars["Remaining"].BaseValue = RequiredCombats - _completedCombats;
        }
    }

    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<TezcatarasEmber>();
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9, ValueProp.Move), new DynamicVar("Remaining", RequiredCombats)];

    public FlameSword()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override Task OnPlay(
        PlayerChoiceContext context,
        CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(context);
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        // Run hooks enumerate deck cards AND combat clones. Only the permanent
        // instance earns progress, even when its combat copy is sealed/unplayed.
        if (Pile?.Type != PileType.Deck || HasBeenRemovedFromState
            || !Owner.Deck.Cards.Contains(this)
            || (_lastCountedRoom?.TryGetTarget(out var previousRoom) == true && ReferenceEquals(room, previousRoom))
            || Enchantment is TezcatarasEmber)
            return Task.CompletedTask;

        _lastCountedRoom = new(room);
        CompletedCombats++;
        // A normal card still has one enchantment slot. Do not destroy another
        // enchantment to grant this one, or throw at combat end when it is full.
        if (CompletedCombats == RequiredCombats && Enchantment == null)
        {
            CardCmd.Enchant<TezcatarasEmber>(this, 1);
            if (LocalContext.IsMine(this)) EnchantmentVfxCmd.Preview(this);
        }
        return Task.CompletedTask;
    }

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("ShowRemaining", Enchantment is not TezcatarasEmber);

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _lastCountedRoom = null;
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}
