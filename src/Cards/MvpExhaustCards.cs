using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class MimicProliferation : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    public MimicProliferation() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel? selected = (await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            card => card != this, this)).FirstOrDefault();
        if (selected == null || CombatState == null) return;
        await CardCmd.Exhaust(context, selected);
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
            await CardPileCmd.Add(CombatState.CloneCard(selected), PileType.Hand);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

public sealed class MiasmaFrenzy : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public MiasmaFrenzy() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel[] attacks = PileType.Hand.GetPile(Owner).Cards.Where(card => card.Type == CardType.Attack).ToArray();
        foreach (CardModel card in attacks) await CardCmd.Exhaust(context, card);
        if (attacks.Length > 0)
            await PowerCmd.Apply<StrengthPower>(context, Owner.Creature, attacks.Length, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class MagicExcess : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public MagicExcess() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel[] selected = (await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, 4),
            card => card != this, this)).ToArray();
        foreach (CardModel card in selected) await CardCmd.Exhaust(context, card);
        if (selected.Length == 4)
        {
            int missing = Math.Max(0, 10 - PileType.Hand.GetPile(Owner).Cards.Count);
            if (missing > 0) await CardPileCmd.Draw(context, missing, Owner);
        }
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class DestructionReaction : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];
    public DestructionReaction() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel[] drawn = (await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner)).ToArray();
        if (drawn.Length == 0) return;
        HashSet<CardModel> selectable = drawn
            .Where(card => card.Pile?.Type == PileType.Hand)
            .ToHashSet();
        if (selectable.Count == 0) return;
        CardModel? selected = (await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            selectable.Contains,
            this)).FirstOrDefault();
        if (selected != null) await CardCmd.Exhaust(context, selected);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class ThousandCurseScythe : MSCorruptCard, IPermanentGrowthCard
{
    private int _currentDamage = 8;

    [SavedProperty]
    public int CurrentDamage
    {
        get => _currentDamage;
        set
        {
            AssertMutable();
            _currentDamage = value;
            DynamicVars.Damage.BaseValue = value;
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(CurrentDamage, ValueProp.Move), new DynamicVar("Growth", 4)];

    public ThousandCurseScythe()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
    }

    public override Task AfterCardExhausted(
        PlayerChoiceContext context,
        CardModel card,
        bool causedByEthereal)
    {
        if (card != this)
        {
            return Task.CompletedTask;
        }
        int growth = DynamicVars["Growth"].IntValue;
        CurrentDamage += growth;
        PermanentCardCmd.TryModifyDeckVersion(
            this,
            deck => ((ThousandCurseScythe)deck).CurrentDamage += growth);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() =>
        DynamicVars["Growth"].UpgradeValueBy(2);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class DesireRecycle : MSCorruptCard
{
    public DesireRecycle() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<DesireRecyclePower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class ReflectiveBarrier : MSCorruptCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8, ValueProp.Move)];
    public ReflectiveBarrier() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    public override async Task AfterCardExhausted(
        PlayerChoiceContext context,
        CardModel card,
        bool causedByEthereal)
    {
        if (card != this)
        {
            return;
        }
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.Block.BaseValue,
            DynamicVars.Block.Props,
            null);
        await PowerCmd.Apply<MagicAmplificationPower>(
            context, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Ethereal);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class SuperRegeneration : MSCorruptCard
{
    private bool _returnAfterExhaust;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> CardSpecificHoverTips =>
        [CardHoverTipSupport.Static("MAIDENSUCCUBUS_OVERDRAFT")];
    public SuperRegeneration() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    internal static bool CanSelect(CardModel card) =>
        card is not SuperRegeneration && !card.Keywords.Contains(CardKeyword.Unplayable);

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card == this && cardPlay.IsFirstInSeries) _returnAfterExhaust = false;
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardPile pile = PileType.Exhaust.GetPile(Owner);
        CardModel? selected = (await CardSelectCmd.FromCombatPile(context, pile, Owner,
            new CardSelectorPrefs(new MegaCrit.Sts2.Core.Localization.LocString(
                "card_selection", "MAIDEN_SUCCUBUS_TO_PLAY_FROM_EXHAUST"), 1),
            CanSelect)).FirstOrDefault();
        if (selected != null) await CardCmd.AutoPlay(context, selected, null);
        if (await OverdraftCmd.Offer(context, this, 1)) _returnAfterExhaust = true;
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal)
    {
        if (card != this || !_returnAfterExhaust) return;
        _returnAfterExhaust = false;
        if (Pile?.Type == PileType.Exhaust) await CardPileCmd.Add(this, PileType.Hand);
    }

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _returnAfterExhaust = false; // A new copy did not pay this play's release.
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
