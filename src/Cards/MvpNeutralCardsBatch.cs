using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class SwordVerdict : MSNeutralCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(25, ValueProp.Move), new DynamicVar("Multiplier", 2)];
    public SwordVerdict() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        decimal damage = DynamicVars.Damage.BaseValue;
        if (play.Target.CurrentHp * 2 < play.Target.MaxHp)
            damage *= DynamicVars["Multiplier"].BaseValue;
        return DamageCmd.Attack(damage).FromCard(this, play).Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars["Multiplier"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class LightningRecoil : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move)];
    public LightningRecoil() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal cost, out decimal modified)
    {
        modified = cost;
        if (card != this) return false;
        modified += PileType.Hand.GetPile(Owner).Cards.Count(other => other != this);
        return true;
    }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_magic").Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class PrepareAhead : MSNeutralCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(7, ValueProp.Move), new CardsVar(2)];
    public PrepareAhead() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<DrawCardsNextTurnPower>(context, Owner.Creature,
            DynamicVars.Cards.BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class IceShard : MSGeneratedCard
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3, ValueProp.Move)];
    public IceShard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class IceBreakingSlash : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move)];
    public IceBreakingSlash() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        CardModel shard = Owner.RunState.CreateCard<IceShard>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(shard);
        await CardPileCmd.AddGeneratedCardToCombat(shard, PileType.Hand, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class IceShield : MSNeutralCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public IceShield() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        for (int i = 0; i < 3; i++)
        {
            CardModel shard = Owner.RunState.CreateCard<IceShard>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(shard);
            await CardPileCmd.AddGeneratedCardToCombat(shard, PileType.Hand, Owner);
        }
    }
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class FlashStab : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];
    public FlashStab() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        CardModel copy = Owner.RunState.CreateCard<FlashStab>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(copy);
        await CardPileCmd.Add(copy, PileType.Draw, CardPilePosition.Random);
    }
    // The design intentionally defines no numerical upgrade for this card.  An
    // upgraded copy still propagates its upgrade marker to generated copies.
    protected override void OnUpgrade() { }
}
