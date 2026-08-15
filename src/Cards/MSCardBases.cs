using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Cards;

public abstract class MSNeutralCardTemplate : ModCardTemplate, IMaidenSuccubusRouteCard
{
    public RouteCardKind RouteKind => RouteCardKind.Neutral;
    public override CardPoolModel Pool => ModelDb.CardPool<MSNeutralCardPool>();

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/atlases/card_atlas.sprites/beta.tres");

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

public abstract class MSCorruptCardTemplate : ModCardTemplate, IMaidenSuccubusRouteCard
{
    public RouteCardKind RouteKind => RouteCardKind.Corrupt;
    public override CardPoolModel Pool => ModelDb.CardPool<MSCorruptCardPool>();

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/atlases/card_atlas.sprites/beta.tres");

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

public abstract class MSHolyCardTemplate : ModCardTemplate, IMaidenSuccubusRouteCard
{
    public RouteCardKind RouteKind => RouteCardKind.Holy;
    public override CardPoolModel Pool => ModelDb.CardPool<MSHolyCardPool>();

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/atlases/card_atlas.sprites/beta.tres");

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
