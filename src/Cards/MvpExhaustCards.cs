using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
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

[RegisterCard(typeof(MSCorruptCardPool))]
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

[RegisterCard(typeof(MSCorruptCardPool))]
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
        CardModel? selected = (await CardSelectCmd.FromSimpleGrid(context, drawn, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1))).FirstOrDefault();
        if (selected != null) await CardCmd.Exhaust(context, selected);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class ThousandCurseScythe : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(33, ValueProp.Move)];
    public ThousandCurseScythe() : base(8, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal cost, out decimal modified)
    {
        modified = cost;
        if (card != this) return false;
        modified = Math.Max(0, cost - PileType.Exhaust.GetPile(Owner).Cards.Count);
        return true;
    }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(11);
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
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(9, ValueProp.Move)];
    public ReflectiveBarrier() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    public override Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal) =>
        card == this
            ? CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, DynamicVars.Block.Props, null)
            : Task.CompletedTask;
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Exhaust);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class SuperRegeneration : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public SuperRegeneration() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardPile pile = PileType.Exhaust.GetPile(Owner);
        CardModel? selected = (await CardSelectCmd.FromCombatPile(context, pile, Owner,
            new CardSelectorPrefs(new MegaCrit.Sts2.Core.Localization.LocString(
                "card_selection", "MAIDEN_SUCCUBUS_TO_PLAY_FROM_EXHAUST"), 1),
            card => card != this && !card.Keywords.Contains(CardKeyword.Unplayable))).FirstOrDefault();
        if (selected != null) await CardCmd.AutoPlay(context, selected, null);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
