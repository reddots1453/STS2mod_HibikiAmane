using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

public abstract class ClothingHazardStatus : MSGeneratedCard
{
    public override int MaxUpgradeLevel => 0;

    protected ClothingHazardStatus(IEnumerable<CardKeyword> keywords)
        : base(1, CardType.Status, CardRarity.Status, TargetType.Self)
    {
        _keywords = keywords.ToArray();
    }

    private readonly IReadOnlyList<CardKeyword> _keywords;
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected Task LoseArmor(PlayerChoiceContext context) =>
        TransformationCmd.LoseArmor(context, Owner.Creature, 1, this);
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class BarbedHookStatus : ClothingHazardStatus
{
    public BarbedHookStatus() : base([CardKeyword.Exhaust]) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        LoseArmor(context);
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class ClothingBurnStatus : ClothingHazardStatus
{
    public ClothingBurnStatus()
        : base([CardKeyword.Ethereal, CardKeyword.Exhaust]) { }

    public override bool HasTurnEndInHandEffect => true;

    protected override Task OnTurnEndInHand(PlayerChoiceContext context) =>
        LoseArmor(context);
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class BitingPaperStatus : ClothingHazardStatus
{
    public BitingPaperStatus() : base([CardKeyword.Exhaust]) { }

    public override bool HasTurnEndInHandEffect => true;

    protected override Task OnTurnEndInHand(PlayerChoiceContext context) =>
        LoseArmor(context);
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class DissolvingFluidStatus : ClothingHazardStatus
{
    public DissolvingFluidStatus()
        : base([CardKeyword.Retain, CardKeyword.Exhaust]) { }

    public override Task BeforeSideTurnEnd(
        PlayerChoiceContext context,
        CombatSide side,
        IEnumerable<Creature> participants) =>
        side == CombatSide.Player && Pile?.Type == PileType.Hand
            ? LoseArmor(context)
            : Task.CompletedTask;
}
