using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class Surf : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4, ValueProp.Move), new EnergyVar(3)];
    public Surf() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        int totalCost = 0;
        while (totalCost < DynamicVars.Energy.IntValue)
        {
            if (PileType.Hand.GetPile(Owner).Cards.Count >= 10) break;
            IEnumerable<CardModel> drawn = await CardPileCmd.Draw(context, 1, Owner);
            CardModel? card = drawn.FirstOrDefault();
            if (card == null) break;
            totalCost += card.EnergyCost.CostsX
                ? 0
                : Math.Max(0, card.EnergyCost.GetAmountToSpend());
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
                .TargetingAllOpponents(CombatState).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1);
}

public sealed class PressBack : MSNeutralCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    public PressBack() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel? selected = (await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(new LocString("card_selection", "MAIDEN_SUCCUBUS_TO_DRAW_BOTTOM"), 1),
            card => card != this, this)).FirstOrDefault();
        if (selected != null)
            await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Bottom);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class AcceleratedMotion : MSNeutralCard
{
    [SavedProperty]
    public bool AddedPickupCopies { get; set; }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new DynamicVar("Copies", 1)];
    public AcceleratedMotion() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card != this || AddedPickupCopies || oldPileType != PileType.None || card.Pile?.Type != PileType.Deck)
            return;
        AddedPickupCopies = true;
        for (int i = 0; i < DynamicVars["Copies"].IntValue; i++)
        {
            AcceleratedMotion copy = Owner.RunState.CreateCard<AcceleratedMotion>(Owner);
            copy.AddedPickupCopies = true;
            if (IsUpgraded) CardCmd.Upgrade(copy);
            await CardPileCmd.Add(copy, PileType.Deck);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class MagicSword : MSNeutralCard
{
    [SavedProperty]
    public bool EnchantedOnPickup { get; set; }

    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<ChargeEnchantment>(2);

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move)];
    public MagicSword() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

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
                PickupEnchantmentCmd.EnchantAndPreview<ChargeEnchantment>(this, 2);
            EnchantedOnPickup = Enchantment is ChargeEnchantment;
        }
        return Task.CompletedTask;
    }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class ChangePanties : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<SlipperyPower>(2), new PowerVar<VulnerablePower>(2),
            new DynamicVar("Threshold", 6)];
    public ChangePanties() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        card != this
            || Data.Desire.Get(Owner) >= DynamicVars["Threshold"].IntValue;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<SlipperyPower>(context, Owner.Creature,
            DynamicVars["SlipperyPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(context, Owner.Creature,
            DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() =>
        DynamicVars["Threshold"].UpgradeValueBy(-1);
}
