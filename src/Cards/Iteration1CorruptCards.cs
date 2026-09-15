using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using MaidenSuccubus.Core.Temptation;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class Exhibitionist : MSCorruptCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<NakedDesireStatus>()];
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0),
        new CalculationExtraVar(1),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(
            static (card, _) => Temptation.Get(card.Owner)),
    ];

    public Exhibitionist()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.CalculatedBlock.Calculate(null),
            DynamicVars.CalculatedBlock.Props,
            play);
        CardModel status = CombatState!.CreateCard(
            ModelDb.Card<NakedDesireStatus>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(status, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class LordOfBlaze : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<LordOfBlazePower>(2)];

    public LordOfBlaze()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<LordOfBlazePower>(
            context,
            Owner.Creature,
            DynamicVars["LordOfBlazePower"].BaseValue,
            Owner.Creature,
            this);

    protected override void OnUpgrade() =>
        DynamicVars["LordOfBlazePower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class DarkFlameBarrier : MSCorruptCard
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(6, ValueProp.Move), new DynamicVar("Turns", 2)];

    public DarkFlameBarrier()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<DarkFlameBarrierPower>(
            context, Owner.Creature, DynamicVars["Turns"].BaseValue, Owner.Creature, this);
        if (await TransformationCmd.PayOverdraft(context, Owner.Creature, this))
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class MiasmaConversion : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public MiasmaConversion()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel[] cards = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card != this
                && card.Type is CardType.Attack or CardType.Curse)
            .ToArray();
        foreach (CardModel card in cards)
        {
            await CardCmd.Exhaust(context, card);
        }
        if (cards.Length > 0)
        {
            await PowerCmd.Apply<MagicAmplificationPower>(
                context, Owner.Creature, cards.Length, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class DarkPunishment : MSCorruptCard
{
    [SavedProperty] public int CardsExhaustedThisCombat { get; set; }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(33, ValueProp.Move)];

    public DarkPunishment()
        : base(8, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override bool TryModifyEnergyCostInCombat(
        CardModel card, decimal original, out decimal modified)
    {
        modified = original;
        if (!ReferenceEquals(card, this))
        {
            return false;
        }

        modified = Math.Max(0, original - CardsExhaustedThisCombat);
        return true;
    }

    public override Task AfterCardExhausted(
        PlayerChoiceContext context,
        CardModel card,
        bool ethereal)
    {
        if (card.Owner == Owner)
        {
            CardsExhaustedThisCombat++;
        }
        return Task.CompletedTask;
    }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        IterationCardEffects.Attack(this, context, play, DynamicVars.Damage.BaseValue);

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(11);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class RecollectionRoom : MSCorruptCard
{
    public RecollectionRoom()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<RecollectionRoomPower>(
            context, Owner.Creature, 2, Owner.Creature, this);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class MiasmaAffinity : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Ethereal];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    public MiasmaAffinity()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel[] source = PileType.Draw.GetPile(Owner).Cards.ToArray();
        if (source.Length == 0)
        {
            return;
        }
        int count = Math.Min(DynamicVars.Cards.IntValue, source.Length);
        CardModel[] selected = (await CardSelectCmd.FromSimpleGrid(
            context,
            source,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, count))).ToArray();
        foreach (CardModel card in selected)
        {
            card.AddKeyword(CardKeyword.Ethereal);
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class DemonStaff : MSCorruptCard
{
    // DesignDoc's current-game candidate catalogue.  IDs are used instead of
    // localized titles so the pool remains stable in every language.
    internal static IReadOnlySet<string> CandidateIds { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        // Ironclad
        "DEMONIC_SHIELD", "DOMINATE", "FEED", "FIEND_FIRE", "IMPERVIOUS",
        "INFERNAL_BLADE", "MOLTEN_FIST", "OFFERING", "STOKE", "CINDER",
        "TRUE_GRIT", "HAVOC", "TREMBLE", "ASHEN_STRIKE", "HOWL_FROM_BEYOND",
        "FORGOTTEN_RITUAL", "BURNING_PACT", "EVIL_EYE", "DRUM_OF_BATTLE",
        "SECOND_WIND", "FEEL_NO_PAIN", "PACTS_END", "THRASH", "BRAND",
        "DARK_EMBRACE",

        // Silent
        "ADRENALINE", "ASSASSINATE", "BACKSTAB", "BLADE_DANCE",
        "CALCULATED_GAMBLE", "EXPOSE", "MALAISE", "MIRAGE", "NIGHTMARE",
        "PIERCING_WAIL", "THE_HUNT", "INTIMIDATE", "KNIFE_TRAP",

        // Defect (the current Chinese localization calls SCAVENGE “内存清理”)
        "BOOT_SEQUENCE", "CHILL", "DOUBLE_ENERGY", "ENERGY_SURGE",
        "GENETIC_ALGORITHM", "HOLOGRAM", "IGNITION", "RAINBOW", "REBOOT",
        "SIGNAL_BOOST", "SUPERCRITICAL", "VOLTAIC", "WHITE_NOISE", "HOTFIX",
        "FUSION", "SCAVENGE", "FLAK_CANNON",

        // Necrobinder
        "AFTERLIFE", "DREDGE", "GLIMPSE_BEYOND", "GRAVEBLAST",
        "LEGION_OF_BONE", "PUTREFY", "REANIMATE", "SHARED_FATE", "THE_SCYTHE",
        "TRANSFIGURE", "WISP", "DIRGE", "CLEANSE", "SOUL_STORM", "EIDOLON",

        // Regent
        "BIG_BANG", "BOMBARDMENT", "BUNDLE_OF_JOY", "DECISIONS_DECISIONS",
        "GUARDS", "KNOW_THY_PLACE", "ROYAL_GAMBLE", "TYRANNY",
    };

    public DemonStaff()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) =>
        this.SecondaryCosts().Set(DesireResource.Id, 1);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        IEnumerable<CardModel> source = Owner.UnlockState.CharacterCardPools
            .SelectMany(pool => pool.GetUnlockedCards(
                Owner.UnlockState,
                Owner.RunState.CardMultiplayerConstraint))
            .Where(card => IsEligibleCandidate(card, IsUpgraded));
        List<CardModel> choices = CardFactory.GetDistinctForCombat(
            Owner,
            source,
            3,
            Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (IsUpgraded)
        {
            foreach (CardModel card in choices.Where(card => card.IsUpgradable))
            {
                CardCmd.Upgrade(card);
            }
        }
        if (choices.Count == 0)
        {
            return;
        }

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            context, choices, Owner, false);
        if (selected == null)
        {
            return;
        }
        GeneratedCardCostCmd.SetFreeThisTurn(selected);
        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() { }

    internal static bool IsEligibleCandidate(CardModel card, bool upgradedPool)
    {
        if (!CandidateIds.Contains(card.Id.Entry))
            return false;
        if (!upgradedPool || !card.IsUpgradable)
            return true;

        CardModel upgraded = card.ToMutable();
        CardCmd.Upgrade(upgraded);
        return HasExhaustRule(upgraded);
    }

    private static bool HasExhaustRule(CardModel card)
    {
        if (card.Keywords.Contains(CardKeyword.Exhaust))
            return true;

        string exhaustTipId = HoverTipFactory.FromKeyword(CardKeyword.Exhaust).Id;
        return card.HoverTips.Any(tip => tip.Id == exhaustTipId);
    }
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class LureDeep : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Temptation", 10), new CardsVar(1)];

    public LureDeep()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Temptation.Modify(context, Owner, DynamicVars["Temptation"].IntValue);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Temptation"].UpgradeValueBy(5);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class ExposePlay : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Temptation", 20), new DynamicVar("ArmorThreshold", 1)];
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public ExposePlay()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) =>
        this.SecondaryCosts().Set(DesireResource.Id, 1);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Temptation.Modify(context, Owner, DynamicVars["Temptation"].IntValue);
        if ((TransformationCmd.GetArmor(Owner.Creature)?.Amount ?? 0)
                > DynamicVars["ArmorThreshold"].IntValue
            || CombatState!.HittableEnemies.Count == 0)
        {
            return;
        }
        var enemies = CombatState.HittableEnemies;
        var enemy = enemies[
            Owner.RunState.Rng.CombatTargets.NextInt(enemies.Count)];
        IntentMoveFactory.TryForceErotic(enemy.Monster!, Owner);
    }

    protected override void OnUpgrade() =>
        DynamicVars["ArmorThreshold"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class FullOfOpenings : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Repeats", 1)];
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [PortableKeyword.Value];

    public FullOfOpenings()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        int repeats = ControlQuery.IsControlled(Owner)
            ? 1 + DynamicVars["Repeats"].IntValue
            : 1;
        for (int i = 0; i < repeats; i++)
        {
            await PowerCmd.Apply<WeakPower>(
                context, play.Target, 1, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(
                context, play.Target, 1, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Repeats"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class LoversDagger : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [PortableKeyword.Value];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(18, ValueProp.Move)];

    public LoversDagger()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        decimal damage = DynamicVars.Damage.BaseValue;
        if (ControlQuery.IsControlled(Owner))
        {
            damage *= 2;
        }
        if (play.Target?.IsStunned == true)
        {
            damage *= 2;
        }
        return IterationCardEffects.Attack(this, context, play, damage);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class DesireWhip : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [PortableKeyword.Value, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7, ValueProp.Move)];

    public DesireWhip()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) =>
        this.SecondaryCosts().Set(DesireResource.Id, 1);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await IterationCardEffects.Attack(
            this, context, play, DynamicVars.Damage.BaseValue);
        if (play.Target?.Monster?.NextMove.Intents.Any(
                intent => intent is ControlIntent or InvasionIntent) == true)
        {
            await IntentMoveFactory.Stun(play.Target);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class PleasureGarden : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public PleasureGarden()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies) =>
        this.SecondaryCosts().Set(DesireResource.Id, 2);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Temptation.Modify(context, Owner, 30);
        foreach (var enemy in CombatState!.HittableEnemies)
        {
            IntentMoveFactory.TryForceErotic(enemy.Monster!, Owner);
        }
    }

    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class SemenAppetite : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<SemenAppetitePower>(2)];

    public SemenAppetite()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<SemenAppetitePower>(
            context,
            Owner.Creature,
            DynamicVars["SemenAppetitePower"].BaseValue,
            Owner.Creature,
            this);

    protected override void OnUpgrade() =>
        DynamicVars["SemenAppetitePower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class BiteInvader : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [PortableKeyword.Value];

    public BiteInvader()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await PowerCmd.Apply<WeakPower>(
            context, play.Target, 7, Owner.Creature, this);
        await IntentMoveFactory.Stun(play.Target);
    }

    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class InsatiableGreed : MSCorruptCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [CardHoverTipSupport.Static("MAIDENSUCCUBUS_VARIATION")];

    public InsatiableGreed()
        : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self) =>
        this.SecondaryCosts().Set(DesireResource.Id, 2);

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<UnboundedDesirePower>(
            context, Owner.Creature, 1, Owner.Creature, this);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
