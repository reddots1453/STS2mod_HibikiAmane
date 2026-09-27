using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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
        if (card == this
            && !EnchantedOnPickup
            && oldPileType == PileType.None
            && card.Pile?.Type == PileType.Deck)
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
        if (card == this
            && !EnchantedOnPickup
            && oldPileType == PileType.None
            && card.Pile?.Type == PileType.Deck)
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
    private const int RequiredPlays = 6;
    private int _timesPlayed;

    [SavedProperty]
    public int TimesPlayed
    {
        get => _timesPlayed;
        set
        {
            AssertMutable();
            _timesPlayed = Math.Clamp(value, 0, RequiredPlays);
            DynamicVars["Remaining"].BaseValue = RequiredPlays - _timesPlayed;
        }
    }

    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<TezcatarasEmber>();
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9, ValueProp.Move), new DynamicVar("Remaining", RequiredPlays)];

    public FlameSword()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

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

        if (Enchantment is TezcatarasEmber)
            return;

        FlameSword persistent = DeckVersion as FlameSword ?? this;
        persistent.TimesPlayed++;
        TimesPlayed = persistent.TimesPlayed;
        if (persistent.TimesPlayed < RequiredPlays)
            return;

        if (persistent != this && persistent.Enchantment == null)
            CardCmd.Enchant<TezcatarasEmber>(persistent, 1);

        if (Enchantment == null && CombatEnchantmentCmd.IsCombatClone(this))
            CombatEnchantmentCmd.ApplyVanilla<TezcatarasEmber>(this, 1);
        else if (persistent == this && Enchantment == null)
            CardCmd.Enchant<TezcatarasEmber>(this, 1);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}
