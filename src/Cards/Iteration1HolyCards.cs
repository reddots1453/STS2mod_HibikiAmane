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

    public ResistanceGloves()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ControlPower? control = ControlQuery.GetInstances(Owner).FirstOrDefault();
        if (control != null)
        {
            await ControlCmd.Escape(context, control, 2);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class RestraintEvasion : MSHolyCard
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [PortableKeyword.Value];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(6),
        new CalculationExtraVar(1),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(
            static (_, target) => target?.Monster?.NextMove.Intents
                .OfType<ControlIntent>()
                .FirstOrDefault()?.BlockRequired ?? 0),
    ];

    public RestraintEvasion()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.CalculatedBlock.Calculate(play.Target),
            DynamicVars.CalculatedBlock.Props,
            play);

    protected override void OnUpgrade() { }
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
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<RegenerativeMagicFiberPower>(
            context, Owner.Creature, 1, Owner.Creature, this);

    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class LightPowerRelease : MSHolyCard
{
    public LightPowerRelease()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await TransformationCmd.EnterImmaculateRobe(
            context, Owner.Creature, this);
        await PowerCmd.Apply<EternalRobePower>(
            context, Owner.Creature, 9, Owner.Creature, this);
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
