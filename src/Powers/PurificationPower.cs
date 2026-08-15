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
public sealed class PurificationPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MaidenSuccubus/images/ui/desire_resource.png",
        BigIconPath: "res://MaidenSuccubus/images/ui/desire_resource.png");

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
                -1m,
                Owner,
                null);
        }
    }

    internal List<PowerModel> GetCandidates()
    {
        List<PowerModel> candidates = [];
        AddIfPresent<VulnerablePower>(candidates);
        AddIfPresent<WeakPower>(candidates);
        AddIfPresent<FrailPower>(candidates);
        AddIfPresent<CondemnationPower>(candidates);
        return candidates;
    }

    private void AddIfPresent<T>(ICollection<PowerModel> candidates)
        where T : PowerModel
    {
        T? power = Owner.GetPower<T>();
        if (power is { Amount: > 0 })
        {
            candidates.Add(power);
        }
    }
}
