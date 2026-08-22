using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class SemenConversion : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    public SemenConversion() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
        await PowerCmd.Apply<DesirePaidWithHpPower>(context, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class AbnormalAdaptation : MSCorruptCard
{
    public AbnormalAdaptation() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<AbnormalAdaptationPower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class MasochisticGirl : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MasochisticGirlPower>(3)];
    public MasochisticGirl() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<MasochisticGirlPower>(context, Owner.Creature,
            DynamicVars["MasochisticGirlPower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["MasochisticGirlPower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class LightArrow : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move)];
    public LightArrow() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        int layers = Core.Powers.PowerLayerQuery.CountDebuffLayers(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        if (layers > 0) await CardPileCmd.Draw(context, layers, Owner);
    }
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class TerminalSanctuary : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PurificationPower>(2), new PowerVar<BlurPower>(2), new PowerVar<SanctuaryPower>(2)];
    public TerminalSanctuary() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<PurificationPower>(context, Owner.Creature, 2, Owner.Creature, this);
        await PowerCmd.Apply<BlurPower>(context, Owner.Creature, 2, Owner.Creature, this);
        await PowerCmd.Apply<SanctuaryPower>(context, Owner.Creature, 2, Owner.Creature, this);
    }
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class Blizzard : MSHolyCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<IceMist>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];
    public Blizzard() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .TargetingAllOpponents(CombatState).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        await CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard<IceMist>(Owner), PileType.Hand, Owner);
        BlizzardEchoPower echo = (BlizzardEchoPower)ModelDb.Power<BlizzardEchoPower>().ToMutable();
        echo.Damage = DynamicVars.Damage.BaseValue;
        await PowerCmd.Apply(context, echo, Owner.Creature, 2, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}
