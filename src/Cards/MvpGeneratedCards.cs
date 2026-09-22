using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Core.Transformation;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Cards;

public abstract class MSGeneratedCard : ModCardTemplate
{
    public override CardPoolModel Pool => ModelDb.CardPool<MSGeneratedCardPool>();
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: CardArtAssets.GetPortraitPath(GetType()));
    protected virtual IEnumerable<IHoverTip> CardSpecificHoverTips => [];
    protected sealed override IEnumerable<IHoverTip> AdditionalHoverTips =>
        CardHoverTipSupport.FromDynamicPowerVars(DynamicVars.Values)
            .Concat(CardHoverTipSupport.FromDescriptionReferences(this))
            .Concat(CardSpecificHoverTips);
    protected MSGeneratedCard(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        bool shouldShowInCardLibrary = false)
        : base(cost, type, rarity, target, shouldShowInCardLibrary) { }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class ArousalStatus : MSGeneratedCard
{
    public override int MaxUpgradeLevel => 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Ethereal, CardKeyword.Unplayable];
    public ArousalStatus() : base(-1, CardType.Status, CardRarity.Status, TargetType.None) { }
    public override async Task AfterCardDrawn(
        PlayerChoiceContext context, CardModel card, bool fromHandDraw)
    {
        if (card == this) await Data.Desire.Modify(Owner, 1);
    }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class NakedDesireStatus : MSGeneratedCard
{
    public override int MaxUpgradeLevel => 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    public override bool HasTurnEndInHandEffect => true;
    public NakedDesireStatus() : base(-1, CardType.Status, CardRarity.Status, TargetType.None) { }
    protected override async Task OnTurnEndInHand(PlayerChoiceContext context)
    {
        MagicArmorPower? armor = TransformationCmd.GetArmor(Owner.Creature);
        if (armor != null)
            await PowerCmd.Decrement(armor);
    }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class IceMist : MSGeneratedCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override IEnumerable<MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar> CanonicalVars =>
        [new MegaCrit.Sts2.Core.Localization.DynamicVars.PowerVar<MegaCrit.Sts2.Core.Models.Powers.DexterityPower>(2)];
    public IceMist() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        decimal amount = DynamicVars["DexterityPower"].BaseValue;
        await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.DexterityPower>(
            context, Owner.Creature, amount, Owner.Creature, this);
        await PowerCmd.Apply<RestoreDexterityAtTurnEndPower>(
            context, Owner.Creature, amount, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["DexterityPower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class FourthRouteQuestChoice : MSGeneratedCard
{
    public string QuestId { get; set; } = string.Empty;
    public override int MaxUpgradeLevel => 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new StringVar("QuestName", "未选择"),
            new StringVar("QuestText", "尚未选择任务"),
            new StringVar("RewardText", "尚未指定奖励"),
        ];
    public FourthRouteQuestChoice() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None) { }
    public void Configure(string id, string name, string text, string rewardText)
    {
        QuestId = id;
        ((StringVar)DynamicVars["QuestName"]).StringValue = name;
        ((StringVar)DynamicVars["QuestText"]).StringValue = text;
        ((StringVar)DynamicVars["RewardText"]).StringValue = rewardText;
    }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class OverdraftAcceptChoice : MSGeneratedCard
{
    public override int MaxUpgradeLevel => 0;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("ArmorCost", 1)];

    public OverdraftAcceptChoice()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.None) { }

    public void Configure(int armorCost) =>
        DynamicVars["ArmorCost"].BaseValue = armorCost;

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Task.CompletedTask;
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class OverdraftDeclineChoice : MSGeneratedCard
{
    public override int MaxUpgradeLevel => 0;

    public OverdraftDeclineChoice()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.None) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Task.CompletedTask;
}
