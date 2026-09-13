using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Relics;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class ClearHeartNecklace : MSRelicTemplate
{
    private bool _isSynchronizing;

    public override RelicRarity Rarity => RelicRarity.Shop;
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/circlet.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres");

    public override async Task AfterObtained()
    {
        CorruptionEvents.Changed += OnCorruptionChanged;
        await SynchronizeVariation();
    }

    public override Task AfterRemoved()
    {
        CorruptionEvents.Changed -= OnCorruptionChanged;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext context,
        Player player)
    {
        if (player == Owner && Data.Desire.Get(Owner) >= 5)
        {
            Flash();
            await Data.Desire.Modify(Owner, -1);
        }
    }

    private void OnCorruptionChanged(CorruptionChanged change)
    {
        if (ReferenceEquals(change.RunState, Owner.RunState))
        {
            _ = SynchronizeVariation();
        }
    }

    private async Task SynchronizeVariation()
    {
        if (_isSynchronizing
            || HasBeenRemovedFromState
            || Owner.RunState is not RunState runState
            || CorruptionQuery.Get(runState) < 3)
        {
            return;
        }

        _isSynchronizing = true;
        try
        {
            await RelicCmd.Replace(
                this,
                ModelDb.Relic<MurkyHeartNecklace>().ToMutable());
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error(
                $"Failed to vary Clear Heart Necklace: {ex}");
        }
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class MurkyHeartNecklace : MSRelicTemplate
{
    private bool _isSynchronizing;

    public override RelicRarity Rarity => RelicRarity.Shop;
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/circlet.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres");

    public override async Task AfterObtained()
    {
        CorruptionEvents.Changed += OnCorruptionChanged;
        await SynchronizeVariation();
    }

    public override Task AfterRemoved()
    {
        CorruptionEvents.Changed -= OnCorruptionChanged;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext context,
        Player player)
    {
        if (player == Owner && Data.Desire.Get(Owner) < 5)
        {
            Flash();
            await Data.Desire.Modify(Owner, 1);
        }
    }

    private void OnCorruptionChanged(CorruptionChanged change)
    {
        if (ReferenceEquals(change.RunState, Owner.RunState))
        {
            _ = SynchronizeVariation();
        }
    }

    private async Task SynchronizeVariation()
    {
        if (_isSynchronizing
            || HasBeenRemovedFromState
            || Owner.RunState is not RunState runState
            || CorruptionQuery.Get(runState) >= 3)
        {
            return;
        }

        _isSynchronizing = true;
        try
        {
            await RelicCmd.Replace(
                this,
                ModelDb.Relic<ClearHeartNecklace>().ToMutable());
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error(
                $"Failed to vary Murky Heart Necklace: {ex}");
        }
    }
}
