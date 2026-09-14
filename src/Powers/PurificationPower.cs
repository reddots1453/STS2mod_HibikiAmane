using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

/// <summary>
/// Permanent buff. At the start of its owner's turn, performs Amount
/// independent cleanses. Each cleanse chooses uniformly between the currently
/// present eligible debuff types, not between individual layers.
/// </summary>
[RegisterPower]
public sealed class PurificationPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => CommonPowerAssets.Generic;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player.Creature != Owner || Amount <= 0)
        {
            return;
        }

        for (int i = 0; i < Amount; i++)
        {
            List<PowerModel> candidates = GetCandidates();
            if (candidates.Count == 0)
            {
                break;
            }

            int selectedIndex = player.RunState.Rng.CombatCardSelection
                .NextInt(candidates.Count);
            PowerModel selected = candidates[selectedIndex];
            Flash();
            await PowerCmd.ModifyAmount(
                choiceContext,
                selected,
                selected.Amount > 0 ? -1m : 1m,
                Owner,
                null);
        }
    }

    internal List<PowerModel> GetCandidates() => Owner.Powers
        .Where(power => power.Amount != 0)
        .Where(power => power.StackType == PowerStackType.Counter)
        .Where(power => power.TypeForCurrentAmount == PowerType.Debuff)
        .GroupBy(power => power.Id)
        .Select(group => group.First())
        .ToList();
}
