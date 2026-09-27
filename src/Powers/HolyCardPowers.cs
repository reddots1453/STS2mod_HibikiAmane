using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Scriptures;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class RestoreDexterityAtTurnEndPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
        {
            return;
        }
        decimal amount = Amount;
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<DexterityPower>(
            choiceContext,
            Owner,
            -amount,
            Owner,
            null);
    }
}

[RegisterPower]
public sealed class PreventNextDesireGainPower :
    MaidenSuccubusPowerTemplate,
    ISecondaryResourceHookListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public bool ShouldGainSecondaryResource(
        SecondaryResourceContext context,
        decimal amount)
    {
        if (Amount <= 0 || amount <= 0
            || context.Definition.Id != DesireResource.Id
            || context.Player.Creature != Owner)
        {
            return true;
        }
        Flash();
        if (Amount > 1)
            TaskHelper.RunSafely(PowerCmd.Apply<PreventNextDesireGainPower>(
                new BlockingPlayerChoiceContext(), Owner, -1, Owner, null));
        else
            TaskHelper.RunSafely(PowerCmd.Remove(this));
        return false;
    }
}

[RegisterPower]
public sealed class InwardDisciplinePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target != Owner || !Owner.HasPower<WeakPower>())
        {
            return 1m;
        }
        return 1m + Amount / 100m;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer != Owner || !Owner.HasPower<FrailPower>())
        {
            return 1m;
        }
        return 1m + Amount / 100m;
    }
}

[RegisterPower]
public sealed class HolyRadiancePower : MaidenSuccubusPowerTemplate
{
    private bool _resolving;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => CommonPowerAssets.Generic;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (_resolving
            || power.Owner != Owner
            || amount <= 0
            || power.TypeForCurrentAmount != PowerType.Buff
            || power == this
            || Owner.CombatState == null)
        {
            return;
        }
        _resolving = true;
        try
        {
            Flash();
            IReadOnlyList<Creature> enemies = Owner.CombatState.HittableEnemies;
            if (enemies.Count > 0)
            {
                Creature enemy = enemies[
                    Owner.CombatState.RunState.Rng.CombatTargets.NextInt(enemies.Count)];
                await CreatureCmd.Damage(
                    choiceContext,
                    enemy,
                    Amount,
                    ValueProp.Move | ValueProp.Unpowered,
                    Owner,
                    null,
                    null);
            }
        }
        finally
        {
            _resolving = false;
        }
    }
}

[RegisterPower]
public sealed class HolyResonancePower :
    MaidenSuccubusPowerTemplate,
    IScriptureTriggeredListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterScriptureTriggered(ScriptureTriggered scripture)
    {
        if (scripture.Owner != Owner)
        {
            return;
        }
        Flash();
        await CreatureCmd.GainBlock(
            Owner,
            Amount,
            ValueProp.Move,
            null);
    }
}
