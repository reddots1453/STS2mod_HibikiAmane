using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class Exhibitionist : MSCorruptCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(18, ValueProp.Move)];

    public Exhibitionist()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        CardModel status = Owner.RunState.CreateCard(
            ModelDb.Card<NakedDesireStatus>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(status, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(6);
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
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(5, ValueProp.Move)];

    public DarkFlameBarrier()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<DarkFlameBarrierPower>(
            context, Owner.Creature, 1, Owner.Creature, this);
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
            await PowerCmd.Apply<StrengthPower>(
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
        modified = Math.Max(0, original - CardsExhaustedThisCombat);
        return ReferenceEquals(card, this);
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
    public DemonStaff()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) =>
        this.SecondaryCosts().Set(DesireResource.Id, 1);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        IEnumerable<CardModel> source = Owner.UnlockState.CharacterCardPools
            .SelectMany(pool => pool.GetUnlockedCards(
                Owner.UnlockState,
                Owner.RunState.CardMultiplayerConstraint))
            .Where(card => card is not IMaidenSuccubusRouteCard
                && card.Keywords.Contains(CardKeyword.Exhaust));
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
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class LureDeep : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Escape", 3)];
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public LureDeep()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Target?.Monster != null)
        {
            IntentMoveFactory.TryForceControl(
                play.Target.Monster, Owner, DynamicVars["Escape"].IntValue);
        }
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => DynamicVars["Escape"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class ExposePlay : MSCorruptCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(3, ValueProp.Move)];
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [PortableKeyword.Value];

    public ExposePlay()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        if ((TransformationCmd.GetArmor(Owner.Creature)?.Amount ?? 0) > 1
            || CombatState!.HittableEnemies.Count == 0)
        {
            return;
        }
        var enemies = CombatState.HittableEnemies;
        var enemy = enemies[
            Owner.RunState.Rng.CombatTargets.NextInt(enemies.Count)];
        IntentMoveFactory.TryForceErotic(enemy.Monster!, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class FullOfOpenings : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Repeats", 1)];
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [PortableKeyword.Value];

    public FullOfOpenings()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

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
        if (play.Target?.Monster?.NextMove.Intents.Any(
                intent => intent is StunIntent) == true)
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
        [PortableKeyword.Value];
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
            await CreatureCmd.Stun(play.Target);
        }
    }

    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class PleasureGarden : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public PleasureGarden()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies) =>
        this.SecondaryCosts().Set(DesireResource.Id, 2);

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        foreach (var enemy in CombatState!.HittableEnemies)
        {
            IntentMoveFactory.TryForceErotic(enemy.Monster!, Owner);
        }
        return Task.CompletedTask;
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
        [CardKeyword.Retain];

    public BiteInvader()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await PowerCmd.Apply<WeakPower>(
            context, play.Target, 7, Owner.Creature, this);
        await CreatureCmd.Stun(play.Target);
    }

    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class InsatiableGreed : MSCorruptCard
{
    public InsatiableGreed()
        : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self) =>
        this.SecondaryCosts().Set(DesireResource.Id, 2);

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<UnboundedDesirePower>(
            context, Owner.Creature, 1, Owner.Creature, this);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
