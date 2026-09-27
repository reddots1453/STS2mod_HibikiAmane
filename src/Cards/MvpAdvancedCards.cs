using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class AllCurseBite : MSCorruptCard
{
    private int _exhaustedAttackDamage;

    [SavedProperty]
    public int ExhaustedAttackDamage
    {
        get => _exhaustedAttackDamage;
        set
        {
            AssertMutable();
            _exhaustedAttackDamage = value;
            DynamicVars.ExtraDamage.BaseValue = value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(1),
        new ExtraDamageVar(ExhaustedAttackDamage),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier(static (_, _) => 1),
    ];

    public AllCurseBite()
        : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        return DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(context);
    }

    public override Task AfterCardExhausted(
        PlayerChoiceContext context,
        CardModel card,
        bool causedByEthereal)
    {
        if (card.Owner != Owner || card.Type != CardType.Attack)
        {
            return Task.CompletedTask;
        }
        int addedDamage;
        try
        {
            addedDamage = Math.Max(0, card.DynamicVars.Damage.IntValue);
        }
        catch (KeyNotFoundException)
        {
            try
            {
                addedDamage = Math.Max(
                    0, card.DynamicVars.CalculatedDamage.IntValue);
            }
            catch (KeyNotFoundException)
            {
                addedDamage = 0;
            }
        }
        ExhaustedAttackDamage += addedDamage;
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class CurseInfection : MSCorruptCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [CardHoverTipSupport.Static("MAIDENSUCCUBUS_CURSE_INFECTION")];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public CurseInfection() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override async Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal)
    {
        if (card != this) return;
        await CardPileCmd.Draw(context, 2, Owner);
        CurseInfectionStatus.TryApplyToRandomHandCard(Owner);
    }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class ForgeCharge : MSHolyCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<ChargeEnchantment>(DynamicVars["Charge"].IntValue);
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar> CanonicalVars =>
        [new MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar("Charge", 2)];
    public ForgeCharge() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardPile discard = PileType.Discard.GetPile(Owner);
        CardModel? selected = (await CardSelectCmd.FromCombatPile(context, discard, Owner,
            new CardSelectorPrefs(new LocString("card_selection", "MAIDEN_SUCCUBUS_TO_DRAW_TOP"), 1),
            card => LayeredEnchantments.HasOpenSlot(card) && ModelDb.Enchantment<ChargeEnchantment>().CanEnchant(card)))
            .FirstOrDefault();
        if (selected == null) return;
        await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Top);
        CombatEnchantmentCmd.ApplyAudited<ChargeEnchantment>(selected, DynamicVars["Charge"].BaseValue);
    }
    protected override void OnUpgrade() => DynamicVars["Charge"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class YarusMemory : MSHolyCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<SoulLinkEnchantment>();
    [SavedProperty] public bool LinkedOnPickup { get; set; }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];
    public YarusMemory() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card != this || LinkedOnPickup || oldPileType != PileType.None || card.Pile?.Type != PileType.Deck)
            return;
        LinkedOnPickup = true;
        SoulLinkEnchantment canonical = ModelDb.Enchantment<SoulLinkEnchantment>();
        if (Enchantment == null)
            PickupEnchantmentCmd.EnchantAndPreview(
                canonical.ToMutable(), this, 1);
        CardSelectorPrefs prefs = new(CardSelectorPrefs.EnchantSelectionPrompt, 0, 2)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };
        IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckGeneric(
            Owner, prefs, candidate => candidate != this && LayeredEnchantments.HasOpenSlot(candidate)
                && canonical.CanEnchant(candidate));
        foreach (CardModel target in selected)
            PickupEnchantmentCmd.EnchantAndPreview(
                canonical.ToMutable(), target, 1);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class EnchantmentChoiceCard : MSGeneratedCard
{
    public string ChoiceId { get; set; } = string.Empty;
    public override int MaxUpgradeLevel => 0;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("Enchant")];
    public EnchantmentChoiceCard() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None) { }
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips => ChoiceId switch
    {
        "swift" => HoverTipFactory.FromEnchantment<Swift>(4),
        "charge" => HoverTipFactory.FromEnchantment<ChargeEnchantment>(3),
        "glam" => HoverTipFactory.FromEnchantment<Glam>(),
        "ember" => HoverTipFactory.FromEnchantment<TezcatarasEmber>(),
        "instinct" => HoverTipFactory.FromEnchantment<Instinct>(),
        "proliferation" => HoverTipFactory.FromEnchantment<ProliferationEnchantment>(),
        "iron_wall" => HoverTipFactory.FromEnchantment<IronWallEnchantment>(),
        "nimble" => HoverTipFactory.FromEnchantment<Nimble>(8),
        "sharp" => HoverTipFactory.FromEnchantment<Sharp>(8),
        _ => [],
    };
    public void Configure(string id, string display)
    {
        ChoiceId = id;
        ((StringVar)DynamicVars["Enchant"]).StringValue = display;
    }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class BeyondReasonForge : MSNeutralCard
{
    private sealed record Option(string Id, string Display, Func<CardModel, bool> CanApply);
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<Swift>(4)
            .Concat(HoverTipFactory.FromEnchantment<ChargeEnchantment>(3))
            .Concat(HoverTipFactory.FromEnchantment<Glam>())
            .Concat(HoverTipFactory.FromEnchantment<TezcatarasEmber>())
            .Concat(HoverTipFactory.FromEnchantment<Instinct>())
            .Concat(HoverTipFactory.FromEnchantment<ProliferationEnchantment>())
            .Concat(HoverTipFactory.FromEnchantment<IronWallEnchantment>())
            .Concat(HoverTipFactory.FromEnchantment<Nimble>(8))
            .Concat(HoverTipFactory.FromEnchantment<Sharp>(8));
    public BeyondReasonForge() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel[] hand = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card != this && LayeredEnchantments.HasOpenSlot(card)).ToArray();
        List<Option> legal = CreateOptions().Where(option => hand.Any(option.CanApply))
            .ToList().UnstableShuffle(Owner.RunState.Rng.CombatCardSelection).Take(3).ToList();
        if (legal.Count == 0) return;

        List<CardModel> choiceCards = [];
        foreach (Option option in legal)
        {
            EnchantmentChoiceCard choice = Owner.RunState.CreateCard<EnchantmentChoiceCard>(Owner);
            choice.Configure(option.Id, option.Display);
            choiceCards.Add(choice);
        }
        EnchantmentChoiceCard? selectedOption = await CardSelectCmd.FromChooseACardScreen(
            context, choiceCards, Owner, canSkip: false) as EnchantmentChoiceCard;
        Option? optionDef = legal.FirstOrDefault(option => option.Id == selectedOption?.ChoiceId);
        if (optionDef == null) return;

        CardModel? target = (await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1),
            card => card != this && LayeredEnchantments.HasOpenSlot(card) && optionDef.CanApply(card), this))
            .FirstOrDefault();
        if (target != null) Apply(optionDef.Id, target);
    }

    private List<Option> CreateOptions() =>
    [
        Vanilla<Swift>("swift", "迅捷：4"),
        Custom<ChargeEnchantment>("charge", "充能：3"),
        Vanilla<Glam>("glam", "华彩"),
        Vanilla<TezcatarasEmber>("ember", "特兹卡塔拉的余烬"),
        Vanilla<Instinct>("instinct", "本能"),
        Custom<ProliferationEnchantment>("proliferation", "增殖"),
        Custom<IronWallEnchantment>("iron_wall", "铁壁"),
        Vanilla<Nimble>("nimble", "灵巧：8"),
        Vanilla<Sharp>("sharp", "锋利：8")
    ];

    private static Option Vanilla<T>(string id, string display) where T : EnchantmentModel =>
        new(id, display, card => ModelDb.Enchantment<T>().CanEnchant(card));
    private static Option Custom<T>(string id, string display) where T : EnchantmentModel =>
        new(id, display, card => ModelDb.Enchantment<T>().CanEnchant(card));

    private static void Apply(string id, CardModel card)
    {
        switch (id)
        {
            case "swift": CombatEnchantmentCmd.ApplyVanilla<Swift>(card, 4); break;
            case "charge": CombatEnchantmentCmd.ApplyAudited<ChargeEnchantment>(card, 3); break;
            case "glam": CombatEnchantmentCmd.ApplyVanilla<Glam>(card, 1); break;
            case "ember": CombatEnchantmentCmd.ApplyVanilla<TezcatarasEmber>(card, 1); break;
            case "instinct": CombatEnchantmentCmd.ApplyVanilla<Instinct>(card, 1); break;
            case "proliferation": CombatEnchantmentCmd.Apply<ProliferationEnchantment>(card, 1); break;
            case "iron_wall": CombatEnchantmentCmd.Apply<IronWallEnchantment>(card, 1); break;
            case "nimble": CombatEnchantmentCmd.ApplyVanilla<Nimble>(card, 8); break;
            case "sharp": CombatEnchantmentCmd.ApplyVanilla<Sharp>(card, 8); break;
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
