using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Data;

namespace MaidenSuccubus.Powers.Scriptures;

[RegisterPower]
public sealed class GuardianScripturePower : ScripturePowerTemplate
{
    protected override ScriptureTriggerTiming Timing =>
        ScriptureTriggerTiming.TurnEnd;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new GuardianScriptureBlockVar()];

    public override PowerAssetProfile AssetProfile => ScriptureAssets.Profile;

    protected override Task TriggerEffect(PlayerChoiceContext choiceContext) =>
        CreatureCmd.GainBlock(Owner, DynamicVars.Block, null);
}

[RegisterPower]
public sealed class NimbleScripturePower : ScripturePowerTemplate
{
    private const int DexterityAmount = 2;

    protected override ScriptureTriggerTiming Timing =>
        ScriptureTriggerTiming.OnApply;

    public override PowerAssetProfile AssetProfile => ScriptureAssets.Profile;

    protected override Task TriggerEffect(PlayerChoiceContext choiceContext) =>
        PowerCmd.Apply<DexterityPower>(
            choiceContext,
            Owner,
            DexterityAmount,
            Owner,
            null);

    public override Task AfterRemoved(Creature oldOwner) =>
        PowerCmd.Apply<DexterityPower>(
            new ThrowingPlayerChoiceContext(),
            oldOwner,
            -DexterityAmount,
            oldOwner,
            null);
}

[RegisterPower]
public sealed class PunishmentScripturePower : ScripturePowerTemplate
{
    protected override ScriptureTriggerTiming Timing =>
        ScriptureTriggerTiming.TurnEnd;

    public override PowerAssetProfile AssetProfile => ScriptureAssets.Profile;

    protected override async Task TriggerEffect(PlayerChoiceContext choiceContext)
    {
        var combatState = Owner.CombatState;
        var player = Owner.Player;
        if (combatState == null || player == null)
        {
            return;
        }

        IReadOnlyList<Creature> enemies = combatState.HittableEnemies;
        if (enemies.Count == 0)
        {
            return;
        }

        int index = player.RunState.Rng.CombatTargets.NextInt(enemies.Count);
        await CondemnationCmd.Apply(
            choiceContext,
            enemies[index],
            1m,
            Owner,
            null);
    }
}

[RegisterPower]
public sealed class WisdomScripturePower : ScripturePowerTemplate
{
    protected override ScriptureTriggerTiming Timing =>
        ScriptureTriggerTiming.TurnStart;

    public override PowerAssetProfile AssetProfile => ScriptureAssets.Profile;

    protected override Task TriggerEffect(PlayerChoiceContext choiceContext)
    {
        var player = Owner.Player
            ?? throw new InvalidOperationException(
                "Wisdom Scripture can only be owned by a player.");
        return CardPileCmd.Draw(choiceContext, 1, player);
    }
}

[RegisterPower]
public sealed class VitalityScripturePower : ScripturePowerTemplate
{
    protected override ScriptureTriggerTiming Timing =>
        ScriptureTriggerTiming.TurnStart;

    public override PowerAssetProfile AssetProfile => ScriptureAssets.Profile;

    protected override Task TriggerEffect(PlayerChoiceContext choiceContext)
    {
        var player = Owner.Player
            ?? throw new InvalidOperationException(
                "Vitality Scripture can only be owned by a player.");
        return PlayerCmd.GainEnergy(1m, player);
    }
}

[RegisterPower]
public sealed class BlissScripturePower : ScripturePowerTemplate
{
    protected override ScriptureTriggerTiming Timing =>
        ScriptureTriggerTiming.TurnStart;

    public override PowerAssetProfile AssetProfile => ScriptureAssets.Profile;

    protected override async Task TriggerEffect(PlayerChoiceContext choiceContext)
    {
        var player = Owner.Player
            ?? throw new InvalidOperationException(
                "Bliss Scripture can only be owned by a player.");
        if (Desire.Get(player) > 0)
        {
            await Desire.Modify(choiceContext, player, -1);
        }

        if (Desire.Get(player) == 0)
        {
            await PlayerCmd.GainEnergy(1m, player);
            await CardPileCmd.Draw(choiceContext, 1, player);
        }
    }
}

internal static class ScriptureAssets
{
    internal static PowerAssetProfile Profile => CommonPowerAssets.Generic;
}
