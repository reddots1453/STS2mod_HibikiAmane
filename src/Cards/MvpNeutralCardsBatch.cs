using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class SwordVerdict : MSNeutralCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(24, ValueProp.Move)];
    public SwordVerdict() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        bool belowHalf = play.Target.CurrentHp * 2 < play.Target.MaxHp;
        decimal damage = DynamicVars.Damage.BaseValue * (belowHalf ? 2 : 1);
        await DamageCmd.Attack(damage).FromCard(this, play).Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(context);
        if (belowHalf && play.Target.IsAlive)
        {
            await IntentMoveFactory.Stun(play.Target);
        }
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class LightningRecoil : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6, ValueProp.Move)];
    public LightningRecoil() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        if (await TransformationCmd.PayOverdraft(
                context, Owner.Creature, this))
        {
            await CardPileCmd.Draw(context, 1, Owner);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

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
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<IceShard>(IsUpgraded)];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move)];
    public IceBreakingSlash() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        CardModel shard = CombatState!.CreateCard<IceShard>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(shard);
        await CardPileCmd.AddGeneratedCardToCombat(shard, PileType.Hand, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class IceShield : MSNeutralCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<IceShard>(IsUpgraded)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public IceShield() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        for (int i = 0; i < 3; i++)
        {
            CardModel shard = CombatState!.CreateCard<IceShard>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(shard);
            await CardPileCmd.AddGeneratedCardToCombat(shard, PileType.Hand, Owner);
        }
    }
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class FlashStab : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5, ValueProp.Move)];
    public FlashStab() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        CardModel copy = CreateClone();
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Draw, Owner, CardPilePosition.Random);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}
