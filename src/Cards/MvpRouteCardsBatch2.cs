using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Powers;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class PleasureDrowning : MSCorruptCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7, ValueProp.Move), new CardsVar(2)];
    public PleasureDrowning() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
        for (int i = 0; i < 2; i++)
            await CardPileCmd.Add(Owner.RunState.CreateCard<ArousalStatus>(Owner), PileType.Draw, CardPilePosition.Random);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class DesireLockdown : MSCorruptCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(18, ValueProp.Move)];
    public DesireLockdown() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await CardPileCmd.AddGeneratedCardToCombat(
            Owner.RunState.CreateCard<NakedDesireStatus>(Owner), PileType.Hand, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(6);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class MentalStabilizer : MSCorruptCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(11, ValueProp.Move)];
    public MentalStabilizer() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        => this.SecondaryCosts().Set(DesireResource.Id, 1);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        CardModel[] cards = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card.Type is CardType.Status or CardType.Curse).ToArray();
        await CardCmd.Discard(context, cards);
        if (cards.Length > 0) await CardPileCmd.Draw(context, cards.Length, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class MasochisticTrance : MSCorruptCard
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6, ValueProp.Move)];
    public MasochisticTrance() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        int layers = PowerLayerQuery.CountDebuffLayers(Owner.Creature);
        if (layers > 0) await CardPileCmd.Draw(context, layers, Owner);
    }
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class NoLewdness : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(3), new CardsVar(3)];
    public NoLewdness() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal cost, out decimal modified)
    {
        modified = cost;
        if (card != this) return false;
        modified += Data.Desire.Get(Owner);
        return true;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class PhotonVolt : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(10, ValueProp.Move), new PowerVar<Powers.PurificationPower>(1)];
    public PhotonVolt() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_magic").Execute(context);
        if (Data.Desire.Get(Owner) <= 2)
            await PowerCmd.Apply<Powers.PurificationPower>(context, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}
