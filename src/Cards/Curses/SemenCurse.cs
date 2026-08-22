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
        PortraitPath: "res://images/packed/card_portraits/ironclad/bash.png");

    protected MSInvasionCurseTemplate(int cost = 1)
        : base(cost, CardType.Curse, CardRarity.Curse, TargetType.None, true) { }
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class SemenCurse : MSInvasionCurseTemplate;

[RegisterCard(typeof(MSInvasionCursePool))] public sealed class FoulSlimeCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class AphrodisiacCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class SporeMucusCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class ParalyticSlimeCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class CorrosiveSlimeCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class InsectEggCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class ParasiticEggCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class InkFluidCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class ScorchingFluidCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class EctoplasmResidueCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class MagicResidueCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class VineSeedCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class SludgeSemenCurse : MSInvasionCurseTemplate
{
    public SludgeSemenCurse() : base(2) { }
}
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class DeepSeaSlimeCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))] public sealed class ExperimentalLiquidCurse : MSInvasionCurseTemplate;
[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class RoyalEssenceCurse : MSInvasionCurseTemplate
{
    public RoyalEssenceCurse() : base(2) { }
}

public abstract class MSEventCurseTemplate : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedByModifiers => false;
    public override CardPoolModel Pool => ModelDb.CardPool<MSGeneratedCardPool>();
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/packed/card_portraits/ironclad/bash.png");
    protected MSEventCurseTemplate(int cost = -1)
        : base(cost, CardType.Curse, CardRarity.Curse, TargetType.None, true) { }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class LewdMarkMinorCurse : MSEventCurseTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class LewdMarkSpreadCurse : MSEventCurseTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class LewdMarkCompleteCurse : MSEventCurseTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Retain];
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class TransparentOutfitCurse : MSEventCurseTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class InfatuationCurse : MSEventCurseTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public InfatuationCurse() : base(2) { }
}
[RegisterCard(typeof(MSGeneratedCardPool))] public sealed class HypnosisCurse : MSEventCurseTemplate;
[RegisterCard(typeof(MSGeneratedCardPool))] public sealed class GagCurse : MSEventCurseTemplate { public GagCurse() : base(2) { } }
[RegisterCard(typeof(MSGeneratedCardPool))] public sealed class ClimaxBanCurse : MSEventCurseTemplate;
