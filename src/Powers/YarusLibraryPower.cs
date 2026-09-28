using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class YarusLibraryPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    private bool IsActive => IsMutable && Amount > 0 && Owner.IsAlive && Owner.Powers.Contains(this);
    public override bool ShouldDraw(Player player, bool fromHandDraw) => !IsActive || player.Creature != Owner;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (!IsActive || player.Creature != Owner || Owner.CombatState == null
            || CombatManager.Instance.IsOverOrEnding)
            return;
        var combat = Owner.CombatState;

        LibraryPileChoice[] choices = new[] { PileType.Draw, PileType.Discard, PileType.Exhaust }
            .Select(pile =>
            {
                LibraryPileChoice choice = Owner.CombatState.CreateCard<LibraryPileChoice>(player);
                choice.Configure(pile);
                return choice;
            }).ToArray();
        if (await CardSelectCmd.FromChooseACardScreen(context, choices, player, canSkip: false)
            is not LibraryPileChoice selected)
            return;
        if (!IsActive || Owner.CombatState != combat || CombatManager.Instance.IsOverOrEnding
            || !choices.Contains(selected))
            return;

        // Snapshot the selected pile: cards returned by their own effects cannot
        // be replayed repeatedly to turn a one-card discard pile into ten plays.
        List<CardModel> cards = selected.SelectedPile.GetPile(player).Cards.ToList();
        cards.StableShuffle(player.RunState.Rng.CombatCardGeneration);
        foreach (CardModel card in cards.Take(10))
        {
            if (Owner.CombatState != combat || !Owner.IsAlive || CombatManager.Instance.IsOverOrEnding) break;
            if (card.Owner == player && card.Pile == selected.SelectedPile.GetPile(player))
                await CardCmd.AutoPlay(context, card, target: null);
        }
    }
}
