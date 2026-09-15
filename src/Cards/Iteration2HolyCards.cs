using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MaidenSuccubus.Core.Temptation;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class HolyFlame : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BurningPower>(4), new DynamicVar("BonusBurning", 2)];

    public HolyFlame()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await PowerCmd.Apply<BurningPower>(context, play.Target,
            DynamicVars["BurningPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<HolyFlamePower>(context, Owner.Creature,
            DynamicVars["BonusBurning"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BurningPower"].UpgradeValueBy(1);
        DynamicVars["BonusBurning"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class BurningRack : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BurningPower>(2), new PowerVar<WeakPower>(2)];

    public BurningRack()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await PowerCmd.Apply<BurningPower>(context, play.Target,
            DynamicVars["BurningPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<WeakPower>(context, play.Target,
            DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BurningPower"].UpgradeValueBy(1);
        DynamicVars["WeakPower"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class ExorcismPerfume : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Temptation", 5), new CardsVar(1)];

    public ExorcismPerfume()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Temptation.Modify(context, Owner, -DynamicVars["Temptation"].IntValue);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade() =>
        DynamicVars["Temptation"].UpgradeValueBy(5);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class PurificationOrb : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, PortableKeyword.Value];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Desire", 2), new DynamicVar("Armor", 1),
            new DynamicVar("Temptation", 10)];

    public PurificationOrb()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Data.Desire.Modify(Owner, -DynamicVars["Desire"].IntValue);
        await TransformationCmd.GainArmor(
            context, Owner.Creature, DynamicVars["Armor"].IntValue, this);
        await Temptation.Modify(
            context, Owner, -DynamicVars["Temptation"].IntValue);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
