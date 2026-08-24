using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards.Scriptures;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Powers;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class FocusedSlash : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(30, ValueProp.Move)];

    public FocusedSlash()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this)
        {
            return false;
        }
        modifiedCost += Data.Desire.Get(Owner);
        return true;
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(10);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class DesireWard : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8, ValueProp.Move)];

    public DesireWard()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await PowerCmd.Apply<PreventNextDesireGainPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class MomentaryGrace : MSHolyCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<IceMist>(IsUpgraded)];
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(6, ValueProp.Move)];

    public MomentaryGrace()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        CardModel mist = CombatState!.CreateCard<IceMist>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(mist);
        await CardPileCmd.AddGeneratedCardToCombat(mist, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
    }
}

public sealed class HolyDefensePrototype01 : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DexterityPower>(2)];

    public HolyDefensePrototype01()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        decimal dexterity = DynamicVars["DexterityPower"].BaseValue;
        await PowerCmd.Apply<DexterityPower>(
            choiceContext,
            Owner.Creature,
            dexterity,
            Owner.Creature,
            this);
        await PowerCmd.Apply<RestoreDexterityAtTurnEndPower>(
            choiceContext,
            Owner.Creature,
            dexterity,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() =>
        DynamicVars["DexterityPower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class RetainedGuard : MSHolyCard
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(8, ValueProp.Move)];

    public RetainedGuard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class Worship : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DexterityPower>(1), new PowerVar<PurificationPower>(1)];

    public Worship()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PowerCmd.Apply<DexterityPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["DexterityPower"].BaseValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<PurificationPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["PurificationPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DexterityPower"].UpgradeValueBy(1);
        DynamicVars["PurificationPower"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class InwardDiscipline : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<InwardDisciplinePower>(25)];

    public InwardDiscipline()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        PowerCmd.Apply<InwardDisciplinePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["InwardDisciplinePower"].BaseValue,
            Owner.Creature,
            this);

    protected override void OnUpgrade() =>
        DynamicVars["InwardDisciplinePower"].UpgradeValueBy(25);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class SunDance : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public SunDance()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        int amount = PowerLayerQuery.CountBuffLayers(Owner.Creature);
        if (amount <= 0)
        {
            return;
        }
        await PowerCmd.Apply<DexterityPower>(
            choiceContext,
            Owner.Creature,
            amount,
            Owner.Creature,
            this);
        await PowerCmd.Apply<RestoreDexterityAtTurnEndPower>(
            choiceContext,
            Owner.Creature,
            amount,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class DivineEcho : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Layers", 1)];

    public DivineEcho()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        PowerModel[] buffs = Owner.Creature.Powers
            .Where(power =>
                power.IsVisible
                && power.Amount > 0
                && power.TypeForCurrentAmount
                    == MegaCrit.Sts2.Core.Entities.Powers.PowerType.Buff)
            .ToArray();
        foreach (PowerModel buff in buffs)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                buff,
                DynamicVars["Layers"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars["Layers"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class HolyRadiance : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<HolyRadiancePower>(1)];

    public HolyRadiance()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        PowerCmd.Apply<HolyRadiancePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["HolyRadiancePower"].BaseValue,
            Owner.Creature,
            this);

    protected override void OnUpgrade() =>
        DynamicVars["HolyRadiancePower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class Judgment : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7, ValueProp.Move), new DynamicVar("Condemnation", 2)];

    public Judgment()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await CondemnationCmd.Apply(
            choiceContext,
            cardPlay.Target,
            DynamicVars["Condemnation"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() =>
        DynamicVars["Condemnation"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class OriginalSinBrand : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Condemnation", 2), new CardsVar(1)];

    public OriginalSinBrand()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await CondemnationCmd.Apply(
                choiceContext,
                enemy,
                DynamicVars["Condemnation"].BaseValue,
                Owner.Creature,
                this);
        }

        var discardPile = PileType.Discard.GetPile(Owner);
        int selectionCount = Math.Min(
            DynamicVars.Cards.IntValue,
            discardPile.Cards.Count);
        if (selectionCount <= 0)
        {
            return;
        }

        CardModel[] selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            discardPile.Cards,
            Owner,
            new CardSelectorPrefs(
                new LocString(
                    "card_selection",
                    "MAIDEN_SUCCUBUS_TO_HAND_FROM_DISCARD"),
                selectionCount))).ToArray();
        foreach (CardModel card in selected)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class FinalJudgment : MSHolyCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [CardHoverTipSupport.Static("MAIDENSUCCUBUS_OVERDRAFT")];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Condemnation", 1)];

    public FinalJudgment()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        ArgumentNullException.ThrowIfNull(CombatState);

        int existingLayers = cardPlay.Target
            .GetPower<CondemnationPower>()?.Amount ?? 0;
        int judgedLayers = existingLayers
            + DynamicVars["Condemnation"].IntValue;

        await CondemnationCmd.Apply(
            choiceContext,
            cardPlay.Target,
            DynamicVars["Condemnation"].BaseValue,
            Owner.Creature,
            this);

        // Applying the seventh layer triggers judgment automatically. Below
        // threshold, this card explicitly forces the same judgment.
        if (judgedLayers < CondemnationPower.JudgmentThreshold)
        {
            await CondemnationCmd.Judge(
                choiceContext,
                cardPlay.Target,
                force: true);
        }

        if (await Commands.OverdraftCmd.Offer(choiceContext, this, 1))
        {
            decimal splashDamage =
                judgedLayers * CondemnationPower.DamagePerLayer;
            foreach (var enemy in CombatState.HittableEnemies
                         .Where(enemy => enemy != cardPlay.Target)
                         .ToArray())
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    enemy,
                    splashDamage,
                    ValueProp.Move | ValueProp.Unpowered,
                    Owner.Creature,
                    this,
                    cardPlay);
            }
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class Chant : MSHolyCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        ScriptureCardPreview.All();

    public Chant()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        Type[] scriptureTypes =
        [
            typeof(GuardianScripture),
            typeof(NimbleScripture),
            typeof(PunishmentScripture),
            typeof(WisdomScripture),
            typeof(VitalityScripture),
            typeof(BlissScripture),
        ];
        var rng = Owner.RunState.Rng.CombatCardGeneration;
        List<CardModel> available =
        [
            ModelDb.Card<GuardianScripture>(),
            ModelDb.Card<PunishmentScripture>(),
            ModelDb.Card<NimbleScripture>(),
            ModelDb.Card<WisdomScripture>(),
            ModelDb.Card<VitalityScripture>(),
            ModelDb.Card<BlissScripture>()
        ];
        List<CardModel> cards = [];
        while (cards.Count < 3)
        {
            int index = rng.NextInt(available.Count);
            CardModel canonical = available[index];
            available.RemoveAt(index);
            cards.Add(Owner.RunState.CreateCard(canonical, Owner));
        }
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            cards,
            Owner,
            canSkip: false);
        if (selected != null)
        {
            await CardPileCmd.Add(selected, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class Gospel : MSHolyCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
    [
        HoverTipFactory.FromCard<GuardianScripture>(IsUpgraded),
        HoverTipFactory.FromCard<PunishmentScripture>(IsUpgraded),
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public Gospel()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        CardModel[] hand = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card != this)
            .ToArray();
        foreach (CardModel card in hand)
        {
            CardModel? transformed = card.Type switch
            {
                CardType.Skill =>
                    await ScriptureCmd.TransformCombatCard<GuardianScripture>(card),
                CardType.Attack =>
                    await ScriptureCmd.TransformCombatCard<PunishmentScripture>(card),
                _ => null,
            };
            if (IsUpgraded
                && transformed != null
                && transformed.IsUpgradable)
            {
                CardCmd.Upgrade(transformed);
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class HolyResonance : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<HolyResonancePower>(2)];

    public HolyResonance()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        PowerCmd.Apply<HolyResonancePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["HolyResonancePower"].BaseValue,
            Owner.Creature,
            this);

    protected override void OnUpgrade() =>
        DynamicVars["HolyResonancePower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class SneakSnack : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(2), new DynamicVar("Condemnation", 2)];

    public SneakSnack()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CondemnationCmd.Apply(
            choiceContext,
            Owner.Creature,
            DynamicVars["Condemnation"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() =>
        DynamicVars.Energy.UpgradeValueBy(1);
}
