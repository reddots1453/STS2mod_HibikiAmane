using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class HumilityLesson : MSGeneratedCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    public HumilityLesson() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel? selected = (await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1),
            card => card != this && card.Type is CardType.Attack or CardType.Skill, this)).FirstOrDefault();
        if (selected == null) return;
        if (selected.DynamicVars.TryGetValue("Damage", out DynamicVar? damage)) damage.BaseValue *= 2;
        if (selected.DynamicVars.TryGetValue("Block", out DynamicVar? block)) block.BaseValue *= 2;
        selected.FinalizeUpgradeInternal();
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
