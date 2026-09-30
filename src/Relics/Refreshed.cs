using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Relics;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class Refreshed : MSRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;
    public override RelicAssetProfile AssetProfile => RelicIconAssets.For("event_Refreshed");
    public override bool HasUponPickupEffect => true;
    public override bool ShowCounter => true;
    public override int DisplayAmount => RemainingCombats;
    [SavedProperty] public int RemainingCombats { get; set; } = 3;
    [SavedProperty] public bool CombatPrepared { get; set; }
    [SavedProperty] public bool OpeningPending { get; set; }

    private bool Eligible => IsMutable && !HasBeenRemovedFromState
        && Owner is { Character: MaidenSuccubusCharacter } && Owner.Creature.IsAlive;

    public override Task BeforeCombatStart()
    {
        if (!Eligible || CombatPrepared || RemainingCombats <= 0) return Task.CompletedTask;
        CombatPrepared = true;
        OpeningPending = true;
        RemainingCombats--;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    // Same hand-draw boundary as native BagOfPreparation; queries never spend a charge.
    public override decimal ModifyHandDraw(Player player, decimal count) =>
        Eligible && player == Owner && OpeningPending && player.PlayerCombatState?.TurnNumber == 1
            ? count + 2 : count;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        // Native hand draw has finished before this hook, even if drawing was blocked.
        if (!Eligible || player != Owner || !OpeningPending || player.PlayerCombatState?.TurnNumber != 1) return;
        OpeningPending = false;
        Flash();
        if (RemainingCombats == 0) await RelicCmd.Remove(this);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        CombatPrepared = false;
        OpeningPending = false;
        // Also clean up if the last battle ended before the opening hand completed.
        if (!HasBeenRemovedFromState && IsMutable && Owner?.Character is MaidenSuccubusCharacter
            && RemainingCombats == 0) await RelicCmd.Remove(this);
    }
}
