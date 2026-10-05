using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Localization;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class Tranquilizer : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [SecondaryResourceVars.ForLocal(
            "DesireLoss", MaidenSuccubusMod.ModId, DesireResource.LocalId, 2),
            new StringVar("DesireIcons", MaidenDesireIconAssets.FormatAmount(2)),
            new CardsVar(1)];
    public Tranquilizer() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Data.Desire.Modify(Owner, -DynamicVars["DesireLoss"].IntValue);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }
    protected override void OnUpgrade()
    {
        DynamicVars["DesireLoss"].UpgradeValueBy(1);
        ((StringVar)DynamicVars["DesireIcons"]).StringValue =
            MaidenDesireIconAssets.FormatAmount(DynamicVars["DesireLoss"].IntValue);
    }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class Stigma : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Layers", 2)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public Stigma() : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        List<CardModel> choices =
        [
            Owner.RunState.CreateCard<StigmaCondemnationChoice>(Owner),
            Owner.RunState.CreateCard<StigmaWeakChoice>(Owner)
        ];
        CardModel? choice = await CardSelectCmd.FromChooseACardScreen(context, choices, Owner, canSkip: false);
        if (choice is StigmaCondemnationChoice)
            await CondemnationCmd.Apply(context, play.Target, DynamicVars["Layers"].BaseValue, Owner.Creature, this);
        else if (choice is StigmaWeakChoice)
            await PowerCmd.Apply<WeakPower>(context, play.Target, DynamicVars["Layers"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars["Layers"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class StigmaTargetChoice : MSGeneratedCard
{
    public int TargetIndex { get; set; }
    public override int MaxUpgradeLevel => 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("Target", "选择目标")];
    public StigmaTargetChoice() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None) { }
    public void Configure(int index, string display)
    {
        TargetIndex = index;
        ((StringVar)DynamicVars["Target"]).StringValue = display;
    }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class StigmaCondemnationChoice : MSGeneratedCard
{
    public override int MaxUpgradeLevel => 0;
    public StigmaCondemnationChoice() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None) { }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class StigmaWeakChoice : MSGeneratedCard
{
    public override int MaxUpgradeLevel => 0;
    public StigmaWeakChoice() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None) { }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class EternalDamnation : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [MaidenSuccubus.Keywords.SinkingKeyword.Value];
    public EternalDamnation() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    public override Task BeforeCombatStart() =>
        Pile?.Type == PileType.Draw
            ? CardPileCmd.Add(this, PileType.Discard)
            : Task.CompletedTask;
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<CondemnationRetentionPower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class ForgeNimble : MSHolyCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<Adroit>(3);
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5, ValueProp.Move)];
    public ForgeNimble() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        CardModel? selected = (await CardSelectCmd.FromHand(
            context,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1),
            card => card != this && MaidenSuccubus.Enchantments.LayeredEnchantments.HasOpenSlot(card)
                && ModelDb.Enchantment<Adroit>().CanEnchant(card),
            this)).FirstOrDefault();
        if (selected != null)
            CombatEnchantmentCmd.ApplyVanilla<Adroit>(selected, 3);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class Rest : MSHolyCard
{
    protected override bool IsPlayable =>
        TransformationCmd.IsTransformed(Owner.Creature);
    public Rest() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await TransformationCmd.Exit(context, Owner.Creature);
        await PowerCmd.Apply<RestNextTurnPower>(
            context, Owner.Creature, 1, Owner.Creature, this);
        PlayerCmd.EndTurn(Owner, canBackOut: false);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class MultipleReproduction : MSHolyCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [CardHoverTipSupport.Static("MAIDENSUCCUBUS_OVERDRAFT")];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public MultipleReproduction() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int previousAmount = Owner.Creature.GetPower<MultipleReproductionPower>()?.Amount ?? 0;
        MultipleReproductionPower? power = await PowerCmd.Apply<MultipleReproductionPower>(
            context, Owner.Creature, 1, Owner.Creature, this);
        if (power != null)
            power.Schedule(!await OverdraftCmd.Offer(context, this, 1), power.Amount - previousAmount);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
