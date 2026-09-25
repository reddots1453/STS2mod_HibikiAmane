using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Data;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class DarkThrust : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move), new CardsVar(2)];
    public DarkThrust() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) =>
        this.SecondaryCosts().Set(DesireResource.Id, 1);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class BlasphemousTwilight : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5, ValueProp.Move), new DynamicVar("Hits", 5)];
    public BlasphemousTwilight() : base(1, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy) =>
        this.SecondaryCosts().Set(DesireResource.Id, 4);
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(DynamicVars["Hits"].IntValue)
            .FromCard(this, play).TargetingRandomOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash").Execute(context);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1);
        DynamicVars["Hits"].UpgradeValueBy(1);
    }
}

public sealed class MagicOverdraft : MSCorruptCard
{
    protected override IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip>
        CardSpecificHoverTips =>
        [CardHoverTipSupport.Static("MAIDENSUCCUBUS_OVERDRAFT")];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, ValueProp.Move)];
    public MagicOverdraft() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) =>
        this.SecondaryCosts().Set(DesireResource.Id, 3);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        if (await OverdraftCmd.Offer(context, this, 1))
            await PowerCmd.Apply<SlipperyPower>(context, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class WinterHolly : MSCorruptCard
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10, ValueProp.Move)];
    public WinterHolly() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) =>
        this.SecondaryCosts().Set(DesireResource.Id, 2);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        ArgumentNullException.ThrowIfNull(CombatState);
        CardModel copy = CombatState.CloneCard(this);
        copy.AddKeyword(CardKeyword.Exhaust);
        await CardPileCmd.Add(copy, PileType.Hand);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class AllHopeLost : MSCorruptCard, ISecondaryResourceHookListener
{
    private sealed class DesireScaledDamageVar : DamageVar
    {
        public DesireScaledDamageVar() : base(6, ValueProp.Move) { }

        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            int desire = card.CombatState == null
                ? 1
                : Math.Max(0, Desire.Get(card.Owner));
            decimal damage = BaseValue * desire;
            if (runGlobalHooks)
            {
                PreviewValue = Hook.ModifyDamage(
                    card.Owner.RunState,
                    card.CombatState,
                    target,
                    card.Owner.Creature,
                    damage,
                    Props,
                    card,
                    null,
                    ModifyDamageHookType.All,
                    previewMode,
                    out _);
                return;
            }

            EnchantmentModel? enchantment = card.Enchantment;
            if (enchantment != null)
            {
                damage += enchantment.EnchantDamageAdditive(damage, Props);
                damage *= enchantment.EnchantDamageMultiplicative(damage, Props);
            }
            PreviewValue = Math.Max(0, damage);
        }
    }

    private sealed class CurrentEnergyHitsVar : RepeatVar
    {
        public CurrentEnergyHitsVar() : base("Hits", 0) { }

        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            PreviewValue = card.CombatState == null
                ? BaseValue
                : Hook.ModifyXValue(
                    card.CombatState,
                    card,
                    card.EnergyCost.GetAmountToSpend()) + BaseValue;
        }
    }

    private int _desireSpent;
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DesireScaledDamageVar(), new CurrentEnergyHitsVar()];
    public AllHopeLost() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) =>
        this.SecondaryCosts().Set(DesireResource.Id, SecondaryResourceCost.X());

    public Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context)
    {
        if (context.Card == this && context.Definition.Id == DesireResource.Id)
            _desireSpent = context.Amount;
        return Task.CompletedTask;
    }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        int hits = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        decimal damage = DynamicVars.Damage.BaseValue * _desireSpent;
        _desireSpent = 0;
        return DamageCmd.Attack(damage).WithHitCount(hits).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars["Hits"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class LegendaryMiner : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BlockPerDesire", 3)];
    public LegendaryMiner() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<LegendaryMinerPower>(context, Owner.Creature,
            DynamicVars["BlockPerDesire"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["BlockPerDesire"].UpgradeValueBy(1);
}
