using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class Ignite : MSCorruptCard
{
    public Ignite() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel? selected = (await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            card => card != this, this)).FirstOrDefault();
        if (selected == null) return;
        await CardCmd.Exhaust(context, selected);
        IgnitePower? power = await PowerCmd.Apply<IgnitePower>(context, Owner.Creature, 1, Owner.Creature, this);
        if (power != null)
        {
            power.CardId = selected.Id.Entry;
            power.WasUpgraded = selected.IsUpgraded;
        }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class BlackVortex : MSCorruptCard
{
    public BlackVortex() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        while (true)
        {
            List<CardModel> played = [];
            for (int i = 0; i < 2; i++)
            {
                await CardPileCmd.ShuffleIfNecessary(context, Owner);
                CardModel? card = PileType.Draw.GetPile(Owner).Cards.FirstOrDefault();
                if (card == null) break;
                played.Add(card);
                await CardCmd.AutoPlay(context, card, null);
            }
            if (played.Count == 0 || !await OverdraftCmd.Offer(context, this, 1)) break;
            foreach (CardModel card in played)
            {
                if (card.Pile?.Type != PileType.Exhaust)
                    await CardCmd.Exhaust(context, card);
            }
        }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class GrudgeFire : MSCorruptCard
{
    private int _currentDamage = 1;
    [SavedProperty]
    public int CurrentDamage
    {
        get => _currentDamage;
        set { AssertMutable(); _currentDamage = value; DynamicVars.Damage.BaseValue = value; }
    }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(CurrentDamage, ValueProp.Move)];
    public GrudgeFire() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_fire").Execute(context);
    }
    public override Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal)
    {
        if (card.Owner == Owner && card.Type == CardType.Attack && card != this)
        {
            CurrentDamage += Math.Max(0, card.DynamicVars.Damage.IntValue);
        }
        return Task.CompletedTask;
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class ChainDestruction : MSCorruptCard
{
    public ChainDestruction() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<ChainDestructionPower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class CurseCorridor : MSCorruptCard
{
    public CurseCorridor() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<CurseCorridorPower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class GrudgeBlade : MSCorruptCard, IPermanentGrowthCard
{
    private int _currentDamage = 8;
    [SavedProperty]
    public int CurrentDamage
    {
        get => _currentDamage;
        set { AssertMutable(); _currentDamage = value; DynamicVars.Damage.BaseValue = value; }
    }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(CurrentDamage, ValueProp.Move), new DynamicVar("Increase", 4)];
    public GrudgeBlade() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
    }
    public override Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal)
    {
        if (card != this) return Task.CompletedTask;
        int increase = DynamicVars["Increase"].IntValue;
        CurrentDamage += increase;
        PermanentCardCmd.ModifyDeckVersion(this, deck => ((GrudgeBlade)deck).CurrentDamage += increase);
        return Task.CompletedTask;
    }
    protected override void OnUpgrade() => DynamicVars["Increase"].UpgradeValueBy(1);
}
