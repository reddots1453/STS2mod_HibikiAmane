using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers.Scriptures;

namespace MaidenSuccubus.Cards.Scriptures;

public abstract class ScriptureCardTemplate<TPower> : ScriptureCardTemplate
    where TPower : ScripturePowerTemplate
{
    protected ScriptureCardTemplate(CardRarity rarity)
        : base(rarity)
    {
    }

    protected sealed override Task ApplyScripture(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        ScriptureCmd.Apply<TPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Duration"].BaseValue,
            Owner.Creature,
            this);
}

public abstract class MSScriptureCardTemplate : ModCardTemplate
{
    public override CardPoolModel Pool => ModelDb.CardPool<MSScriptureCardPool>();

    // Scriptures are derivative cards. They may only be created by the
    // explicit scripture effects and must never enter the compendium or any
    // generic card-generation/reward pool.
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Duration", 2), new EnergyVar(1), new IntVar("Cards", 1)];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/packed/card_portraits/ironclad/bash.png");

    protected virtual IEnumerable<IHoverTip> CardSpecificHoverTips => [];
    protected sealed override IEnumerable<IHoverTip> AdditionalHoverTips =>
        CardHoverTipSupport.FromDynamicPowerVars(DynamicVars.Values)
            .Concat(CardSpecificHoverTips);

    protected MSScriptureCardTemplate(CardRarity rarity)
        : base(0, CardType.Skill, rarity, TargetType.Self, false)
    {
    }

    protected sealed override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        ApplyScripture(choiceContext, cardPlay);

    protected abstract Task ApplyScripture(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay);

    protected override void OnUpgrade() =>
        DynamicVars["Duration"].UpgradeValueBy(1);
}

public abstract class ScriptureCardTemplate : MSScriptureCardTemplate
{
    protected ScriptureCardTemplate(CardRarity rarity) : base(rarity)
    {
    }
}
