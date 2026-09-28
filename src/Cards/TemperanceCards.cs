using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class TemperanceSignet : MSGeneratedCard
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", 3)];
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips => [HoverTipFactory.FromPower<NoDrawPower>()];
    public TemperanceSignet() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<NoDrawPower>(context, Owner.Creature, 1, Owner.Creature, this);
        await TemperancePileCmd.Play(context, Owner, DynamicVars["Cards"].IntValue);
    }
    protected override void OnUpgrade() => DynamicVars["Cards"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class TemperanceCirclet : MSGeneratedCard
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips => [HoverTipFactory.FromPower<YarusLibraryPower>()];
    public TemperanceCirclet() : base(2, CardType.Power, CardRarity.Ancient, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<YarusLibraryPower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
