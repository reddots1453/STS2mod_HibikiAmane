using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Core.Cards;
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
        // Keep the full design selection range; unresolved extraction is an explicit
        // development gap, never a guessed Damage/Block rewrite or an unhandled fault.
        var entry = HumilityExtractedCards.Get(selected);
        if (entry.Program == null && HumilityRewriteCapability.Find(selected) == null)
        {
            Godot.GD.PushWarning($"[Humility] {selected.GetType().FullName}: {entry.Error}");
            ThinkCmd.Play(new LocString("cards", "MAIDEN_HUMILITY_REWRITE.unsupported"), Owner.Creature);
            return;
        }
        HumilityExtractedCards.Apply(selected);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
