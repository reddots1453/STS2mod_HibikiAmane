using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Core.Invasion;

namespace MaidenSuccubus.Cards.Curses;

public abstract class MSInvasionCurseTemplate :
    ModCardTemplate,
    IInvasionSourcedCurse
{
    [SavedProperty]
    public string SourceMonsterId { get; set; } = "";

    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedByModifiers => false;
    public override CardPoolModel Pool => ModelDb.CardPool<MSInvasionCursePool>();
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/atlases/card_atlas.sprites/beta.tres");

    protected MSInvasionCurseTemplate()
        : base(1, CardType.Curse, CardRarity.Curse, TargetType.None, true) { }
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class SemenCurse : MSInvasionCurseTemplate;
