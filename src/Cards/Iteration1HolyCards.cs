using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class HolyCurse : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7, ValueProp.Move)];

    public HolyCurse()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        int layers = Core.Powers.PowerLayerQuery.CountDebuffLayers(play.Target);
        await IterationCardEffects.Attack(
            this, context, play, DynamicVars.Damage.BaseValue);
        if (layers > 0)
        {
            await CardPileCmd.Draw(context, layers, Owner);
        }
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class WindRumor : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9, ValueProp.Move), new CardsVar(1)];

    public WindRumor()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await IterationCardEffects.Attack(
            this, context, play, DynamicVars.Damage.BaseValue);
        CardModel[] candidates = PileType.Discard.GetPile(Owner).Cards.ToArray();
        if (candidates.Length == 0)
        {
            return;
        }
        int count = Math.Min(DynamicVars.Cards.IntValue, candidates.Length);
        CardModel[] selected = (await CardSelectCmd.FromSimpleGrid(
            context,
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, count))).ToArray();
        foreach (CardModel card in selected)
        {
            await CardPileCmd.Add(card, PileType.Draw);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class ResistanceGloves : MSHolyCard, IEscapeCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [PortableKeyword.Value];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Escape", 2)];

    public ResistanceGloves()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ControlPower? control = ControlQuery.GetInstances(Owner).FirstOrDefault();
        if (control != null)
        {
            await ControlCmd.Escape(
                context, control, DynamicVars["Escape"].IntValue);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars["Escape"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class RestraintEvasion : MSHolyCard, IEscapeCard
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [PortableKeyword.Value];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6, ValueProp.Move),
    ];

    public RestraintEvasion()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        ControlIntent? intent = play.Target?.Monster?.NextMove.Intents
            .OfType<ControlIntent>()
            .FirstOrDefault();
        if (intent != null)
        {
            await CreatureCmd.GainBlock(
                Owner.Creature, intent.BlockRequired, DynamicVars.Block.Props, play);
        }
        ControlPower? control = ControlQuery.GetInstances(Owner)
            .FirstOrDefault(candidate =>
                play.Target == null || ReferenceEquals(candidate.Applier, play.Target));
        if (control != null)
        {
            await ControlCmd.Escape(context, control, 1);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class ChastityDefense : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [PortableKeyword.Value];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ChastityDefensePower>(1)];

    public ChastityDefense()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        decimal turns = DynamicVars["ChastityDefensePower"].BaseValue;
        await PowerCmd.Apply<ChastityDefensePower>(
            context, Owner.Creature, turns, Owner.Creature, this);
        await PowerCmd.Apply<RetainHandPower>(
            context, Owner.Creature, turns, Owner.Creature, this);
    }

    protected override void OnUpgrade() =>
        DynamicVars["ChastityDefensePower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class RegenerativeMagicFiber : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [PortableKeyword.Value];

    public RegenerativeMagicFiber()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<RegenerativeMagicFiberPower>(
            context, Owner.Creature, 1, Owner.Creature, this);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class LightPowerRelease : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [SinkingKeyword.Value];
    public LightPowerRelease()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        card != this || !Owner.Creature.HasPower<EternalRobePower>();

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await TransformationCmd.EnterEternalRobe(
            context, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class TacticalCore : MSHolyCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(7, ValueProp.Move), new DynamicVar("Turns", 3)];

    public TacticalCore()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<TacticalCorePower>(
            context,
            Owner.Creature,
            DynamicVars["Turns"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
        DynamicVars["Turns"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class BattleTechniqueReplay : MSHolyCard
{
    protected override bool IsPlayable => false;

    public BattleTechniqueReplay()
        : base(-1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override async Task BeforeCombatStart()
    {
        await EnsureReplayPower();
    }

    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (card == this)
        {
            await EnsureReplayPower();
        }
    }

    private async Task EnsureReplayPower()
    {
        if (!Owner.Creature.HasPower<BattleTechniqueReplayPower>())
        {
            await PowerCmd.Apply<BattleTechniqueReplayPower>(
                new BlockingPlayerChoiceContext(),
                Owner.Creature,
                1,
                Owner.Creature,
                this);
        }
    }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Task.CompletedTask;

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
