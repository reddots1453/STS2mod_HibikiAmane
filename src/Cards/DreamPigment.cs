using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class DreamPigment : MSNeutralCard
{
    public DreamPigment() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        // Read the current draw order, without randomly selecting cards or
        // reshuffling the discard pile to fill a missing route.
        foreach (RouteCardKind route in new[] { RouteCardKind.Corrupt, RouteCardKind.Holy, RouteCardKind.Neutral })
        {
            if (CombatManager.Instance.IsOverOrEnding || PileType.Hand.GetPile(Owner).Cards.Count >= 10)
                break;
            if (!Hook.ShouldDraw(CombatState, Owner, fromHandDraw: false, out AbstractModel? modifier))
            {
                if (modifier != null)
                    await Hook.AfterPreventingDraw(CombatState, modifier);
                break;
            }
            CardModel? selected = PileType.Draw.GetPile(Owner).Cards.FirstOrDefault(
                card => RouteCardQuery.TryGet(card, out RouteCardKind kind) && kind == route);
            if (selected == null)
                continue;
            await CardPileCmd.Add(selected, PileType.Hand);
            // This is a draw, not just a transfer: retain vanilla history,
            // draw-triggered powers/card effects and the card's UI notification.
            CombatManager.Instance.History.CardDrawn(CombatState, selected, fromHandDraw: false);
            await Hook.AfterCardDrawn(CombatState, context, selected, fromHandDraw: false);
            selected.InvokeDrawn();
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
