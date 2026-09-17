using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Models.Capabilities;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Pools;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Cards;

public abstract class MSNeutralCardTemplate : ModCardTemplate, IMaidenSuccubusRouteCard
{
    public virtual RouteCardKind RouteKind => RouteCardKind.Neutral;
    public override CardPoolModel Pool => ModelDb.CardPool<MSNeutralCardPool>();

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/packed/card_portraits/ironclad/bash.png");

    protected virtual IEnumerable<IHoverTip> CardSpecificHoverTips => [];
    protected sealed override IEnumerable<IHoverTip> AdditionalHoverTips =>
        CardHoverTipSupport.FromDynamicPowerVars(DynamicVars.Values)
            .Concat(CardHoverTipSupport.FromDescriptionReferences(this))
            .Concat(CardSpecificHoverTips);

    protected MSNeutralCardTemplate(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target)
        : base(cost, type, rarity, target, true) { }
}

public abstract class MSNeutralCard : MSNeutralCardTemplate
{
    protected MSNeutralCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target) { }
}

public abstract class MSCorruptCardTemplate :
    ModCardTemplate,
    IMaidenSuccubusRouteCard,
    ICardOverlayContributor
{
    public virtual RouteCardKind RouteKind => RouteCardKind.Corrupt;
    public override CardPoolModel Pool => ModelDb.CardPool<MSCorruptCardPool>();

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/packed/card_portraits/ironclad/bash.png");

    protected virtual IEnumerable<IHoverTip> CardSpecificHoverTips => [];
    protected sealed override IEnumerable<IHoverTip> AdditionalHoverTips =>
        CardHoverTipSupport.FromDynamicPowerVars(DynamicVars.Values)
            .Concat(CardHoverTipSupport.FromDescriptionReferences(this))
            .Concat(CardSpecificHoverTips);

    IEnumerable<CardOverlayContribution>
        ICardOverlayContributor.GetCardOverlays(CardOverlayContext context) =>
        RouteCardVisuals.GetOverlays(RouteKind);

    protected MSCorruptCardTemplate(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target)
        : base(cost, type, rarity, target, true) { }
}

public abstract class MSCorruptCard : MSCorruptCardTemplate
{
    protected MSCorruptCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target) { }
}

public abstract class MSHolyCardTemplate :
    ModCardTemplate,
    IMaidenSuccubusRouteCard,
    ICardOverlayContributor
{
    public virtual RouteCardKind RouteKind => RouteCardKind.Holy;
    public override CardPoolModel Pool => ModelDb.CardPool<MSHolyCardPool>();

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/packed/card_portraits/ironclad/bash.png");

    protected virtual IEnumerable<IHoverTip> CardSpecificHoverTips => [];
    protected sealed override IEnumerable<IHoverTip> AdditionalHoverTips =>
        CardHoverTipSupport.FromDynamicPowerVars(DynamicVars.Values)
            .Concat(CardHoverTipSupport.FromDescriptionReferences(this))
            .Concat(CardSpecificHoverTips);

    IEnumerable<CardOverlayContribution>
        ICardOverlayContributor.GetCardOverlays(CardOverlayContext context) =>
        RouteCardVisuals.GetOverlays(RouteKind);

    protected MSHolyCardTemplate(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target)
        : base(cost, type, rarity, target, true) { }
}

public abstract class MSHolyCard : MSHolyCardTemplate
{
    protected MSHolyCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target) { }
}
